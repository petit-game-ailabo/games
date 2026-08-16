// 建物ようの Lit。**主人公の まわりだけ ディザで 抜く。**
//
// ★なぜ 要るか（2026-08-16・本人「3Dのゲームでカメラが建物に邪魔された時ってどうするもの？」）
//   板の 草木・電柱には もともと 穴を あける しかけが あった（PixelSprite の ClipHole）が、
//   **家や 塀は URP/Lit で 描いて いた ので 対象外**だった。だから 家の 裏へ まわると
//   カメラが 壁に めりこみ、画面が 壁 1色に なって いた。
//
// ★3Dゲームで ふつうに つかわれる 手は 4つ。
//   1) カメラと 主人公の あいだの 物を 消す／すかす（＝これ）
//   2) 当たったら カメラを 手前に 引き寄せる（Cinemachine の Deoccluder）
//   3) **ディザ(市松)で 穴を あける**＝半とうめいより 安く、ドット絵とも 相性が よい
//   4) 屋内は べつの カメラ・べつの 見おろしに 切りかえる
//   この 企画は **1と3の 合わせ技**。すでに ある `_HoleParams`（主人公の 画面での 位置・
//   大きさ・奥ゆき）を そのまま つかう ので、レイを 飛ばす 必要も ない。
//   **主人公より 手前に ある もの だけ** 抜ける ように なって いる。
//
// 光の あつかいは URP の UniversalFragmentPBR に まかせる（自前で 書くと
// ほかの 物と 明るさが そろわない）。
Shader "Natsuyasumi/DitherLit"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Map", 2D) = "white" {}
        [MainColor]   _BaseColor("Base Color", Color) = (1,1,1,1)
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 0.1
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1
        [HDR] _EmissionColor("Emission", Color) = (0,0,0,0)
        [Toggle] _UseEmission("Use Emission", Float) = 0
        // 1 に すると **抜かれない**（虫や、抜かれると 困る もの）
        [Toggle] _HoleIgnore("Ignore see-through hole", Float) = 0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull", Float) = 2
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4  _BaseColor;
            half   _Metallic;
            half   _Smoothness;
            half   _BumpScale;
            half4  _EmissionColor;
            half   _UseEmission;
            half   _HoleIgnore;
        CBUFFER_END

        float4 _HoleParams;   // xy=画面での 主人公の 位置 z=穴の 半径 w=主人公の 奥ゆき

        // **主人公の まわりを ちらして 抜く。**
        // 主人公より おくに ある ものは 抜かない（抜くと 部屋の おくが 素どおしに なる）
        void ClipHole(float4 positionCS, float3 positionWS)
        {
            if (_HoleParams.z <= 0.0001 || _HoleIgnore > 0.5) return;
            float viewZ = -TransformWorldToView(positionWS).z;
            if (viewZ >= _HoleParams.w - 0.35) return;

            float2 d = GetNormalizedScreenSpaceUV(positionCS) - _HoleParams.xy;
            d.x *= _ScreenParams.x / max(_ScreenParams.y, 1.0);   // 丸く 抜く
            float t = saturate(length(d) / _HoleParams.z);        // 0=まん中 1=ふち
            // まん中は まるごと 抜き、ふちは ちらす。**ちらす 帯は せまく**
            //（広いと ざらざらが 目だって 画が よごれる。板の ほうと 同じ 値に そろえる）
            float2 px = floor(positionCS.xy * 0.5);
            float dither = frac(sin(dot(px, float2(12.9898, 78.233))) * 43758.5453);
            clip(t - (0.80 + dither * 0.20));
        }
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            // ★URP 17.5 は **_CLUSTER_LIGHT_LOOP**（_FORWARD_PLUS では ない）
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma shader_feature_local _NORMALMAP

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float4 tangentWS  : TEXCOORD2;
                float2 uv         : TEXCOORD3;
                float  fogFactor  : TEXCOORD4;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs   n = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.normalWS   = n.normalWS;
                OUT.tangentWS  = float4(n.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
                OUT.uv         = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor  = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                ClipHole(IN.positionCS, IN.positionWS);

                half4 baseCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;

                float3 N = normalize(IN.normalWS);
                #if defined(_NORMALMAP)
                    half3 nTS = UnpackNormalScale(
                        SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);
                    float3 B = cross(N, IN.tangentWS.xyz) * IN.tangentWS.w;
                    N = normalize(mul(nTS, float3x3(IN.tangentWS.xyz, B, N)));
                #endif

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.normalWS = N;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = IN.fogFactor;
                inputData.bakedGI = SampleSH(N);
                inputData.positionCS = IN.positionCS;
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);

                SurfaceData sd = (SurfaceData)0;
                sd.albedo = baseCol.rgb;
                sd.alpha = 1.0;
                sd.metallic = _Metallic;
                sd.smoothness = _Smoothness;
                sd.occlusion = 1.0;
                sd.normalTS = half3(0, 0, 1);
                sd.emission = _UseEmission > 0.5 ? _EmissionColor.rgb : half3(0, 0, 0);

                half4 col = UniversalFragmentPBR(inputData, sd);
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return half4(col.rgb, 1.0);
            }
            ENDHLSL
        }

        // 影を おとす。**穴は ここでも あける。**
        // あけないと 抜いた はずの 壁の 影だけ 残って、床に 四角い 影が うかぶ
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex shadowVert
            #pragma fragment shadowFrag
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            struct SAttr { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct SVar  { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            SVar shadowVert(SAttr IN)
            {
                SVar OUT;
                float3 posWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 nrmWS = TransformObjectToWorldNormal(IN.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 L = normalize(_LightPosition - posWS);
                #else
                    float3 L = _LightDirection;
                #endif
                float4 cs = TransformWorldToHClip(ApplyShadowBias(posWS, nrmWS, L));
                #if UNITY_REVERSED_Z
                    cs.z = min(cs.z, UNITY_NEAR_CLIP_VALUE);
                #else
                    cs.z = max(cs.z, UNITY_NEAR_CLIP_VALUE);
                #endif
                OUT.positionCS = cs;
                OUT.positionWS = posWS;
                return OUT;
            }

            half4 shadowFrag(SVar IN) : SV_Target
            {
                ClipHole(IN.positionCS, IN.positionWS);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On ColorMask 0 Cull [_Cull]

            HLSLPROGRAM
            #pragma vertex depthVert
            #pragma fragment depthFrag

            struct DAttr { float4 positionOS : POSITION; };
            struct DVar  { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            DVar depthVert(DAttr IN)
            {
                DVar OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                return OUT;
            }
            half4 depthFrag(DVar IN) : SV_Target
            {
                ClipHole(IN.positionCS, IN.positionWS);
                return 0;
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
