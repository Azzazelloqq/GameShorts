using System;
using System.Threading;
using Disposable;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level;
using LightDI.Runtime;
using TickHandler;
using R3;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Player
{
    internal class EscapeFromDarkPlayerPm : DisposableBase
    {
        internal struct Ctx
        {
            public EscapeFromDarkSceneContextView sceneContextView;
            public EscapeFromDarkLevelPm levelPm;
            public Action onLevelCompleted;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private readonly IInputManager _inputManager;
        private readonly ITickHandler _tickHandler;
        
        private EscapeFromDarkPlayerView _playerView;
        private EscapeFromDarkPlayerModel _playerModel;
        private Vector3 _spawnPosition;

        public EscapeFromDarkPlayerPm(Ctx ctx, 
            [Inject] IInputManager inputManager,
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            _tickHandler = tickHandler;
            
            InitializePlayerModel();
            SpawnPlayer();
            StartInputHandling();
        }

        private void InitializePlayerModel()
        {
            // Получаем позицию спавна из уровня
            _spawnPosition = _ctx.levelPm.LevelView.GetPlayerSpawnWorldPosition();
            
            var modelCtx = new EscapeFromDarkPlayerModel.Ctx
            {
                moveSpeed = 5f,
                startPosition = _spawnPosition
            };
            
            _playerModel = new EscapeFromDarkPlayerModel(modelCtx);
            AddDisposable(_playerModel);
            
            Debug.Log($"EscapeFromDarkPlayerPm: Player model initialized at {_spawnPosition}");
        }

        private void SpawnPlayer()
        {
            if (_ctx.sceneContextView.PlayerPrefab == null)
            {
                Debug.LogError("EscapeFromDarkPlayerPm: Player prefab is not assigned!");
                return;
            }

            // Создаем игрока
            _playerView = UnityEngine.Object.Instantiate(_ctx.sceneContextView.PlayerPrefab, _spawnPosition, Quaternion.identity);
            
            // Инициализируем View
            var viewCtx = new EscapeFromDarkPlayerView.Ctx();
            _playerView.SetCtx(viewCtx);
            
            Debug.Log($"EscapeFromDarkPlayerPm: Player spawned at {_spawnPosition}");
        }

        private void StartInputHandling()
        {
            // Подписываемся на изменения модели игрока
            AddDisposable(_playerModel.IsMoving.Subscribe(OnMovingStateChanged));
            
            // Подписываемся на обновления
            _tickHandler.PhysicUpdate += HandleMovement;
            _tickHandler.FrameUpdate += CheckExitReached;
            _tickHandler.FrameUpdate += MoveView;
            
            Debug.Log("EscapeFromDarkPlayerPm: Input handling started");
        }

        private void MoveView(float deltaTime)
        {
            if (_playerView != null)
            {
                _playerView.UpdatePosition(_playerModel.Position.CurrentValue);
                _playerView.UpdateRotation(_playerModel.CurrentRotation.CurrentValue);
            }
        }
        private void OnMovingStateChanged(bool isMoving)
        {
            if (_playerView != null && _inputManager != null)
            {
                Vector2 inputDirection = _inputManager.GetJoystickInput();
                _playerView.UpdateMovementVisuals(inputDirection, isMoving);
            }
        }

        private void HandleMovement(float deltaTime)
        {

            Vector2 inputDirection = _inputManager.GetJoystickInput();
            
            if (inputDirection.magnitude > 0.1f)
            {
                Vector3 currentPosition = _playerModel.Position.CurrentValue;
                Vector3 targetPosition = currentPosition + new Vector3(inputDirection.x, inputDirection.y, 0) * _playerModel.MoveSpeed * deltaTime;
                
                // Проверяем, можно ли двигаться в эту позицию
                Vector2Int targetMazePos = _ctx.levelPm.GetMazePosition(targetPosition);
                
                if (_ctx.levelPm.IsValidMazePosition(targetMazePos.x, targetMazePos.y))
                {
                    _playerModel.SetPosition(targetPosition);
                    _playerModel.SetMoving(true);
                    _playerModel.SetMovementDirection(inputDirection.normalized);
                }
                // else
                // {
                //     _playerModel.SetMoving(false);
                //     _playerModel.SetMovementDirection(Vector2.zero);
                // }
            }
            else
            {
                _playerModel.SetMoving(false);
                _playerModel.SetMovementDirection(Vector2.zero);
            }
        }

        private void CheckExitReached(float deltaTime)
        {

            Vector3 playerPosition = _playerModel.Position.CurrentValue;
            Vector2Int playerMazePos = _ctx.levelPm.GetMazePosition(playerPosition);
            
            if (_ctx.levelPm.IsExitPosition(playerMazePos.x, playerMazePos.y))
            {
                Debug.Log("EscapeFromDarkPlayerPm: Player reached exit!");
                _ctx.onLevelCompleted?.Invoke();
            }
        }

        public void ResetToSpawn()
        {
            // Получаем новую позицию спавна
            _spawnPosition = _ctx.levelPm.LevelView.GetPlayerSpawnWorldPosition();
            
            // Перемещаем модель игрока на спавн
            _playerModel.ResetToPosition(_spawnPosition);
            
            // Останавливаем визуальное представление
            if (_playerView != null)
            {
                _playerView.Stop();
            }
            
            Debug.Log($"EscapeFromDarkPlayerPm: Player reset to spawn position: {_spawnPosition}");
        }

        public Vector3 GetPlayerPosition()
        {
            return _playerModel.Position.CurrentValue;
        }

        public EscapeFromDarkPlayerModel GetPlayerModel()
        {
            return _playerModel;
        }

        public EscapeFromDarkPlayerView GetPlayerView()
        {
            return _playerView;
        }

        protected override void OnDispose()
        {
            _tickHandler.PhysicUpdate -= HandleMovement;
            _tickHandler.FrameUpdate -= CheckExitReached;
            
            if (_playerView != null)
            {
                UnityEngine.Object.Destroy(_playerView.gameObject);
            }
        }
    }
}
