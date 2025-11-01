Shader "UI/GooeyJoystick"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        
        // Metaball/Gooey effect parameters
        _Circle1Pos ("Circle 1 Position", Vector) = (0.5, 0.5, 0, 0)
        _Circle2Pos ("Circle 2 Position", Vector) = (0.5, 0.5, 0, 0)
        _Circle1Radius ("Circle 1 Radius", Float) = 0.2
        _Circle2Radius ("Circle 2 Radius", Float) = 0.1
        _Threshold ("Threshold", Range(0, 1)) = 0.5
        _Smoothness ("Smoothness", Range(0, 0.5)) = 0.1
        
        // Unity UI properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            
            float2 _Circle1Pos;
            float2 _Circle2Pos;
            float _Circle1Radius;
            float _Circle2Radius;
            float _Threshold;
            float _Smoothness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            // Metaball field function
            float metaballField(float2 pos, float2 center, float radius)
            {
                float dist = distance(pos, center);
                // Using inverse square falloff for smooth metaball effect
                return (radius * radius) / (dist * dist + 0.0001);
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                
                // Calculate metaball field for both circles
                float field1 = metaballField(uv, _Circle1Pos, _Circle1Radius);
                float field2 = metaballField(uv, _Circle2Pos, _Circle2Radius);
                
                // Combine fields
                float combinedField = field1 + field2;
                
                // Apply threshold with smoothstep for smooth edges
                float alpha = smoothstep(_Threshold - _Smoothness, _Threshold + _Smoothness, combinedField);
                
                // Sample texture (if needed for additional effects)
                half4 color = tex2D(_MainTex, uv);
                
                // Apply color and alpha
                color *= IN.color;
                color.a *= alpha;
                
                // Clip based on UI rect
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                
                // Discard fully transparent pixels
                clip(color.a - 0.001);
                
                return color;
            }
            ENDCG
        }
    }
}

