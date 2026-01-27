Shader "Custom/GrassDots2D_V2"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [PerRendererData] _Color ("Tint", Color) = (1,1,1,1)
        
        _BgColor ("Background Color", Color) = (0.1, 0.3, 0.1, 1)
        _GrassColor ("Grass Color", Color) = (0.5, 1, 0.5, 1)
        
        _GrassCount ("Grass Count", Range(3, 10)) = 4
        _GrassLength ("Grass Length", Range(4, 20)) = 5
        _GrassWidth ("Grass Width", Range(0.5, 1.5)) = 0.8
        _DotSize ("Dot Size", Range(0.2, 1.5)) = 1.0
        
        _SwayAmount ("Sway Amount", Range(0.5, 2.0)) = 1.0
        _SwaySpeed ("Sway Speed", Range(1.0, 4.0)) = 2.0
        
        _Shrink ("Shrink", Range(0, 1)) = 0
        _Seed ("Seed", Float) = 1.0
        
        _TopCutoffHeight ("Top Cutoff Height", Range(0, 1)) = 0.25
        _Quality ("Quality", Range(0, 1)) = 1.0
        _UseAlphaGradient ("Use Alpha Gradient", Range(0, 1)) = 1.0
        _Simplify ("Simplify", Range(0, 1)) = 0.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent" 
            "IgnoreProjector"="True" 
            "CanUseSpriteAtlas"="True"
        }
        
        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;

            CBUFFER_START(UnityPerMaterial)
                fixed4 _Color;
                fixed4 _BgColor;
                fixed4 _GrassColor;
                
                float _GrassCount;
                float _DotSize;
                float _SwayAmount;
                float _SwaySpeed;
                float _TopCutoffHeight;
                float _Quality;
                float _UseAlphaGradient;
                float _Simplify;
                
                // Обычные переменные (всегда доступны)
                float _Seed;
                float _Shrink;
                float _GrassLength;
                float _GrassWidth;
            CBUFFER_END

            // GPU Instancing support
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceSeed)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceShrink)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceGrassLength)
                UNITY_DEFINE_INSTANCED_PROP(float, _InstanceGrassWidth)
            UNITY_INSTANCING_BUFFER_END(Props)
            
            // Функции для получения значений (совместимые с instancing и без него)
            float GetSeed() {
                #ifdef UNITY_INSTANCING_ENABLED
                    return UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceSeed);
                #else
                    return _Seed;
                #endif
            }
            
            float GetShrink() {
                #ifdef UNITY_INSTANCING_ENABLED
                    return UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceShrink);
                #else
                    return _Shrink;
                #endif
            }
            
            float GetGrassLength() {
                #ifdef UNITY_INSTANCING_ENABLED
                    return UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceGrassLength);
                #else
                    return _GrassLength;
                #endif
            }
            
            float GetGrassWidth() {
                #ifdef UNITY_INSTANCING_ENABLED
                    return UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceGrassWidth);
                #else
                    return _GrassWidth;
                #endif
            }

            // Простая хэш-функция
            float random(float2 st) 
            {
                return frac(sin(dot(st.xy, float2(12.9898, 78.233))) * 43758.5453123);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                
                // Получаем свойства (instanced или обычные)
                float instanceSeed = GetSeed();
                float instanceShrink = GetShrink();
                float instanceGrassLength = GetGrassLength();
                float instanceGrassWidth = GetGrassWidth();
                
                // Координаты в пикселях (32x32 квадрат)
                float2 pixelPos = IN.texcoord * 32.0;
                half time = (half)_Time.y * (half)_SwaySpeed;
                
                // Базовый цвет фона с учетом обрезания
                float cutoffLine = 1.0 - _TopCutoffHeight;
                fixed4 backgroundColor = _BgColor;
                if (IN.texcoord.y > cutoffLine)
                {
                    backgroundColor.a = 0.0; // Фон прозрачный в верхней части
                }
                fixed4 finalColor = backgroundColor;
                
                // Количество травинок
                half quality = saturate((half)_Quality);
                int grassCount = (int)_GrassCount;
                int minGrassCount = max(1, grassCount / 2);
                grassCount = (int)lerp((float)minGrassCount, (float)grassCount, quality);
                
                // Рисуем каждую травинку
                for(int i = 0; i < 10; i++)
                {
                    if(i >= grassCount) break;
                    
                    // Уникальное семя для каждой травинки
                    float grassSeed = instanceSeed + i * 17.3;
                    
                    // Длина травинки с учетом shrink
                    float currentLength = instanceGrassLength * (1.0 - instanceShrink);
                    currentLength *= (0.8 + 0.4 * random(float2(grassSeed, 3.0))); // Вариация длины
                    
                    // Позиция основания с учетом длины стебля, покачивания и верхней обрезки
                    float cutoffLinePixels = (1.0 - _TopCutoffHeight) * 32.0; // Линия обрезки в пикселях
                    float maxY = min(max(32.0 - currentLength, 0.0), cutoffLinePixels); // Учитываем обрезку
                    float swayMargin = _SwayAmount + 2.0; // Запас для покачивания
                    float minX = swayMargin;
                    float maxX = 32.0 - swayMargin;
                    
                    float2 basePos = float2(
                        minX + random(float2(grassSeed, 1.0)) * (maxX - minX), // С учетом покачивания
                        random(float2(grassSeed, 2.0)) * maxY  // Ограничиваем по высоте и обрезке
                    );
                    
                    // Рисуем точку только когда стебель полностью исчез (currentLength <= 0.1)
                    if(currentLength <= 0.1)
                    {
                        float2 dotDist = pixelPos - basePos;
                        float maxDotSize = min(_DotSize, instanceGrassWidth);
                        float dotRadius = max(maxDotSize, 0.2); // Минимум 0.2
                        if(dot(dotDist, dotDist) < dotRadius * dotRadius)
                        {
                            finalColor = _GrassColor;
                        }
                    }
                    else
                    {
                        half swayPhase = (half)random(float2(grassSeed, 4.0)) * (half)6.2831853;
                        half swayBase = sin(time + swayPhase) * (half)_SwayAmount;
                        float invLength = 1.0 / max(currentLength, 0.1);
                        half stemWidthBase = (half)instanceGrassWidth;
                        // Проходим по высоте стебля
                        float stepSize = max(0.3, instanceGrassWidth * 0.6);
                        int maxSteps = (int)lerp(12.0, 24.0, quality);
                        int steps = (int)min(ceil(currentLength / stepSize), (float)maxSteps);
                        steps = max(steps, 1);
                        bool allowOpaqueEarlyExit = (_UseAlphaGradient < 0.5);
                        for(int s = 0; s <= steps; s++)
                        {
                            float y = min(currentLength, s * stepSize);
                            // Параметр от 0 до 1 по высоте стебля
                            half t = (half)(y * invLength);
                            
                            // Покачивание усиливается к верху
                            half sway = swayBase * t;
                            
                            // Позиция точки на стебле
                            float2 stemPos = basePos + float2(sway, y);
                            
                            // Проверяем расстояние до текущего пикселя
                            float2 stemDist = pixelPos - stemPos;
                            half stemWidth = stemWidthBase;
                            if(_Simplify < 0.5)
                            {
                                stemWidth = stemWidthBase * (half)(1.0 - t * 0.3); // Сужается к верху
                            }
                            
                            if(dot(stemDist, stemDist) < stemWidth * stemWidth)
                            {
                                if(_UseAlphaGradient >= 0.5)
                                {
                                    // Градиент прозрачности: снизу прозрачно, сверху непрозрачно
                                    half alpha = saturate(t * 2.0); // t от 0 до 0.5 дает alpha от 0 до 1
                                    fixed4 grassWithAlpha = _GrassColor;
                                    grassWithAlpha.a *= alpha;
                                    // Смешиваем с учетом альфы
                                    finalColor = lerp(finalColor, grassWithAlpha, alpha);
                                }
                                else
                                {
                                    finalColor = _GrassColor;
                                    if(allowOpaqueEarlyExit)
                                    {
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Применяем цвет спрайта и тинт
                fixed4 spriteColor = tex2D(_MainTex, IN.texcoord);
                finalColor *= spriteColor * IN.color;
                
                return finalColor;
            }
            ENDCG
        }
    }
}