using UnityEngine;

namespace GameShorts.FlyHumans.Gameplay
{
    /// <summary>
    /// View компонент персонажа - только визуализация и данные
    /// </summary>
    public class CharacterView : MonoBehaviour
    {
        [SerializeField] private Transform _characterTransform;
        [SerializeField] private RagdollRoot _ragdollRoot;
        [SerializeField] private Rigidbody[] _rigidbodies;
        
        [Header("Components")]
        [SerializeField] private Animator _animator;
        [SerializeField] private ParticleSystem _flyParticles;
        
        [Header("Movement Settings")]
        [SerializeField] private float _jumpForce = 5f;
        [SerializeField] private float _gravity = 9.8f;
        [SerializeField] private float _maxHeight = 10f;
        
        private float _verticalVelocity;
        private bool _wasFlying;
        private bool _isActive;
        private float _currentGravity;
        private Vector3 _movementDirection;
        private Quaternion _initialRotation;
        private Vector3 _initialPosition;

        // Properties
        public RagdollRoot RagdollRoot => _ragdollRoot;
        public Animator Animator => _animator;
        public float JumpForce => _jumpForce;
        public float Gravity => _gravity;
        
        public bool IsActive
        {
            get => _isActive;
            set => _isActive = value;
        }
        
        public float VerticalVelocity
        {
            get => _verticalVelocity;
            set => _verticalVelocity = value;
        }
        
        public float CurrentGravity
        {
            get => _currentGravity;
            set => _currentGravity = value;
        }

        public void Initialize()
        {
            // Сбрасываем состояние персонажа
            ResetState();
            
            // Запоминаем начальное направление движения (фиксируем его)
            _movementDirection = transform.forward;
            
            // Запоминаем начальный поворот персонажа
            _initialRotation = transform.rotation;
            
            // Запоминаем начальную позицию персонажа
            _initialPosition = _characterTransform.position;
        }
        
        public void ResetState()
        {
            _currentGravity = 0;
            _verticalVelocity = 0f;
            _isActive = false;
            _wasFlying = false;
            
            // Включаем animator если был выключен
            if (_animator != null)
            {
                _animator.enabled = true;
            }
            
            // Останавливаем партиклы при сбросе
            if (_flyParticles != null)
            {
                _flyParticles.Stop();
            }
        }
        
        public void ResetToInitialPosition()
        {
            Debug.Log($"Resetting character to initial position: {_initialPosition}");
            
            // Возвращаем персонажа в начальную позицию
            _characterTransform.position = _initialPosition;
            _characterTransform.rotation = _initialRotation;
            
            // Отключаем физику ragdoll
            foreach (var rigidbody in _rigidbodies)
            {
                rigidbody.useGravity = false;
                rigidbody.linearVelocity = Vector3.zero;
                rigidbody.angularVelocity = Vector3.zero;
            }
            
            // Сбрасываем состояние
            ResetState();
            
            // Включаем анимацию idle
            if (_animator != null)
            {
                _animator.Play("Idle", 0, 0f);
            }
            
            Debug.Log($"Character reset complete. Current position: {_characterTransform.position}");
        }
        
        public void UpdatePosition(float deltaTime)
        {
            Vector3 movement = Vector3.zero;
            
            // Персонаж теперь НЕ двигается вперед - движется мир
            // Движение только вверх/вниз
            movement += Vector3.up * _verticalVelocity * deltaTime;

            var newPos = _characterTransform.position;
            // Обновляем позицию персонажа
            newPos += movement;
            newPos.y = Mathf.Min(_maxHeight,  newPos.y);
            _characterTransform.position = newPos;
            _characterTransform.rotation = _initialRotation;
        }
        
        public void UpdateAnimation()
        {
            bool isFlying = _verticalVelocity > 0f;
            
            // Обновляем параметр Flying только когда состояние изменилось
            if (isFlying != _wasFlying)
            {
                _animator.SetBool("Flying", isFlying);
                _wasFlying = isFlying;
                
                // Включаем/выключаем партиклы при изменении состояния полета
                if (_flyParticles != null)
                {
                    if (isFlying)
                    {
                        // Останавливаем и запускаем заново для незацикленных партиклов
                        _flyParticles.Stop();
                        _flyParticles.Play();
                    }
                    else
                    {
                        _flyParticles.Stop();
                    }
                }
            }
        }
        
        public void StartJumpAnimation()
        {
            _animator.SetTrigger("Start");
        }
        
        public void StopCharacter()
        {
            _isActive = false;
            _animator.enabled = false;
            
            // Останавливаем партиклы при остановке персонажа
            if (_flyParticles != null)
            {
                _flyParticles.Stop();
            }

            foreach (var rigidbody in _rigidbodies)
            {
                rigidbody.useGravity = true;
            }
        }
    }
}

