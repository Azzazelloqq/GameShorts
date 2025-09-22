Shader "Custom/GrassDots2D"
{
    Properties
    {
        // Стандартные свойства SpriteRenderer
        [PerRendererData] [MainTexture] _BaseMap ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _BaseColor ("Tint", Color) = (1,1,1,1)

        // Наши цвета
        _BgColor ("Background", Color) = (0.05, 0.2, 0.05, 1)
        _BladeColor ("Blade Color", Color) = (0.6, 1, 0.6, 1)

        // Геометрия/настройки в «пикселях» квадрата
        _QuadPx ("Quad Size (px)", Vector) = (10,10,0,0)
        _DotRadiusPx ("Dot Radius (px)", Float) = 1.1
        _LineWidthPx ("Line Width (px)", Float) = 0.45 // было 0.6
        _LineLengthPx ("Line Length (px)", Float) = 5.5

        _SwayAmpPx ("Sway Amp (px)", Float) = 0.8
        _SwayFreq ("Sway Freq", Float) = 3.0
        _SwayPhase ("Sway Phase", Float) = 0.0

        _BladeCountMin ("Blades Min", Float) = 3
        _BladeCountMax ("Blades Max", Float) = 5

        _Seed ("Seed", Float) = 1.0
        _Shrink ("Shrink 0..1", Range(0,1)) = 0
        _TimeScale ("Time Scale", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" "DisableBatching"="False"
        }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Текстура/самплер спрайта
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR; // цвет от SpriteRenderer (Tint/вертексный)
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            float4 _BaseColor;

            float4 _BgColor, _BladeColor;
            float4 _QuadPx;
            float _DotRadiusPx, _LineWidthPx, _LineLengthPx;
            float _SwayAmpPx, _SwayFreq, _SwayPhase;
            float _BladeCountMin, _BladeCountMax;
            float _Seed, _Shrink, _TimeScale;

            // Хэши
            float hash11(float x)
            {
                x = frac(sin(x * 127.1) * 43758.5453);
                return frac(x);
            }

            float2 hash21(float x)
            {
                float n = sin(x * 12.9898 + 78.233);
                return float2(frac(n * 43758.5453), frac(n * 9631.4172));
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = TransformObjectToHClip(v.vertex.xyz);
                o.uv = v.uv;
                o.color = v.color * _BaseColor; // умножим Tint
                return o;
            }

            float4 frag(v2f i):SV_Target
            {
                float2 px = i.uv * _QuadPx.xy;
                float time = _TimeParameters.y * _TimeScale;

                // Семплим спрайт (можно просто белый 1x1)
                float4 baseTex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, i.uv);

                int bmin = clamp((int)round(_BladeCountMin), 1, 5);
                int bmax = clamp((int)round(_BladeCountMax), 1, 5);
                if (bmax < bmin)
                {
                    int t = bmin;
                    bmin = bmax;
                    bmax = t;
                }
                int bladeCount = bmin + (int)floor(hash11(_Seed + 0.123) * (bmax - bmin + 1));
                bladeCount = clamp(bladeCount, 1, 5);

                float baseLenPx = lerp(_LineLengthPx, 0.0, saturate(_Shrink));
                float bladeMask = 0.0;

                [unroll(5)]
                for (int k = 0; k < 5; k++)
                {
                    if (k >= bladeCount) break;

                    float id = _Seed * 17.0 + k * 3.14159;
                    float2 rp = hash21(id);

                    float lenPx = baseLenPx * (0.9 + 0.2 * rp.x);
                    float baseX = rp.x * _QuadPx.x;
                    float baseY = rp.y * max(_QuadPx.y - lenPx, 0.0);

                    float phase = rp.y * 6.28318 + _SwayPhase;

                    // Точка
                    float2 d2 = px - float2(baseX, baseY);
                    float dotMask = smoothstep(_DotRadiusPx, _DotRadiusPx - 1.0, length(d2));

                    // Стебель
                    float y0 = baseY, y1 = baseY + lenPx;
                    float inY = step(y0, px.y) * step(px.y, y1);
                    float t = (lenPx > 1e-4) ? saturate((px.y - y0) / max(lenPx, 1e-5)) : 0.0;
                    float sway = _SwayAmpPx * sin(_SwayFreq * time + phase + t * 2.5);
                    float centerX = baseX + sway * t;

                    float halfW = 0.5 * _LineWidthPx;
                    float aa = max(fwidth(px.x), 1.0); // ≈ 1 экранный пиксель AA
                    float xdist = abs(px.x - centerX);
                    float lineSoft = 1.0 - smoothstep(halfW, halfW + aa, xdist);

                    float lineM = inY * lineSoft;
                    bladeMask = max(bladeMask, max(dotMask, lineM));
                }

                // Смешиваем наш процедурный слой с базовой текстурой и цветом спрайта
                float4 grassCol = lerp(_BgColor, _BladeColor, saturate(bladeMask));
                float4 outCol = grassCol * baseTex * i.color; // учитываем спрайт и Tint
                return outCol;
            }
            ENDHLSL
        }
    }
}