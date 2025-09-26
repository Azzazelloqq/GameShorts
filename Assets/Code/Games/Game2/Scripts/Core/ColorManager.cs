using UnityEngine;

namespace Code.Core.ShortGamesCore.Game2
{
    public class ColorManager : MonoBehaviour
    {
        [Header("Block Colors")]
        [SerializeField] private Color[] blockColors = new Color[]
        {
            Color.red,
            Color.blue,
            Color.green,
            Color.yellow,
            Color.magenta,
            Color.cyan,
            new Color(1f, 0.5f, 0f), // Orange
            new Color(0.5f, 0f, 1f), // Purple
            new Color(1f, 0.75f, 0.8f), // Pink
            new Color(0.5f, 1f, 0.5f)  // Light Green
        };

        [Header("Background Colors")]
        [SerializeField] private Color[] backgroundColors = new Color[]
        {
            new Color(0.2f, 0.3f, 0.8f), // Blue
            new Color(0.8f, 0.2f, 0.3f), // Red
            new Color(0.3f, 0.8f, 0.2f), // Green
            new Color(0.8f, 0.5f, 0.2f), // Orange
            new Color(0.6f, 0.2f, 0.8f), // Purple
            new Color(0.2f, 0.8f, 0.8f), // Cyan
            new Color(0.8f, 0.8f, 0.2f), // Yellow
            new Color(0.8f, 0.4f, 0.6f)  // Pink
        };

        [Header("Settings")]
        [SerializeField] private int blocksPerBackgroundChange = 10;

        private Camera mainCamera;

        private void Awake()
        {
            mainCamera = Camera.main;
            if (mainCamera == null)
                mainCamera = FindObjectOfType<Camera>();
        }

        public Color GetBlockColor(int blockIndex)
        {
            return blockColors[blockIndex % blockColors.Length];
        }

        public void UpdateBackgroundColor(int totalBlocks)
        {
            if (mainCamera == null) return;

            int currentGroup = totalBlocks / blocksPerBackgroundChange;
            int nextGroup = currentGroup + 1;
            
            Color currentColor = backgroundColors[currentGroup % backgroundColors.Length];
            Color nextColor = backgroundColors[nextGroup % backgroundColors.Length];
            
            // Calculate progress within current group (0 to 1)
            float progress = (float)(totalBlocks % blocksPerBackgroundChange) / blocksPerBackgroundChange;
            
            // Interpolate between current and next color
            Color targetColor = Color.Lerp(currentColor, nextColor, progress);
            
            mainCamera.backgroundColor = targetColor;
        }

        public void ResetBackgroundColor()
        {
            if (mainCamera != null && backgroundColors.Length > 0)
            {
                mainCamera.backgroundColor = backgroundColors[0];
            }
        }
    }
}
