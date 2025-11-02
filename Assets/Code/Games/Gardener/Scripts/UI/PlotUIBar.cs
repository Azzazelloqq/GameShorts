using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.Gardener.UI
{
    /// <summary>
    /// UI бар, отображающий прогресс роста и уровень воды над грядкой
    /// Позиционируется в screen-space через WorldToScreenPoint
    /// </summary>
    public class PlotUIBar : BaseMonoBehaviour
    {
        [SerializeField] private Slider _growthProgressBar;
        [SerializeField] private Slider _waterLevelBar;
        [SerializeField] private Vector2 _screenOffset = new Vector2(0, 50);
        [SerializeField] private CanvasGroup _canvasGroup;
        
        private RectTransform _rectTransform;
        private Camera _worldCamera;
        private Transform _targetPlot;
        private bool _isInitialized;
        private Canvas _parentCanvas;

        protected override void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
        }
        
        /// <summary>
        /// Инициализирует бар для конкретной грядки
        /// </summary>
        public void Initialize(Camera camera, Transform plotTransform)
        {
            _worldCamera = camera;
            _targetPlot = plotTransform;
            _isInitialized = true;
            
            // Получаем родительский Canvas
            _parentCanvas = GetComponentInParent<Canvas>();
            
            UpdatePosition();
        }
        
        /// <summary>
        /// Обновляет позицию бара относительно грядки
        /// </summary>
        public void UpdatePosition()
        {
            if (!_isInitialized || _targetPlot == null || _worldCamera == null || _rectTransform == null)
                return;
            
            Vector3 worldPosition = _targetPlot.position;
            
            // Преобразуем мировую позицию в экранную
            Vector3 screenPosition = _worldCamera.WorldToScreenPoint(worldPosition);
            
            // Если объект за камерой, скрываем UI
            if (screenPosition.z < 0)
            {
                SetVisible(false);
                return;
            }
            
            SetVisible(true);
            
            // Для Screen Space - Overlay Canvas используем прямую экранную позицию
            if (_parentCanvas != null && _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                screenPosition.x += _screenOffset.x;
                screenPosition.y += _screenOffset.y;
                screenPosition.z = 0;
                
                _rectTransform.position = screenPosition;
            }
            // Для Screen Space - Camera или World Space
            else if (_parentCanvas != null)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _parentCanvas.transform as RectTransform,
                    screenPosition,
                    _parentCanvas.worldCamera ?? _worldCamera,
                    out localPoint
                );
                
                localPoint.x += _screenOffset.x;
                localPoint.y += _screenOffset.y;
                
                _rectTransform.anchoredPosition = localPoint;
            }
        }
        
        /// <summary>
        /// Устанавливает прогресс роста (0-1)
        /// </summary>
        public void SetGrowthProgress(float progress)
        {
            if (_growthProgressBar != null)
            {
                _growthProgressBar.value = Mathf.Clamp01(progress);
            }
        }
        
        /// <summary>
        /// Устанавливает уровень воды (0-1)
        /// </summary>
        public void SetWaterLevel(float level)
        {
            if (_waterLevelBar != null)
            {
                _waterLevelBar.value = Mathf.Clamp01(level);
            }
        }
        
        /// <summary>
        /// Показывает или скрывает бар
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = visible ? 1 : 0;
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }
    }
}

