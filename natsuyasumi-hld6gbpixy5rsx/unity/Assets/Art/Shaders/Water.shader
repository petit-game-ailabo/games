// 小川と 川の 水面。
//
// ドット絵の 世界に 合わせて、**にじませない・きらきらは 粒で 出す**。
// 写真ふうの 反射は 入れない（まわりと ちぐはぐに なる）。
//  - 底の 色が すける（浅い ところは 明るく、深い ところは 濃く）
//  - さざなみ＝2枚の うねりを ずらして 重ね、しきいで 段に する（＝ドット絵の 階調）
//  - 流れの 向きへ ゆっくり ながれる
Shader "Natsuyasumi/Water"
{
    Properties
    {
        _Shallow("Shallow", Color) = (0.45,0.72,0.70,0.62)
        _Deep("Deep", Color) = (0.10,0.34,0.40,0.88)
        _Foam("Foam", Color) = (0.90,0.96,0.94,1)
        _Speed("Flow speed", Float) = 0.35
        _Scale("Ripple scale", Float) = 2.4
        _Sparkle("Sparkle", Range(0,1)) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Shallow, _Deep, _Foam;
                float _Speed, _Scale;
                half  _Sparkle;
            CBUFFER_END

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;         // r = 深さ 0(岸)〜1(まん中)
                float2 uv : TEXCOORD0;        // x = 岸からの ずれ, y = 流れに そった 距離
            };
            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
                half   depth : TEXCOORD2;
                half   fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                VertexPositionInputs p = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = p.positionCS;
                OUT.positionWS = p.positionWS;
                OUT.uv = IN.uv;
                OUT.depth = IN.color.r;
                OUT.fogFactor = ComputeFogFactor(p.positionCS.z);
                return OUT;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            // かどの とれた ゆらぎ（値の noise）
            float Noise2(float2 p)
            {
                float2 i = floor(p), f = frac(p);
                f = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1, 0));
                float c = Hash21(i + float2(0, 1));
                float d = Hash21(i + float2(1, 1));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            // 段を つけた うねり。なめらかに すると 写真ふうに なるので しきいで 刻む。
            // ★**すじが そろうと 川では なく エスカレーターに 見える。**
            //   波を 2枚 かさねるだけだと きれいな 格子が 出て、高台から 見おろした とき
            //   白い 矢じるしが ならんだ ベルトコンベアに 見えた。ゆらぎを まぜて 崩す
            float Ripple(float2 uv, float t)
            {
                float2 q = uv * _Scale;
                float n = Noise2(q * 1.7 + float2(0.0, -t * 0.8)) * 2.0 - 1.0;
                float a = sin((q.y * 3.1 + q.x * 1.7) - t * 2.1 + n * 1.6);
                float b = sin((q.y * 5.7 - q.x * 2.9) * 0.7 + t * 1.3 + n * 1.1);
                return a * 0.5 + b * 0.3 + n * 0.4;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _Speed;
                float r = Ripple(IN.uv, t);
                // 3段に 刻む＝ドット絵の 階調
                float step3 = floor(saturate(r * 0.5 + 0.5) * 3.0) / 2.0;

                half3 col = lerp(_Shallow.rgb, _Deep.rgb, saturate(IN.depth));
                col = lerp(col, col * 1.14, step3);

                // 岸ぎわの 白い すじ（あわ）。
                // **岸から 2.2 ぶんも 白く すると 川幅の 半分が あわに なる。**
                // 実さい 水面が ほとんど 白っぽく 見えて いた。ふちの 細い すじに とどめる
                half edge = 1.0 - saturate(IN.depth * 4.2);
                half foam = saturate(edge * (0.40 + 0.60 * step3));
                col = lerp(col, _Foam.rgb, foam * 0.45);

                // きらきら。太陽の むきに 合わせて **点で ちらす**。
                // うねりの しきいだけで 出すと、うねりの すじに そって 一列に ならぶ
                Light mainLight = GetMainLight();
                half sun = saturate(dot(half3(0, 1, 0), mainLight.direction));
                // **しきいを ゆるく すると 水面が 雪に なる。**
                // 面の 1割も 白く すると 一面の 粒に なった。細かく・まばらに 出す
                half gate = step(0.955, Noise2(IN.uv * _Scale * 11.0 + float2(0.0, -t * 1.4)));
                half sp = gate * step(0.70, saturate(r * 0.5 + 0.5)) * _Sparkle * sun;
                col += sp * mainLight.color * 0.6;

                half a = lerp(_Shallow.a, _Deep.a, saturate(IN.depth));
                col = MixFog(col, IN.fogFactor);
                return half4(col, saturate(a + foam * 0.25));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
