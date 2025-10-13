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

        private void Start()
        {
            // Ищем FloatingJoystick на сцене, если не назначен
            if (joystick == null)
            {
                joystick = FindObjectOfType<FloatingJoystick>();
                
                if (joystick == null)
                {
                    Debug.LogWarning("PlayerController: FloatingJoystick not found on scene!");
                }
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
                // Конвертируем 2D input в 3D направление относительно камеры
                Vector3 moveDirection = GetCameraRelativeDirection(_inputDirection);
                
                // Перемещение персонажа
                Vector3 move = moveDirection * (moveSpeed * Time.fixedDeltaTime);
                _characterController.Move(move);
                
                // Поворот персонажа в направлении движения
                RotateTowardsDirection(moveDirection);
            }

            // Применяем гравитацию
            ApplyGravity();
        }

        private Vector3 GetCameraRelativeDirection(Vector2 input)
        {
            Camera mainCamera = Camera.main;
            
            // Если камеры нет, используем world space направление
            if (mainCamera == null)
            {
                return new Vector3(input.x, 0f, input.y).normalized;
            }

            // Получаем направления вперед и вправо от камеры
            Vector3 cameraForward = mainCamera.transform.forward;
            Vector3 cameraRight = mainCamera.transform.right;
            
            // Игнорируем вертикальную составляющую камеры
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            // Вычисляем направление движения относительно камеры
            Vector3 direction = cameraForward * input.y + cameraRight * input.x;
            return direction.normalized;
        }

        private void RotateTowardsDirection(Vector3 direction)
        {
            if (direction.magnitude < 0.1f) return;

            // Вычисляем целевой поворот
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            
            // Плавно поворачиваем персонажа
            _viewPlayer.rotation = Quaternion.Slerp(
                _viewPlayer.rotation, 
                targetRotation, 
                rotationSpeed * Time.deltaTime
            );
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

