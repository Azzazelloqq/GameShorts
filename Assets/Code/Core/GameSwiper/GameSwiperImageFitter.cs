using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.GameSwiper
{
    /// <summary>
    /// Component to handle fullscreen display of game render textures in the swiper
    /// Ensures that the game content fills the entire screen on any device
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class GameSwiperImageFitter : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField]
        private ScaleMode _scaleMode = ScaleMode.StretchToFill; // Default to full screen
        
        private RawImage _rawImage;
        private RectTransform _rectTransform;
        private float _lastScreenWidth;
        private float _lastScreenHeight;
        
        public enum ScaleMode
        {
            ScaleToFit,     // Shows full content, may have letterboxing
            ScaleAndCrop,   // Fills the screen, may crop content  
            StretchToFill   // Fills entire screen (default for mobile)
        }
        
        private void Awake()
        {
            _rawImage = GetComponent<RawImage>();
            _rectTransform = GetComponent<RectTransform>();
        }
        
        private void Start()
        {
            UpdateAspectRatio();
        }
        
        private void Update()
        {
            // Check if screen dimensions changed
            if (Mathf.Abs(_lastScreenWidth - Screen.width) > 0.1f || 
                Mathf.Abs(_lastScreenHeight - Screen.height) > 0.1f)
            {
                UpdateAspectRatio();
            }
        }
        
        /// <summary>
        /// Updates the display mode of the RawImage
        /// </summary>
        public void UpdateAspectRatio()
        {
            if (_rawImage == null)
            {
                return;
            }
            
            _lastScreenWidth = Screen.width;
            _lastScreenHeight = Screen.height;
            
            // Since RenderTexture matches screen size, we typically want to fill the screen
            switch (_scaleMode)
            {
                case ScaleMode.ScaleToFit:
                    if (_rawImage.texture != null)
                    {
                        float screenAspect = _lastScreenWidth / _lastScreenHeight;
                        float textureAspect = (float)_rawImage.texture.width / _rawImage.texture.height;
                        ApplyScaleToFit(screenAspect, textureAspect);
                    }
                    break;
                    
                case ScaleMode.ScaleAndCrop:
                    if (_rawImage.texture != null)
                    {
                        float screenAspect = _lastScreenWidth / _lastScreenHeight;
                        float textureAspect = (float)_rawImage.texture.width / _rawImage.texture.height;
                        ApplyScaleAndCrop(screenAspect, textureAspect);
                    }
                    break;
                    
                case ScaleMode.StretchToFill:
                default:
                    ApplyStretchToFill();
                    break;
            }
        }
        
        private void ApplyScaleToFit(float screenAspect, float textureAspect)
        {
            // Reset UV rect to show full texture
            _rawImage.uvRect = new Rect(0, 0, 1, 1);
            
            // Adjust the size of the RectTransform to maintain aspect ratio
            if (screenAspect > textureAspect)
            {
                // Screen is wider than texture - fit by height
                float width = _rectTransform.rect.height * textureAspect;
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            }
            else
            {
                // Screen is taller than texture - fit by width
                float height = _rectTransform.rect.width / textureAspect;
                _rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            }
        }
        
        private void ApplyScaleAndCrop(float screenAspect, float textureAspect)
        {
            // Calculate UV rect to crop the texture
            float scaleX = 1f;
            float scaleY = 1f;
            float offsetX = 0f;
            float offsetY = 0f;
            
            if (screenAspect > textureAspect)
            {
                // Screen is wider - crop top and bottom
                scaleY = textureAspect / screenAspect;
                offsetY = (1f - scaleY) * 0.5f;
            }
            else
            {
                // Screen is taller - crop left and right
                scaleX = screenAspect / textureAspect;
                offsetX = (1f - scaleX) * 0.5f;
            }
            
            _rawImage.uvRect = new Rect(offsetX, offsetY, scaleX, scaleY);
        }
        
        private void ApplyStretchToFill()
        {
            // Reset UV rect to show full texture
            _rawImage.uvRect = new Rect(0, 0, 1, 1);
            
            // Don't modify anchors or position - let GameSwiper control positioning
            // The RawImage size and position should be controlled by its parent/GameSwiper
        }
        
        /// <summary>
        /// Called when a new texture is assigned
        /// </summary>
        public void OnTextureChanged()
        {
            UpdateAspectRatio();
        }
        
        /// <summary>
        /// Sets the scale mode
        /// </summary>
        public void SetScaleMode(ScaleMode mode)
        {
            _scaleMode = mode;
            UpdateAspectRatio();
        }
    }
}
