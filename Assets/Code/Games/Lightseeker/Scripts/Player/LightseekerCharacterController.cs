using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using LightDI.Runtime;
using R3;
using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerCharacterController : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public LightseekerSceneContextView sceneContextView;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        
        private float _moveSpeed = 5f;
        private float _rotationSpeed = 10f;
        private float _gravity = -9.81f;
        
        private Transform _playerTransform;
        private CharacterController _characterController;
        private FloatingJoystick _joystick;
        
        private Vector3 _velocity;
        private bool _isMoving;
        private Vector2 _inputDirection;
        private bool _isInputEnabled = true;
        private readonly IInputManager _inputManager;

        public LightseekerCharacterController(Ctx ctx, [Inject] IInputManager inputManager)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            SubscribeToPause();
        }

        private void SubscribeToPause()
        {
            AddDispose(_ctx.isPaused.Subscribe(isPaused =>
            {
                _isInputEnabled = !isPaused;
            }));
        }

        // Метод для установки ссылки на transform игрока (будет вызван извне)
        public void SetPlayerTransform(Transform playerTransform, CharacterController characterController)
        {
            _playerTransform = playerTransform;
            _characterController = characterController;
        }

        // Этот метод нужно вызывать каждый кадр (из MonoBehaviour.Update)
        public void UpdateMovement()
        {
            if (!_isInputEnabled || _playerTransform == null || _characterController == null)
                return;

            HandleInput();
            Move();
        }

        // Этот метод нужно вызывать в FixedUpdate
        public void FixedUpdateMovement()
        {
            if (!_isInputEnabled || _playerTransform == null || _characterController == null)
                return;

            ApplyGravity();
        }

        private void HandleInput()
        {
            _inputDirection = _inputManager.GetJoystickInput();
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
                    float rotation = rotationInput * _rotationSpeed * Time.deltaTime;
                    _playerTransform.Rotate(Vector3.up, rotation);
                }
                
                // Движение вперед/назад по оси Y джойстика
                float moveInput = _inputDirection.y;
                moveInput = moveInput > 0 ? 1 : moveInput;
                if (Mathf.Abs(moveInput) > 0.1f)
                {
                    // Движение относительно текущего направления персонажа
                    Vector3 moveDirection = _playerTransform.forward * moveInput;
                    Vector3 move = moveDirection * (_moveSpeed * Time.fixedDeltaTime);
                    _characterController.Move(move);
                }
            }
        }

        private void ApplyGravity()
        {
            if (_characterController.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Небольшое значение для стабильности на земле
            }

            _velocity.y += _gravity * Time.fixedDeltaTime;
            _characterController.Move(_velocity * Time.fixedDeltaTime);
        }
    }
}

