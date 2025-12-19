using System.Threading;
using Disposable;
using Code.Core.InputManager;
using LightDI.Runtime;
using R3;
using TickHandler;
using UnityEngine;

namespace Lightseeker
{
    internal class LightseekerPlayerPm : DisposableBase
    {
        internal struct Ctx
        {
            public LightseekerSceneContextView sceneContextView;
            public CancellationToken cancellationToken;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private readonly IInputManager _inputManager;
        private readonly ITickHandler _tickHandler;
        
        private LightseekerPlayerView _playerView;
        private Vector3 _velocity;
        private static Vector3 _savedInitialPosition;
        private static Quaternion _savedInitialRotation;
        private static bool _hasInitialPositionBeenSaved = false;
        private bool _isInputEnabled = true;
        private Vector2 _inputDirection;
        private bool _isMoving;

        private const float MoveSpeed = 7f;
        private const float RotationSpeed = 70f;
        private const float Gravity = -9.81f;
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

        /// <summary>
        /// Сбрасывает статическое состояние для полного перезапуска игры
        /// </summary>
        public static void ResetStaticState()
        {
            _hasInitialPositionBeenSaved = false;
            _savedInitialPosition = Vector3.zero;
            _savedInitialRotation = Quaternion.identity;
            Debug.Log("LightseekerPlayerPm: Static state reset");
        }

        public LightseekerPlayerPm(Ctx ctx, 
            [Inject] IInputManager inputManager, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            _tickHandler = tickHandler;
            _playerView = _ctx.sceneContextView.PlayerPrefab;
            
            SubscribeToPause();
            StartMovement();
            
            Debug.Log($"LightseekerPlayerPm: InputManager IsJoystickActive: {_inputManager.IsJoystickActive}");
        }

        private void SubscribeToPause()
        {
            AddDisposable(_ctx.isPaused.Subscribe(isPaused =>
            {
                _isInputEnabled = !isPaused;
            }));
        }
        
        private void HandleMovement(float deltaTime)
        {
            // Получаем input от джойстика
            _inputDirection = _inputManager.GetJoystickInput();

            _isMoving = _inputDirection.magnitude > 0.1f;
            
            if (_inputDirection.magnitude > 0.1f)
            {
                Debug.Log($"LightseekerPlayerPm: Input detected: {_inputDirection}, IsInputEnabled: {_isInputEnabled}");
            }
        }
        private void StartMovement()
        {
            _tickHandler.PhysicUpdate += HandleMovement;
            _tickHandler.FrameUpdate += UpdateMovement;
        }

        private void UpdateMovement(float deltaTime)
        {
            if (!_isInputEnabled)
            {
                Debug.Log("LightseekerPlayerPm: Input disabled");
                return;
            }
            
            if (_playerView == null)
            {
                Debug.LogError("LightseekerPlayerPm: PlayerView is NULL!");
                return;
            }
            
            if (_playerView.CharacterController == null)
            {
                Debug.LogError("LightseekerPlayerPm: CharacterController is NULL!");
                return;
            }

            bool isMoving = _inputDirection.magnitude > 0.1f;

            if (isMoving)
            {
                // Поворот по оси X джойстика
                float rotationInput = _inputDirection.x;
                if (Mathf.Abs(rotationInput) > 0.1f)
                {
                    float rotation = rotationInput * RotationSpeed * deltaTime;
                    _playerView.transform.Rotate(Vector3.up, rotation);
                }
                
                // Движение вперед/назад по оси Y джойстика
                float moveInput = _inputDirection.y;
                if (Mathf.Abs(moveInput) > 0.1f)
                {
                    Vector3 moveDirection = _playerView.transform.forward * moveInput;
                    Vector3 move = moveDirection * (MoveSpeed * deltaTime);
                    _playerView.CharacterController.Move(move);
                }
            }

            UpdateAnimation();
            // Применяем гравитацию
            ApplyGravity(deltaTime);
        }

        private void UpdateAnimation()
        {
            _playerView.Animator.SetBool(IsMovingHash, _isMoving);
        }

        private void ApplyGravity(float deltaTime)
        {
            if (_playerView.CharacterController.isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }

            _velocity.y += Gravity * deltaTime;
            _playerView.CharacterController.Move(_velocity * deltaTime);
        }

        protected override void OnDispose()
        {
            _tickHandler.PhysicUpdate -= HandleMovement;
            _tickHandler.FrameUpdate -= UpdateMovement;
            
            base.OnDispose();
        }
    }
}

