using System;
using UnityEngine;

namespace SurvivalDuck
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private Transform _viewPlayer;
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float gravity = -9.81f;
        
        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private FloatingJoystick joystick;
        
        private CharacterController _characterController;
        private Vector3 _velocity;
        private bool _isMoving;
        private Vector2 _inputDirection;

        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();
            
            // Если аниматор не назначен, попробуем найти в детях
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }
        }

        private void Update()
        {
            UpdateAnimation();
            Move();
        }

        private void FixedUpdate()
        {  
            HandleMovement();
        }

        private void HandleMovement()
        {
            if (joystick == null) return;

            // Получаем input от джойстика
            _inputDirection = new Vector2(joystick.Horizontal, joystick.Vertical);

            _isMoving = _inputDirection.magnitude > 0.1f;
        }

        private void Move()
        {
            if (_isMoving)
            {
                // Поворот по оси X джойстика
                float rotationInput = _inputDirection.x;
                if (Mathf.Abs(rotationInput) > 0.1f)
                {
                    float rotation = rotationInput * rotationSpeed * Time.deltaTime;
                    _viewPlayer.Rotate(Vector3.up, rotation);
                }
                
                // Движение вперед/назад по оси Y джойстика
                float moveInput = _inputDirection.y;
                if (Mathf.Abs(moveInput) > 0.1f)
                {
                    // Движение относительно текущего направления персонажа
                    Vector3 moveDirection = _viewPlayer.forward * moveInput;
                    Vector3 move = moveDirection * (moveSpeed * Time.fixedDeltaTime);
                    _characterController.Move(move);
                }
            }

            // Применяем гравитацию
            ApplyGravity();
        }

        private void ApplyGravity()
        {
            if (_characterController.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Небольшое значение для стабильности на земле
            }

            _velocity.y += gravity * Time.fixedDeltaTime;
            _characterController.Move(_velocity * Time.fixedDeltaTime);
        }

        private void UpdateAnimation()
        {
            if (animator != null)
            {
                animator.SetBool(IsMovingHash, _isMoving);
            }
        }

        // Публичные методы для внешнего управления
        public void SetMoveSpeed(float speed)
        {
            moveSpeed = Mathf.Max(0, speed);
        }

        public void SetRotationSpeed(float speed)
        {
            rotationSpeed = Mathf.Max(0, speed);
        }

        public bool IsMoving()
        {
            return _isMoving;
        }

        public Vector3 GetVelocity()
        {
            return _characterController.velocity;
        }

        private void OnValidate()
        {
            // Валидация значений в редакторе
            moveSpeed = Mathf.Max(0, moveSpeed);
            rotationSpeed = Mathf.Max(0, rotationSpeed);
        }
    }
}

