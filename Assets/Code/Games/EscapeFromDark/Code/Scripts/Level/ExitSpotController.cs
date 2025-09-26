using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level
{
    internal class ExitSpotController : MonoBehaviour
    {
        [Header("Light Settings")]
        [SerializeField] private Light2D light2D;
        [SerializeField] private float activationDistance = 3f;
        [SerializeField] private float maxIntensity = 12f;
        [SerializeField] private float updateInterval = 0.1f; // Интервал обновления для оптимизации
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        private Transform _playerTransform;
        private float _lastUpdateTime;
        private bool _isActive;

        internal struct Ctx
        {
            public Transform playerTransform;
        }

        public void SetCtx(Ctx ctx)
        {
            _playerTransform = ctx.playerTransform;
            
            if (light2D == null)
            {
                light2D = GetComponent<Light2D>();
            }
            
            if (light2D == null)
            {
                Debug.LogError("ExitSpotController: Light2D component not found!");
                return;
            }
            
            // Изначально свет полностью выключен
            light2D.intensity = 0f;
            light2D.enabled = false; // Полностью выключаем компонент света
            _isActive = false;
            
            Debug.Log("ExitSpotController: Initialized with light disabled");
        }

        void Start()
        {
            // Гарантируем, что свет изначально выключен
            if (light2D == null)
            {
                light2D = GetComponent<Light2D>();
            }
            
            if (light2D != null)
            {
                light2D.intensity = 0f;
                light2D.enabled = false;
                _isActive = false;
                Debug.Log("ExitSpotController: Light disabled in Start()");
            }
        }

        void Update()
        {
            // Оптимизация - обновляем не каждый кадр
            if (Time.time - _lastUpdateTime < updateInterval)
                return;
                
            _lastUpdateTime = Time.time;
            
            UpdateLightIntensity();
        }

        private void UpdateLightIntensity()
        {
            if (_playerTransform == null || light2D == null)
                return;

            // Вычисляем расстояние до игрока
            float distance = Vector3.Distance(transform.position, _playerTransform.position);
            
            if (distance <= activationDistance)
            {
                if (!_isActive)
                {
                    _isActive = true;
                    light2D.enabled = true; // Включаем свет при активации
                    if (showDebugInfo)
                    {
                        Debug.Log($"ExitSpotController: Activated at distance {distance:F2}");
                    }
                }
                
                // Вычисляем интенсивность света (обратно пропорционально расстоянию)
                float normalizedDistance = distance / activationDistance; // 0 (близко) до 1 (далеко)
                float intensity = Mathf.Lerp(maxIntensity, 0f, normalizedDistance);
                
                light2D.intensity = intensity;
                
                if (showDebugInfo)
                {
                    Debug.Log($"ExitSpotController: Distance {distance:F2}, Intensity {intensity:F2}");
                }
            }
            else
            {
                if (_isActive)
                {
                    _isActive = false;
                    light2D.intensity = 0f;
                    light2D.enabled = false; // Полностью выключаем свет при деактивации
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"ExitSpotController: Deactivated at distance {distance:F2}");
                    }
                }
            }
        }

        // Метод для установки игрока извне (если нужно)
        public void SetPlayer(Transform playerTransform)
        {
            _playerTransform = playerTransform;
            Debug.Log("ExitSpotController: Player reference updated");
        }

        // Методы для настройки параметров во время игры
        public void SetActivationDistance(float newDistance)
        {
            activationDistance = newDistance;
            Debug.Log($"ExitSpotController: Activation distance set to {newDistance}");
        }

        public void SetMaxIntensity(float newIntensity)
        {
            maxIntensity = newIntensity;
            Debug.Log($"ExitSpotController: Max intensity set to {newIntensity}");
        }

        // Принудительное обновление интенсивности
        [ContextMenu("Force Update Light")]
        public void ForceUpdateLight()
        {
            UpdateLightIntensity();
        }

        // Принудительное выключение света
        public void DisableLight()
        {
            if (light2D != null)
            {
                light2D.intensity = 0f;
                light2D.enabled = false;
                _isActive = false;
                Debug.Log("ExitSpotController: Light forcibly disabled");
            }
        }

        // Метод для отладки - показать текущие параметры
        [ContextMenu("Debug Info")]
        private void DebugInfo()
        {
            if (_playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, _playerTransform.position);
                Debug.Log($"ExitSpotController Debug:");
                Debug.Log($"- Distance to player: {distance:F2}");
                Debug.Log($"- Activation distance: {activationDistance}");
                Debug.Log($"- Current intensity: {(light2D != null ? light2D.intensity : 0):F2}");
                Debug.Log($"- Max intensity: {maxIntensity}");
                Debug.Log($"- Is active: {_isActive}");
            }
            else
            {
                Debug.Log("ExitSpotController: No player reference");
            }
        }

        private void OnValidate()
        {
            // Ограничиваем значения в разумных пределах
            activationDistance = Mathf.Max(0.1f, activationDistance);
            maxIntensity = Mathf.Max(0f, maxIntensity);
            updateInterval = Mathf.Max(0.01f, updateInterval);
        }
    }
}
