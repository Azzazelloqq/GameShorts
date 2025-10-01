Shader "Custom/ScreenGradientBackground"
{
    Properties {
        _TopColor ("Top Color", Color) = (0.1,0.3,0.8,1)
        _BottomColor ("Bottom Color", Color) = (0.8,0.9,1,1)
        _Power ("Falloff", Range(0.1,5)) = 1
        _Horizontal ("Horizontal (0/1)", Float) = 0
    }
    SubShader {
        Tags { "Queue"="Background" "RenderType"="Opaque" }
        Cull Off
        ZWrite Off
        ZTest Always
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            fixed4 _TopColor, _BottomColor; float _Power; float _Horizontal;

            struct appdata { float4 vertex:POSITION; float2 uv:TEXCOORD0; };
            struct v2f { float4 pos:SV_POSITION; float2 uv:TEXCOORD0; };

            v2f vert (appdata v){ v2f o; o.pos = UnityObjectToClipPos(v.vertex); o.uv = v.uv; return o; }

            fixed4 frag (v2f i) : SV_Target {
                float t = _Horizontal > 0.5 ? i.uv.x : i.uv.y;   // верт/гориз.
                t = saturate(pow(t, _Power));
                return lerp(_BottomColor, _TopColor, t);
            }
            ENDCG
        }
    }
}
