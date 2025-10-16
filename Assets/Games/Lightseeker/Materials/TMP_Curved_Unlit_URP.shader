Shader "Universal Render Pipeline/TMP Curved (Unlit)"
{
    Properties
    {
        _MainTex      ("Font Atlas (SDF)", 2D) = "white" {}
        _FaceColor    ("Face Color", Color) = (1,1,1,1)
        _OutlineColor ("Outline Color", Color) = (0,0,0,1)
        _OutlineWidth ("Outline Width", Range(0,0.5)) = 0.1
        _Softness     ("Edge Softness", Range(0.001,0.1)) = 0.02
        _FaceDilate   ("Face Dilate", Range(-0.5,0.5)) = 0.0

        _Curvature    ("Curvature (1/R)", Float) = 0.000001
        _UseCamera    ("Use Camera Center (0/1)", Float) = 1
        _BendCenter   ("Bend Center (World)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags{
            "RenderPipeline"="UniversalRenderPipeline"
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
        }
        Cull Off
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "ForwardUnlit"
            Tags{ "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   3.5
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_ST;

            float4 _FaceColor;
            float4 _OutlineColor;
            float  _OutlineWidth;
            float  _Softness;
            float  _FaceDilate;

            float  _Curvature;
            float  _UseCamera;     // 0 = BendCenter, 1 = Camera
            float4 _BendCenter;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
                half fogFactor     : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 wp = TransformObjectToWorld(IN.positionOS.xyz);

                float3 center = lerp(_BendCenter.xyz, _WorldSpaceCameraPos, saturate(_UseCamera));
                float2 dxz = wp.xz - center.xz;
                float  r2  = dot(dxz, dxz);
                float  bend = -r2 * (_Curvature / 100);   // use negative to bend "down"
                wp.y += bend;

                OUT.positionHCS = TransformWorldToHClip(wp);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _FaceColor;
                OUT.fogFactor = ComputeFogFactor(OUT.positionHCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float4 s = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);
                float dist = s.a;

                float mid = 0.5 + _FaceDilate;
                float2 g = float2(ddx(dist), ddy(dist));
                float fw = length(g) + _Softness;

                float face = smoothstep(mid - fw, mid + fw, dist);

                float edgeMid = mid - _OutlineWidth;
                float edge = smoothstep(edgeMid - fw, edgeMid + fw, dist);

                float3 rgb = lerp(_OutlineColor.rgb, IN.color.rgb, face);
                float  alpha = max(edge, face) * IN.color.a;

                half4 col = half4(rgb, alpha);
                col.rgb = MixFog(col.rgb, IN.fogFactor);
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}