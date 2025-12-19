using Disposable;
using UnityEngine;
using TMPro;
using System;
using System.Collections;

namespace Code.Games
{
    internal class Game2048CubeView : MonoBehaviourDisposable
    {
        internal struct Ctx
        {
            public Guid cubeId;
            public int number;
            public Action<Guid> onCollisionWithCube; // (myId, otherId)
        }

        [SerializeField] 
        private Rigidbody _rigidbody;
        
        [SerializeField] 
        private Transform _transform;
        [SerializeField] 
        private LineRenderer _line;
        
        [Header("Cube Appearance")]
        [SerializeField] 
        private Renderer _cubeRenderer;
        
        [SerializeField] 
        private TextMeshPro[] _textMeshComponents;
        
        [Header("Cube Settings")]
        private int _number = 2;
        
        [SerializeField] private CubeColorManager  _cubeColorManager;
        
        [Header("Launch Settings")]
        [SerializeField] 
        [Tooltip("Кривая ускорения (X - время 0-1, Y - множитель силы)")]
        private AnimationCurve _accelerationCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        
        [SerializeField]
        [Tooltip("Длительность применения силы (в секундах)")]
        private float _launchDuration = 0.5f;
        
        private Color _cubeColor = new Color(1f, 0.6f, 0f, 1f);
        
        private Color _textColor = Color.white;
        
        private Color _outlineColor = Color.black;
        
        [SerializeField] 
        private float _outlineWidth = 0.2f;

        private Ctx _ctx;
        private bool _isControlled;
        private Material _cubeMaterial;
        private float _createdPositionX;
        private float _targetX;
        private const float _lerpSpeed = 15f;
        private Coroutine _launchCoroutine;

        public LineRenderer Line => _line;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            
            // Инициализируем компоненты если нужно
            InitializeComponents();
            
            // Устанавливаем число из контекста
            _number = ctx.number;
            
            // Обновляем цвета куба при установке контекста
            UpdateColorsFromManager();
        }
        
        /// <summary>
        /// Инициализирует компоненты куба
        /// </summary>
        private void InitializeComponents()
        {
            if (_transform == null)
                _transform = transform;
            
            if (_cubeRenderer == null)
                _cubeRenderer = GetComponent<Renderer>();
                
            if (_cubeRenderer != null && _cubeMaterial == null)
            {
                // Создаём копию материала для этого кубика
                _cubeMaterial = new Material(_cubeRenderer.material);
                _cubeRenderer.material = _cubeMaterial;
            }
            
            // Если TextMeshPro компоненты не назначены, попробуем найти их
            if (_textMeshComponents == null || _textMeshComponents.Length == 0)
            {
                _textMeshComponents = GetComponentsInChildren<TextMeshPro>();
            }
            
            _createdPositionX = transform.position.x;
        }

        public void StartControl()
        {
            _isControlled = true;
            _rigidbody.isKinematic = true;
            _targetX = _transform.position.x;
        }

        /// <summary>
        /// Устанавливает целевую позицию X для плавного движения
        /// </summary>
        public void SetTargetX(float targetX)
        {
            _targetX = targetX;
            Debug.Log($"Set TargetX({targetX})");
        }
        
        /// <summary>
        /// Получает текущую позицию X куба
        /// </summary>
        public float GetPositionX()
        {
            return _transform.position.x;
        }
        
        public void UpdateController(float deltaTime)
        {
            if (_isControlled)
            {
                // Плавно перемещаем куб к целевой позиции
                Vector3 currentPosition = _transform.position;
                currentPosition.x = Mathf.Lerp(currentPosition.x, _targetX, _lerpSpeed * deltaTime);
                _transform.position = currentPosition;
            }
        }

        public void LaunchForward(float force)
        {
            _isControlled = false;
            _line.gameObject.SetActive(false);
            _rigidbody.isKinematic = false;
            
            // Запускаем корутину для применения силы по кривой
            _launchCoroutine = StartCoroutine(LaunchCoroutine(force));
        }
        
