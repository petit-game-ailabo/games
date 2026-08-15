// ドット絵の 板（キャラ・草木）を えがく シェーダ。
//
// ねらいは ふたつ:
//  1) **息づかい**。頂点シェーダで 板を たてに のびちぢみさせる。
//     足もと(下端)は 動かさず、上へ いくほど 大きく 動く＝地に ついたまま 息を する。
//     参考: https://tec.tecotec.co.jp/entry/2025/12/25/000000
//     （記事は three.js だが、やっている ことは同じ＝sin(時間) を 高さの 重みで かける）
//  2) **ドット絵の 見た目を こわさない**。切りぬき(アルファテスト)・にじませない・
//     まわりこみの ある やわらかい 光。URP/Lit の 金属っぽい てかりは いらない。
//
// 影は ふつうに おとす／うける。深度も 書く（被写界深度が 正しく かかる ように）。
Shader "Natsuyasumi/PixelSprite"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)
        _Cutoff("Alpha Cutoff", Range(0,1)) = 0.5

        _BreatheAmp("Breathe Amp (height ratio)", Range(0,0.3)) = 0.035
        _BreatheSpeed("Breathe Speed", Range(0,10)) = 1.5
        _SwayAmp("Sway Amp (height ratio)", Range(0,0.3)) = 0.010
        _SwaySpeed("Sway Speed", Range(0,10)) = 0.7
        _Phase("Phase Offset", Float) = 0

        _Wrap("Light Wrap", Range(0,1)) = 0.55
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "TransparentCutout"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
        }

        // 板は 裏を 向くことが ある（十字組みを やめても 影の パスで 裏から 見る）ので 両面
        Cull Off

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _Cutoff;
            half   _BreatheAmp;
            half   _BreatheSpeed;
            half   _SwayAmp;
            half   _SwaySpeed;
            float  _Phase;
            half   _Wrap;
        CBUFFER_END

        TEXTURE2D(_BaseMap);
        SAMPLER(sampler_BaseMap);

        // ★2026-08-15：**主人公の まわりだけ 手前の ものを 抜く。**
        //   家の 入口ぎわで カーブミラーや 電柱が カメラに かぶって、
        //   主人公が 見えなく なって いた（本人の 指摘）。
        //   まるごと 消すと 物が 点滅して 見えるので、**丸く 穴を あける**。
        //   ふちは ちらして 抜く＝ドット絵に なじむ。
        //   SeeThrough.cs が 毎フレーム 入れる：xy=画面の 位置 z=半径 w=主人公までの 深さ
        float4 _HoleParams;

        // 主人公より **手前に ある もの だけ** 抜く。
        // これが ないと 主人公の 板 じしんや うしろの 景色まで 消える
        void ClipHole(float4 positionCS, float3 positionWS)
        {
            if (_HoleParams.z <= 0.0001) return;
            float viewZ = -TransformWorldToView(positionWS).z;
            if (viewZ >= _HoleParams.w - 0.35) return;

            float2 d = GetNormalizedScreenSpaceUV(positionCS) - _HoleParams.xy;
            d.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);   // 丸く 抜く
            float t = saturate(length(d) / _HoleParams.z);        // 0=まん中 1=ふち
            // まん中は まるごと、ふちは ちらして 抜く。
            // ★ちらす 帯は **せまく**。広い（0.55〜1.0）と ざらざらが 目だって
            //   画が よごれて 見えた
            float2 px = floor(positionCS.xy * 0.5);
            float dither = frac(sin(dot(px, float2(12.9898, 78.233))) * 43758.5453);
            clip(t - (0.80 + dither * 0.20));
        }

        // 息づかい／かぜの ゆれ。
        // Quad の ローカル y は -0.5..0.5 なので、+0.5 で 0(足もと)..1(頭) の 重みに なる。
        // 重みを かけるので **下端は まったく 動かない＝浮かない**（木なら みきが 動かない）。
        // よこ揺れは 重みの 2乗＝上の 葉ほど 大きく ゆれる。
        //
        // ★ずらし(位相)と はやさは **置いてある 場所から 決める**。
        //   1本ごとに 素材を 分けなくても ばらばらに 揺れるので、
        //   木が 何百本に なっても 素材は 1つ＝まとめて 描ける
        float3 Breathe(float3 positionOS)
        {
            float w = saturate(positionOS.y + 0.5);
            float3 org = mul(UNITY_MATRIX_M, float4(0, 0, 0, 1)).xyz;
            float ph  = _Phase + org.x * 2.7 + org.z * 1.3;
            float spd = 0.75 + frac(org.x * 0.37 + org.z * 0.61 + 0.13) * 0.6;
            float t = _Time.y * _BreatheSpeed * spd + ph;
            positionOS.y += sin(t) * _BreatheAmp * w;
            positionOS.x += sin(_Time.y * _SwaySpeed * spd + ph * 1.7) * _SwayAmp * w * w;
            return positionOS;
        }
        ENDHLSL

        // ---------------- 本体（光を あてて えがく） ----------------
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
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half   fogFactor  : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 posOS = Breathe(IN.positionOS.xyz);
                VertexPositionInputs p = GetVertexPositionInputs(posOS);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN, FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC) : SV_Target
            {
                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                clip(tex.a - _Cutoff);
                ClipHole(IN.positionCS, IN.positionWS);

                // 裏から 見た ときは 法線も ひっくり返す（両面えがきなので）
                float3 N = normalize(IN.normalWS) * IS_FRONT_VFACE(cullFace, 1.0, -1.0);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // まわりこみの ある 拡散。板は 法線が 1方向しか ないので、
                // ふつうの Lambert だと 光の むきで まっ黒に なる。_Wrap で 下駄を はかせる
                half ndl = saturate(dot(N, mainLight.direction));
                half diff = lerp(ndl, ndl * 0.5 + 0.5, _Wrap);
                half3 lighting = mainLight.color * diff * mainLight.shadowAttenuation;

                // まわりの 明るさ（環境光）
                lighting += SampleSH(N);

                // 行灯などの 点光源
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
                        half ldiff = lerp(lndl, lndl * 0.5 + 0.5, _Wrap);
                        lighting += l.color * ldiff * (l.distanceAttenuation * l.shadowAttenuation);
                    LIGHT_LOOP_END
                #endif

                half3 col = tex.rgb * lighting;
                col = MixFog(col, IN.fogFactor);
                return half4(col, 1.0);
            }
            ENDHLSL
        }

        // ---------------- 影を おとす ----------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct SAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct SVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            SVaryings shadowVert(SAttributes IN)
            {
                SVaryings OUT;
                // **影も いっしょに 息をする**。ここで 同じ ずらしを かけないと 影だけ 止まって 見える
                float3 posOS = Breathe(IN.positionOS.xyz);
                float3 positionWS = TransformObjectToWorld(posOS);
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
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 shadowFrag(SVaryings IN) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ---------------- 深度（被写界深度・霧の ため） ----------------
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex depthVert
            #pragma fragment depthFrag

            struct DAttributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
            };

            struct DVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
            };

            DVaryings depthVert(DAttributes IN)
            {
                DVaryings OUT;
                OUT.positionCS = TransformObjectToHClip(Breathe(IN.positionOS.xyz));
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 depthFrag(DVaryings IN) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(a - _Cutoff);
                return 0;
            }
            ENDHLSL
        }

        // ---------------- 深度＋法線（SSAO などが 要る ばあい） ----------------
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex dnVert
            #pragma fragment dnFrag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"

            struct DNAttributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct DNVaryings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
            };

            DNVaryings dnVert(DNAttributes IN)
            {
                DNVaryings OUT;
                OUT.positionCS = TransformObjectToHClip(Breathe(IN.positionOS.xyz));
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 dnFrag(DNVaryings IN) : SV_Target
            {
                half a = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).a * _BaseColor.a;
                clip(a - _Cutoff);
                return half4(normalize(IN.normalWS) * 0.5 + 0.5, 0);
            }
            ENDHLSL
        }
    }

    Fallback "Universal Render Pipeline/Unlit"
}
