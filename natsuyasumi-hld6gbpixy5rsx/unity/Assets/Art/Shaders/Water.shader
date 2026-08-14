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

            // 段を つけた うねり。なめらかに すると 写真ふうに なるので しきいで 刻む
            float Ripple(float2 uv, float t)
            {
                float a = sin((uv.y * 3.1 + uv.x * 1.7) * _Scale - t * 2.1);
                float b = sin((uv.y * 5.7 - uv.x * 2.9) * _Scale * 0.7 + t * 1.3);
                return a * 0.6 + b * 0.4;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float t = _Time.y * _Speed;
                float r = Ripple(IN.uv, t);
                // 3段に 刻む＝ドット絵の 階調
                float step3 = floor(saturate(r * 0.5 + 0.5) * 3.0) / 2.0;

                half3 col = lerp(_Shallow.rgb, _Deep.rgb, saturate(IN.depth));
                col = lerp(col, col * 1.14, step3);

                // 岸ぎわの 白い すじ（あわ）
                half edge = 1.0 - saturate(IN.depth * 2.2);
                half foam = saturate(edge * (0.45 + 0.55 * step3));
                col = lerp(col, _Foam.rgb, foam * 0.55);

                // きらきら。太陽の むきに 合わせて 点で 出す
                Light mainLight = GetMainLight();
                half sun = saturate(dot(half3(0, 1, 0), mainLight.direction));
                half sp = step(0.86, saturate(r * 0.5 + 0.5)) * _Sparkle * sun;
                col += sp * mainLight.color * 0.8;

                half a = lerp(_Shallow.a, _Deep.a, saturate(IN.depth));
                col = MixFog(col, IN.fogFactor);
                return half4(col, saturate(a + foam * 0.25));
            }
            ENDHLSL
        }
    }
    Fallback Off
}
