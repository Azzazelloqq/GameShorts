using UnityEngine;

namespace Code.Games
{
    /// <summary>
    /// Менеджер для управления цветами кубиков в зависимости от их значения
    /// </summary>
    [CreateAssetMenu(fileName = "CubeColorManager", menuName = "2048/Cube Color Manager")]
    internal class CubeColorManager : ScriptableObject
    {
        [System.Serializable]
        internal struct CubeColorScheme
        {
            public int number;
            public Color baseColor;
            public Color digitColor;
            public Color outlineColor;
        }
        
        [SerializeField] private CubeColorScheme[] colorSchemes = new CubeColorScheme[]
        {
            new CubeColorScheme { number = 2, baseColor = new Color(0.93f, 0.89f, 0.85f), digitColor = new Color(0.47f, 0.43f, 0.4f), outlineColor = new Color(0.2f, 0.2f, 0.2f) },
            new CubeColorScheme { number = 4, baseColor = new Color(0.93f, 0.88f, 0.78f), digitColor = new Color(0.47f, 0.43f, 0.4f), outlineColor = new Color(0.2f, 0.2f, 0.2f) },
            new CubeColorScheme { number = 8, baseColor = new Color(0.95f, 0.69f, 0.47f), digitColor = Color.white, outlineColor = new Color(0.1f, 0.1f, 0.1f) },
            new CubeColorScheme { number = 16, baseColor = new Color(0.96f, 0.58f, 0.39f), digitColor = Color.white, outlineColor = new Color(0.1f, 0.1f, 0.1f) },
            new CubeColorScheme { number = 32, baseColor = new Color(0.96f, 0.49f, 0.37f), digitColor = Color.white, outlineColor = new Color(0.1f, 0.1f, 0.1f) },
            new CubeColorScheme { number = 64, baseColor = new Color(0.96f, 0.37f, 0.23f), digitColor = Color.white, outlineColor = new Color(0.1f, 0.1f, 0.1f) },
            new CubeColorScheme { number = 128, baseColor = new Color(0.93f, 0.81f, 0.45f), digitColor = Color.white, outlineColor = new Color(0.1f, 0.1f, 0.1f) },
            new CubeColorScheme { number = 256, baseColor = new Color(0.93f, 0.8f, 0.38f), digitColor = Color.white, outlineColor = new Color(0.1f, 0.1f, 0.1f) },
            new CubeColorScheme { number = 512, baseColor = new Color(0.93f, 0.78f, 0.31f), digitColor = Color.white, outlineColor = new Color(0.1f, 0.1f, 0.1f) },
            new CubeColorScheme { number = 1024, baseColor = new Color(0.93f, 0.77f, 0.25f), digitColor = Color.white, outlineColor = new Color(0.1f, 0.1f, 0.1f) },
            new CubeColorScheme { number = 2048, baseColor = new Color(0.93f, 0.76f, 0.18f), digitColor = Color.white, outlineColor = new Color(0.1f, 0.1f, 0.1f) }
        };
        
        [SerializeField] private CubeColorScheme defaultScheme = new CubeColorScheme 
        { 
            number = 0, 
            baseColor = new Color(0.8f, 0.8f, 0.8f), 
            digitColor = Color.black, 
            outlineColor = new Color(0.3f, 0.3f, 0.3f) 
        };
        
        /// <summary>
        /// Получает цветовую схему для указанного числа
        /// </summary>
        public CubeColorScheme GetColorScheme(int number)
        {
            foreach (var scheme in colorSchemes)
            {
                if (scheme.number == number)
                    return scheme;
            }
            
            // Если точное совпадение не найдено, возвращаем схему по умолчанию
            return defaultScheme;
        }
    }
}
