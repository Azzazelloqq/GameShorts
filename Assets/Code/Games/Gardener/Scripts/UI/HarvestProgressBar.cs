using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.Gardener.UI
{
    /// <summary>
    /// Прогресс-бар, отображающий прогресс сбора урожая при удержании
    /// Появляется под пальцем/курсором во время удержания
    /// Использует шейдер RadialFill с параметром _Arc2
    /// </summary>
    public class HarvestProgressBar : BaseMonoBehaviour
    {
        [SerializeField] private GameObject _rootPanel;
        [SerializeField] private RectTransform _rootTransform;
        [SerializeField] private Image _progressImage;
        
        [Header("Radial Fill Settings")]
        [SerializeField] private bool _reverseDirection = true; // true: 360 -> 0, false: 0 -> 360
        
        private Material _progressMaterial;
        private static readonly int Arc2PropertyId = Shader.PropertyToID("_Arc2");
        
        private const float MaxArc = 360f;
        private const float MinArc = 0f;

        protected override void Awake()
        {
            if (_rootTransform == null)
                _rootTransform = GetComponent<RectTransform>();
            
            // Создаем экземпляр материала для этого прогресс-бара
            if (_progressImage != null && _progressImage.material != null)
            {
                _progressMaterial = new Material(_progressImage.material);
                _progressImage.material = _progressMaterial;
            }
            else
            {
                Debug.LogWarning("HarvestProgressBar: Progress Image or Material is missing!");
            }
                
            Hide();
        }
        
        /// <summary>
        /// Показывает прогресс-бар в указанной позиции экрана
        /// </summary>
        public void Show(Vector2 screenPosition)
        {
            if (_rootPanel != null)
                _rootPanel.SetActive(true);
                
            if (_rootTransform != null)
                _rootTransform.position = screenPosition;
                
            // Устанавливаем начальное значение прогресса
            UpdateProgress(0f);
        }
        
        /// <summary>
        /// Обновляет прогресс (0-1)
        /// </summary>
        public void UpdateProgress(float progress)
        {
            if (_progressMaterial == null)
                return;
            
            progress = Mathf.Clamp01(progress);
            
            // Вычисляем значение Arc2
            float arcValue;
            if (_reverseDirection)
            {
                // От 360 до 0 (уменьшается по мере заполнения)
                arcValue = Mathf.Lerp(MaxArc, MinArc, progress);
            }
            else
            {
                // От 0 до 360 (увеличивается по мере заполнения)
                arcValue = Mathf.Lerp(MinArc, MaxArc, progress);
            }
            
            _progressMaterial.SetFloat(Arc2PropertyId, arcValue);
        }
        
        /// <summary>
        /// Обновляет позицию прогресс-бара
        /// </summary>
        public void UpdatePosition(Vector2 screenPosition)
        {
            if (_rootTransform != null)
                _rootTransform.position = screenPosition;
        }
        
        /// <summary>
        /// Скрывает прогресс-бар
        /// </summary>
        public void Hide()
        {
            if (_rootPanel != null)
                _rootPanel.SetActive(false);
        }
        
        /// <summary>
        /// Проверяет, отображается ли прогресс-бар
        /// </summary>
        public bool IsVisible()
        {
            return _rootPanel != null && _rootPanel.activeSelf;
        }
        
        protected override void OnDestroy()
        {
            // Уничтожаем экземпляр материала при удалении компонента
            if (_progressMaterial != null)
            {
                Destroy(_progressMaterial);
                _progressMaterial = null;
            }
            
            base.OnDestroy();
        }
    }
}

