// 接地の影ひとつぶん（2026-08-30）。
// 板の 大きさに よらず **世界の 長さで** ふちを ぼかしたい ので、
// 濃さは 頂点の 色（アルファ）で 持つ。テクスチャは つかわない。
Shader "Niwa/Kage" {
    Properties { }
    SubShader {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent-100"
               "RenderPipeline" = "UniversalPipeline" }
        Pass {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct A { float4 pos : POSITION; float4 col : COLOR; };
            struct V { float4 pos : SV_POSITION; float4 col : COLOR; };

            V vert(A i) {
                V o;
                o.pos = TransformObjectToHClip(i.pos.xyz);
                o.col = i.col;
                return o;
            }
            half4 frag(V i) : SV_Target { return half4(0, 0, 0, i.col.a); }
            ENDHLSL
        }
    }
    FallBack Off
}
