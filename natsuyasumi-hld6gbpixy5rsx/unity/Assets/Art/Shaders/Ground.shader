// 山ぎわの 地めん。
//
// **草地と 踏み分け道の 2枚を、頂点の 色で 混ぜる。**
// 道の 形を テクスチャ 1枚に 焼くと、広い 山を おおうには 目が あらすぎる。
// 敷きつめる 絵は そのままの こまかさで 使い、どこが 道かだけを 頂点に 持たせる。
//
// 混ぜぐあい＝頂点の 色の 赤。0＝草、1＝土。
Shader "Natsuyasumi/Ground"
{
    Properties
    {
        _GrassMap("Grass", 2D) = "white" {}
        _DirtMap("Dirt", 2D) = "white" {}
        _TileGrass("Grass tile (m)", Float) = 1.5
        _TileDirt("Dirt tile (m)", Float) = 1.5
        _BaseColor("Tint", Color) = (1,1,1,1)
        _EdgeSharp("Path edge", Range(0.02,0.5)) = 0.16
        _Wrap("Light Wrap", Range(0,1)) = 0.25
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _GrassMap_ST;
            float4 _DirtMap_ST;
            float  _TileGrass;
            float  _TileDirt;
            half4  _BaseColor;
            half   _Wrap;
            half   _EdgeSharp;
        CBUFFER_END

        TEXTURE2D(_GrassMap);  SAMPLER(sampler_GrassMap);
        TEXTURE2D(_DirtMap);   SAMPLER(sampler_DirtMap);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                half   fogFactor  : TEXCOORD2;
                half   dirt       : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.fogFactor  = ComputeFogFactor(p.positionCS.z);
                OUT.dirt       = IN.color.r;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // **世界の 座標で 敷きつめる。** そうしないと 斜面で 絵が のびる
                float2 uvG = IN.positionWS.xz / max(_TileGrass, 0.01);
                float2 uvD = IN.positionWS.xz / max(_TileDirt, 0.01);
                half3 grass = SAMPLE_TEXTURE2D(_GrassMap, sampler_GrassMap, uvG).rgb;
                half3 dirt  = SAMPLE_TEXTURE2D(_DirtMap,  sampler_DirtMap,  uvD).rgb;
                // **道の ふちを 締める。** 頂点の 色は 1m ごとにしか 置けず、
                // そのまま 混ぜると ふちが 2〜3m も ぼけて、道では なく ただの
                // 色の うつり変わりに 見えた。ここで しきいを 立てて 縁を はっきりさせる
                half e = saturate(_EdgeSharp);
                half t = smoothstep(0.5h - e, 0.5h + e, IN.dirt);
                half3 albedo = lerp(grass, dirt, t) * _BaseColor.rgb;

                float3 N = normalize(IN.normalWS);
                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);
                half ndl = saturate(dot(N, mainLight.direction));
                half diff = lerp(ndl, ndl * 0.5 + 0.5, _Wrap);
                half3 lighting = mainLight.color * diff * mainLight.shadowAttenuation;
                lighting += SampleSH(N);

                #if defined(_ADDITIONAL_LIGHTS)
                    InputData inputData = (InputData)0;
                    inputData.positionWS = IN.positionWS;
                    inputData.normalWS = N;
                    inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                    inputData.shadowCoord = shadowCoord;
                    inputData.positionCS = IN.positionCS;
                    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                    uint lightCount = GetAdditionalLightsCount();
                    LIGHT_LOOP_BEGIN(lightCount)
                        Light l = GetAdditionalLight(lightIndex, inputData.positionWS, half4(1,1,1,1));
                        half lndl = saturate(dot(N, l.direction));
                        lighting += l.color * lerp(lndl, lndl * 0.5 + 0.5, _Wrap)
                                  * (l.distanceAttenuation * l.shadowAttenuation);
                    LIGHT_LOOP_END
                #endif

                half3 col = albedo * lighting;
                col = MixFog(col, IN.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct SA { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct SV { float4 positionCS : SV_POSITION; };

            SV shadowVert(SA IN)
            {
                SV OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                #if UNITY_REVERSED_Z
                    positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = positionCS;
                return OUT;
            }

            half4 shadowFrag(SV IN) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex depthVert
            #pragma fragment depthFrag

            struct DA { float4 positionOS : POSITION; };
            struct DV { float4 positionCS : SV_POSITION; };

            DV depthVert(DA IN) { DV o; o.positionCS = TransformObjectToHClip(IN.positionOS.xyz); return o; }
            half4 depthFrag(DV IN) : SV_Target { return 0; }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex dnVert
            #pragma fragment dnFrag

            struct NA { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct NV { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; };

            NV dnVert(NA IN)
            {
                NV o;
                o.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                o.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return o;
            }
            half4 dnFrag(NV IN) : SV_Target { return half4(normalize(IN.normalWS) * 0.5 + 0.5, 0); }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Lit"
}
