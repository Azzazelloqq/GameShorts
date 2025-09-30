Shader "Custom/CubeNumberUnlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1, 0.6, 0, 1)
        _DigitColor ("Digit Color", Color) = (1,1,1,1)

        _DigitsAtlas ("Digits Atlas (0-9)", 2D) = "white" {}
        _AtlasCols ("Atlas Columns", Float) = 5
        _AtlasRows ("Atlas Rows", Float) = 2

        _Number ("Number (0..10000)", Int) = 4
        _MaxDigits ("Max Digits", Int) = 5      // до 5 цифр

        _BlockScale ("Number Block Scale", Range(0.1,1.5)) = 0.85 // общий масштаб «плашки» числа
        _CharSpacing ("Char Spacing (-0.2..0.5)", Range(-0.2,0.5)) = 0.05  // доля ширины символа
        _Offset ("Block Offset (XY)", Vector) = (0,0,0,0)
    }

    SubShader
    {
        Tags{ "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100

        Pass
        {
            Name "Unlit"
            Tags{ "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
            };
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float3 posOS       : TEXCOORD0;
                float3 normalOS    : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _DigitColor;

                float4 _DigitsAtlas_ST;
                float  _AtlasCols;
                float  _AtlasRows;

                int    _Number;
                int    _MaxDigits;

                float  _BlockScale;
                float  _CharSpacing;
                float4 _Offset;
            CBUFFER_END

            TEXTURE2D(_DigitsAtlas); SAMPLER(sampler_DigitsAtlas);

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.posOS = IN.positionOS.xyz;
                OUT.normalOS = IN.normalOS;
                return OUT;
            }

            // Выбираем UV по доминирующей нормали (куб)
            float2 FaceUV(float3 posOS, float3 nOS)
            {
                float3 an = abs(nOS);
                float2 uv;
                if (an.x >= an.y && an.x >= an.z) uv = posOS.zy;      // на YZ
                else if (an.y >= an.x && an.y >= an.z) uv = posOS.xz;  // на XZ
                else uv = posOS.xy;                                    // на XY
                uv = uv + 0.5; // [-0.5..0.5] -> [0..1]
                return uv;
            }

            // Достаём цифру d (0..9) из атласа
            float SampleDigitA(float2 uv01, int d, float atlasCols, float atlasRows)
            {
                d = clamp(d, 0, 9);
                int tx = d % (int)atlasCols;
                int ty = d / (int)atlasCols;
                // инвертируем строку, если атлас читается снизу вверх
                ty = (int)atlasRows - 1 - ty;

                float2 tuv;
                tuv.x = (uv01.x + tx) / atlasCols;
                // без инверсии Y - проверяем правильный порядок рядов
                tuv.y = (uv01.y + ty) / atlasRows;

                float4 s = SAMPLE_TEXTURE2D(_DigitsAtlas, sampler_DigitsAtlas, tuv);
                return saturate(s.a); // альфа — маска цифры
            }

            // Безопасная степень 10 для целых
            int Pow10Int(int p)
            {
                int v = 1;
                [unroll] for (int i=0;i<9;i++) { if (i>=p) break; v*=10; }
                return v;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float2 uv = FaceUV(IN.posOS, IN.normalOS);

                // Центрируем/масштабируем общий блок числа
                float2 blockUV = (uv - 0.5) / max(_BlockScale, 1e-4) + 0.5 + _Offset.xy;

                // Если выходим за «плашку» — фон
                float3 baseCol = _BaseColor.rgb;
                float3 digitCol = _DigitColor.rgb;

                // Сколько цифр в числе
                int n = max(_Number, 0);
                int total = (n == 0) ? 1 : (int)floor(log2((float)n) * 0.30103) + 1; // log10 = log2 * log10(2)
                total = clamp(total, 1, max(_MaxDigits,1)); // ограничиваем сверху

                // Геометрия строки символов
                float spacing = _CharSpacing;          // доля ширины символа
                float charW = 1.0 / total;             // «ячейка» на символ
                float glyphW = charW * (1.0 - spacing);// фактическая ширина глифа
                float sidePad = (charW - glyphW) * 0.5;

                float aCombined = 0.0;

                // Локальные UV для всей строки [0..1]
                float2 uvl = blockUV;

                // Для каждого символа слева-направо
                [unroll]
                for (int i = 0; i < min(total, 6); i++)
                {

                    // Извлекаем цифру: для позиции i (слева направо) берём соответствующую цифру
                    int digitPos = total - 1 - i; // позиция справа (0 = единицы, 1 = десятки, и т.д.)
                    int pow10 = Pow10Int(digitPos);
                    int digit = (n / pow10) % 10;

                    // Диапазон по X для текущего символа
                    float start = i * charW + sidePad;
                    float end   = start + glyphW;

                    // Проверяем, попадает ли текущая точка пикселя в «окно» этого символа
                    float inside = step(start, uvl.x) * step(uvl.x, end) * step(0.0, uvl.y) * step(uvl.y, 1.0);

                    if (inside > 0.5)
                    {
                        // Нормируем в [0..1] внутри символа
                        float2 uvChar;
                        uvChar.x = saturate((uvl.x - start) / max(glyphW, 1e-5));
                        uvChar.y = saturate(uvl.y);

                        // Сэмплим атлас
                        float a = SampleDigitA(uvChar, digit, _AtlasCols, _AtlasRows);
                        aCombined = max(aCombined, a);
                    }
                }

                float3 col = lerp(baseCol, digitCol, aCombined);
                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }
}