        private IEnumerator LaunchCoroutine(float baseForce)
        {
            float elapsedTime = 0f;
            
            while (elapsedTime < _launchDuration)
            {
                float progress = Mathf.Clamp01(elapsedTime / _launchDuration);
                
                // Получаем множитель силы из кривой
                float curveMultiplier = _accelerationCurve.Evaluate(progress);
                
                // Применяем силу вперед с учетом кривой
                // Делим на _launchDuration чтобы распределить импульс по времени
                float currentForce = (baseForce / _launchDuration) * curveMultiplier;
                _rigidbody.AddForce(Vector3.forward * currentForce, ForceMode.Force);
                
                elapsedTime += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }
        }

        /// <summary>
        /// Сбрасывает скорость куба до нуля
        /// </summary>
        public void ResetVelocity()
        {
            StopLaunchAcceleration();
            
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = false;
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
            }
            transform.rotation = Quaternion.identity;
            _isControlled = false;
        }
        
        /// <summary>
        /// Подготавливает куб к контролю игроком (останавливает физику)
        /// </summary>
        public void PrepareForControl()
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.angularVelocity = Vector3.zero;
                _rigidbody.isKinematic = true;
            }
        }
        
        
        /// <summary>
        /// Обновляет материал кубика
        /// </summary>
        private void UpdateCubeMaterial()
        {
            if (_cubeMaterial != null)
            {
                // Устанавливаем базовый цвет для URP шейдера
                _cubeMaterial.SetColor("_BaseColor", _cubeColor);
            }
        }
        
        /// <summary>
        /// Обновляет внешний вид текста
        /// </summary>
        private void UpdateTextAppearance()
        {
            if (_textMeshComponents == null) return;
            
            foreach (var textMesh in _textMeshComponents)
            {
                if (textMesh != null)
                {
                    textMesh.text = _number.ToString();
                    // Устанавливаем цвет текста
                    textMesh.color = _textColor;
                    
                    // Настраиваем обводку
                    textMesh.outlineColor = _outlineColor;
                    textMesh.outlineWidth = _outlineWidth;
                }
            }
        }
        
        /// <summary>
        /// Получает текущее число на кубике
        /// </summary>
        public int GetNumber() => _number;
        
        /// <summary>
        /// Получает Guid куба
        /// </summary>
        public Guid GetCubeId() => _ctx.cubeId;
        
        /// <summary>
        /// Обновляет цвета куба из CubeColorManager
        /// </summary>
        private void UpdateColorsFromManager()
        {
            if (_cubeColorManager == null) return;
            
            var colorScheme = _cubeColorManager.GetColorScheme(_number);
            
            // Обновляем цвета
            _cubeColor = colorScheme.baseColor;
            _textColor = colorScheme.digitColor;
            _outlineColor = colorScheme.outlineColor;
            
            // Применяем изменения
            UpdateCubeMaterial();
            UpdateTextAppearance();
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Player"))
                return;
            
            // Проверяем, что столкнулись с другим кубом
            var otherCube = collision.gameObject.GetComponent<Game2048CubeView>();
            if (otherCube == null)
                return;
            
            // Останавливаем ускорение при столкновении с любым кубом
            StopLaunchAcceleration();

            // Проверяем, что числа одинаковые
            if (GetNumber() != otherCube.GetNumber())
                return;

            // Избегаем дублирования обработки коллизии
            if (GetInstanceID() < otherCube.GetInstanceID())
                return;

            // Уведомляем о коллизии через Action в контексте
            _ctx.onCollisionWithCube?.Invoke(otherCube.GetCubeId());
        }
        
        /// <summary>
        /// Останавливает корутину ускорения при запуске
        /// </summary>
        private void StopLaunchAcceleration()
        {
            if (_launchCoroutine != null)
            {
                StopCoroutine(_launchCoroutine);
                _launchCoroutine = null;
            }
        }
        
        private void OnDestroy()
        {
            // Останавливаем корутину ускорения
            StopLaunchAcceleration();
            
            // Освобождаем созданный материал
            if (_cubeMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(_cubeMaterial);
                else
                    DestroyImmediate(_cubeMaterial);
                    
                _cubeMaterial = null;
            }
        }
    }
}
