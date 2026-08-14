// 空と 遠くの 山なみ。
//
// **のっぺりした 灰色を やめる。** 単色で 塗ると 地平線から さきが
// 「何も ない ところ」に 見えて、山ぎわの 場面が 宙に 浮く。
//
// 絵は 置かずに 計算で 描く：
//  - たての むきで 空の 色を 変える（地平線は うすく、上ほど こい）
//  - **遠くの 山なみを 3枚 かさねる。** 奥の 列ほど 空の 色に 溶かす＝空気遠近
//  - うっすら 雲。ゆっくり ながれる
// 色は TimeOfDay が 時間帯ごとに 入れかえる。
Shader "Natsuyasumi/Sky"
{
    Properties
    {
        _Zenith("Zenith", Color) = (0.32,0.55,0.86,1)
        _Horizon("Horizon", Color) = (0.78,0.87,0.95,1)
        _Ridge("Ridge base", Color) = (0.36,0.45,0.42,1)
        _CloudColor("Cloud", Color) = (1,1,1,1)
        _CloudAmount("Cloud amount", Range(0,1)) = 0.35
        _RidgeHeight("Ridge height", Range(0,0.5)) = 0.13
        _Haze("Haze", Range(0,1)) = 0.7
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background"
               "PreviewType" = "Skybox" "RenderPipeline" = "UniversalPipeline" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Zenith, _Horizon, _Ridge, _CloudColor;
                half  _CloudAmount, _RidgeHeight, _Haze;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 dir : TEXCOORD0; };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.dir = IN.positionOS.xyz;      // 空の 玉なので 位置＝むき
                return OUT;
            }

            // 波を かさねて でこぼこの 稜線を 作る（絵を 用意しない ため）
            float Ridgeline(float a, float seed)
            {
                return sin(a * 1.7 + seed) * 0.55
                     + sin(a * 3.3 - seed * 1.7) * 0.28
                     + sin(a * 7.1 + seed * 0.6) * 0.12
                     + sin(a * 13.0 - seed) * 0.05;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 d = normalize(IN.dir);
                float el = d.y;                                  // -1(下) 〜 1(上)
                float az = atan2(d.z, d.x);                      // まわりの むき

                // --- 空。地平線から 上へ ゆっくり こくなる
                float t = saturate(el);
                half3 sky = lerp(_Horizon.rgb, _Zenith.rgb, pow(t, 0.55));

                // --- 雲。よこに ながれる うすい 帯
                float cl = sin(az * 3.1 + _Time.y * 0.02) * 0.5 + sin(az * 6.7 - _Time.y * 0.013) * 0.3
                         + sin(az * 11.0 + _Time.y * 0.008) * 0.2;
                float band = smoothstep(0.02, 0.42, el) * (1.0 - smoothstep(0.42, 0.95, el));
                float cloud = saturate(cl * 0.5 + 0.5 - (1.0 - _CloudAmount)) * band;
                sky = lerp(sky, _CloudColor.rgb, saturate(cloud * 1.4));

                // --- 遠くの 山なみ。3枚。奥ほど 低く・空の 色に 溶ける
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    float depth = 1.0 - i * 0.34;                          // 手前ほど 1
                    float h = _RidgeHeight * (0.45 + 0.55 * depth)
                            * (0.62 + 0.38 * Ridgeline(az * (1.0 + i * 0.45), 2.1 + i * 3.7));
                    // 稜線より 下なら 山
                    float m = 1.0 - smoothstep(h - 0.006, h + 0.006, el);
                    // 空気遠近：奥の 山ほど 空の 色に 近づく
                    half3 ridgeCol = lerp(sky, _Ridge.rgb * (0.72 + 0.28 * depth), _Haze * depth);
                    sky = lerp(sky, ridgeCol, m);
                }

                // --- 地平線より 下。**暗く 落とさない。**
                // 山の 色に 落として いたら、地形の 切れめの むこうが 黒くなり
                // 崖のように 見えた。遠くの 地めんは かすんで 地平線の 色に なる のが 本当
                sky = lerp(sky, lerp(_Horizon.rgb, _Ridge.rgb, 0.35),
                           smoothstep(0.0, -0.16, el));
                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
