using TMPro;
using UnityEngine;

namespace Code.Utils
{
    internal class FpsCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text fpsText;
        
        [Header("FPS Settings")]
        [SerializeField] private float updateInterval = 0.5f;
        
        [Header("Color Thresholds")]
        [SerializeField] private float goodFpsThreshold = 60f;
        [SerializeField] private float mediumFpsThreshold = 30f;
        
        [Header("Colors")]
        [SerializeField] private Color goodColor = Color.green;
        [SerializeField] private Color mediumColor = Color.yellow;
        [SerializeField] private Color badColor = Color.red;
        
        private float _deltaTime;
        private float _timeSinceLastUpdate;
        private int _frameCount;
        
        private void Update()
        {
            if (fpsText == null)
                return;
            
            _deltaTime += Time.unscaledDeltaTime;
            _frameCount++;
            _timeSinceLastUpdate += Time.unscaledDeltaTime;
            
            if (_timeSinceLastUpdate >= updateInterval)
            {
                float fps = _frameCount / _deltaTime;
                UpdateFpsDisplay(fps);
                
                _deltaTime = 0f;
                _frameCount = 0;
                _timeSinceLastUpdate = 0f;
            }
        }
        
        private void UpdateFpsDisplay(float fps)
        {
            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
            fpsText.color = GetFpsColor(fps);
        }
        
        private Color GetFpsColor(float fps)
        {
            if (fps >= goodFpsThreshold)
            {
                return goodColor;
            }
            else if (fps >= mediumFpsThreshold)
            {
                // Плавный переход от красного к желтому и от желтого к зеленому
                float t = (fps - mediumFpsThreshold) / (goodFpsThreshold - mediumFpsThreshold);
                return Color.Lerp(mediumColor, goodColor, t);
            }
            else
            {
                // Плавный переход от красного к желтому
                float t = fps / mediumFpsThreshold;
                return Color.Lerp(badColor, mediumColor, t);
            }
        }
    }
}

