using System;
using System.Threading;
using Disposable;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.UI;
using UnityEngine;
using R3;
using LightDI.Runtime;
using TickHandler;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Player
{
    internal class LawnmowerPlayerPm : DisposableBase
    {
        internal struct Ctx
        {
            public LawnmowerSceneContextView sceneContextView;
            public LawnmowerLevelManager levelManager;
            public Action onLevelCompleted;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private readonly IInputManager _inputManager;
        
        private PlayerView _playerView;
        private LawnmowerPlayerModel _playerModel;
        private LawnmowerPlayerMoverPm _playerMover;
        private GrassContainerManager _containerManager;
        private FarmerUIPm _farmerUIPm;
        private MainGameUIPm _mainGameUIPm;
        private Vector3 _spawnPosition;
        
        // Grass cutting tracking
        private float _lastGrassCutTime = 0f;
        private readonly ITickHandler _tickHandler;

        public LawnmowerPlayerPm(Ctx ctx, 
            [Inject] IInputManager inputManager, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            _tickHandler = tickHandler;
            
            InitializePlayerModel();
            SpawnPlayer();
            InitializePlayerMover();
            InitializeContainerManager();
            InitializeFarmerUI();
            InitializeMainGameUI();
            StartInputHandling();
        }

        private void InitializePlayerModel()
        {
            // Получаем позицию спавна из текущего уровня
            LevelView currentLevel = _ctx.levelManager.GetCurrentLevel();
            _spawnPosition = currentLevel.GetPlayerSpawnPosition();
            
            _playerModel = new LawnmowerPlayerModel();
            _playerModel.Position.Value = _spawnPosition;
            
            // Применяем настройки из SceneContextView
            var settings = _ctx.sceneContextView.PlayerSettingsAsset;
            _playerModel.MaxSpeed.Value = settings.MaxSpeed;
            _playerModel.AccelerationSpeed.Value = settings.Acceleration;
            _playerModel.DecelerationSpeed.Value = settings.Deceleration;
            _playerModel.UseAcceleration.Value = settings.UseAcceleration;
            _playerModel.RotationSpeed.Value = settings.RotationSpeed;
            _playerModel.InstantRotation.Value = settings.InstantRotation;
            _playerModel.CuttingRadius.Value = settings.CuttingRadius;
            
            string movementType = settings.UseAcceleration ? "with acceleration" : "instant";
            string rotationType = settings.InstantRotation ? "instant" : "smooth";
            Debug.Log($"LawnmowerPlayerPm: Applied settings - Speed: {_playerModel.MaxSpeed.Value} ({movementType}), Rotation: {rotationType}");
        }

        private void SpawnPlayer()
        {
            if (_ctx.sceneContextView.PlayerPrefab == null)
            {
                Debug.LogError("LawnmowerPlayerPm: Player prefab is not assigned!");
                return;
            }
            
            // Создаем игрока
            _playerView = UnityEngine.Object.Instantiate(_ctx.sceneContextView.PlayerPrefab, _spawnPosition, Quaternion.identity);
            
            Debug.Log($"Player spawned at position: {_spawnPosition}");
        }

        private void InitializePlayerMover()
        {
            var moverCtx = new LawnmowerPlayerMoverPm.Ctx
            {
                playerModel = _playerModel,
                useAcceleration = _playerModel.UseAcceleration.Value,
                levelManager = _ctx.levelManager
            };
            
            _playerMover = LawnmowerPlayerMoverPmFactory.CreateLawnmowerPlayerMoverPm(moverCtx);
            AddDisposable(_playerMover);
        }

        private void InitializeContainerManager()
        {
            // Находим зону опустошения на текущем уровне
            LevelView currentLevel = _ctx.levelManager.GetCurrentLevel();
            EmptyingZoneView emptyingZoneView = currentLevel.EmptyingZone;
            EmptyingZonePm emptyingZonePm = null;
            if (emptyingZoneView != null)
            {
                var emptyingZoneCtx = new EmptyingZonePm.Ctx
                {
                    view = currentLevel.EmptyingZone,
                    zoneName = "Emptying Zone",
                    normalColor = Color.yellow,
                    activeColor = Color.green
                };
                emptyingZonePm = new EmptyingZonePm(emptyingZoneCtx);
                AddDisposable(emptyingZonePm);
            }
            
            var containerCtx = new GrassContainerManager.Ctx
            {
                playerModel = _playerModel,
                settings = _ctx.sceneContextView.PlayerSettingsAsset,
                emptyingZonePm = emptyingZonePm
            };
            
            _containerManager = new GrassContainerManager(containerCtx);
            AddDisposable(_containerManager);
            
            Debug.Log($"Container manager initialized with emptying zone: {emptyingZoneView?.name ?? "None"}");
        }

        private void InitializeFarmerUI()
        {
            // Создаем Presenter для фермера UI
            var farmerUICtx = new FarmerUIPm.Ctx
            {
                playerModel = _playerModel,
                settings = _ctx.sceneContextView.PlayerSettingsAsset,
                targetCanvas = _ctx.sceneContextView.UiCanvas,
                worldCamera = _ctx.sceneContextView.MainCamera,
            };
            
            _farmerUIPm = new FarmerUIPm(farmerUICtx);
            AddDisposable(_farmerUIPm);
            
            Debug.Log("Farmer UI MVP initialized");
        }

        private void InitializeMainGameUI()
        {
            var mainGameUICtx = new MainGameUIPm.Ctx
            {
                view = _ctx.sceneContextView.MainUi,
                levelManager = _ctx.levelManager,
                onLevelCompleted = HandleLevelCompleted
            };
                
            _mainGameUIPm = new MainGameUIPm(mainGameUICtx);
            AddDisposable(_mainGameUIPm);
        }

        private void StartInputHandling()
        {
            // Подписываемся на изменения позиции модели для синхронизации с визуальным представлением
            AddDisposable(_playerModel.Position.Subscribe(OnPositionChanged));
            AddDisposable(_playerModel.IsMoving.Subscribe(OnMovingStateChanged));
            AddDisposable(_playerModel.CurrentRotation.Subscribe(OnRotationChanged));
            
            _tickHandler.PhysicUpdate += HandleGrassCutting;
            _tickHandler.FrameUpdate += HandleContainerEmptying;
            _tickHandler.FrameUpdate += HandleMainGameUI;
        }

        private void OnPositionChanged(Vector2 newPosition)
        {
            if (_playerView != null)
            {
                _playerView.UpdatePosition(newPosition);
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

        private void OnRotationChanged(float newRotation)
        {
            if (_playerView != null)
            {
                _playerView.UpdateRotation(newRotation);
            }
        }

        private void HandleGrassCutting(float deltaTime)
        {
            if (!_playerModel.IsMoving.Value) return;
            
            // Проверяем, не заполнен ли контейнер
            if (_containerManager != null && _containerManager.IsContainerFull()) return;
            
            // Используем интервал из настроек
            float cuttingInterval = _ctx.sceneContextView.PlayerSettingsAsset.CuttingInterval;
            if (Time.time - _lastGrassCutTime < cuttingInterval) return;
            
            _lastGrassCutTime = Time.time;
            
            // Получаем текущий уровень
            LevelView currentLevel = _ctx.levelManager.GetCurrentLevel();
            if (currentLevel == null) return;
            
            // Проверяем, находится ли игрок в границах уровня
            Vector3 playerPosition = _playerModel.Position.Value;
            if (!currentLevel.IsPositionInBounds(playerPosition)) return;
            
            // Стрижем траву во всех полях текущего уровня и подсчитываем подстриженные тайлы
            int totalCutTiles = 0;
            foreach (var grassField in currentLevel.GrassFields)
            {
                if (grassField != null && !grassField.IsCompleted)
                {
                    int cutTiles = grassField.CutGrassAtPosition(playerPosition, _playerModel.CuttingRadius.Value);
                    totalCutTiles += cutTiles;
                }
            }
            
            // Добавляем траву в контейнер
            if (totalCutTiles > 0 && _containerManager != null)
            {
                _containerManager.AddGrass(totalCutTiles);
            }
            
            // Воспроизводим звук стрижки
            if (_playerView != null)
            {
                _playerView.PlayGrassCuttingSound();
            }
        }

        private void HandleContainerEmptying(float deltaTime)
        {
            if (_containerManager != null)
            {
                _containerManager.UpdateEmptying(deltaTime);
            }
        }

        private void HandleMainGameUI(float deltaTime)
        {
            if (_mainGameUIPm != null)
            {
                _mainGameUIPm.UpdateLevelProgress(deltaTime);
            }
        }

        private void HandleLevelCompleted()
        {
            Debug.Log("Level completed! Calling onLevelCompleted callback...");
            _ctx.onLevelCompleted?.Invoke();
        }

        /// <summary>
        /// Переинициализация контейнера для нового уровня
        /// </summary>
        public void ReinitializeForNewLevel()
        {
            // Сбрасываем контейнер
            _playerModel.GrassContainerCurrentAmount.Value = 0f;
            _playerModel.IsInEmptyingZone.Value = false;
            _playerModel.EmptyingProgress.Value = 0f;
            
            // Удаляем старый контейнер менеджер
            if (_containerManager != null)
            {
                _containerManager.Dispose();
                _containerManager = null;
            }
            
            // Создаем новый контейнер менеджер для нового уровня
            InitializeContainerManager();
            
            Debug.Log("Container manager reinitialized for new level");
        }

        public void ResetToSpawn()
        {
            // Получаем новую позицию спавна из текущего уровня
            LevelView currentLevel = _ctx.levelManager.GetCurrentLevel();
            _spawnPosition = currentLevel?.GetPlayerSpawnPosition() ?? _spawnPosition;
            
            // Перемещаем модель игрока на спавн
            _playerModel.Position.Value = _spawnPosition;
            _playerModel.CurrentSpeed.Value = 0f;
            _playerModel.IsMoving.Value = false;
            
            // Останавливаем визуальное представление
            if (_playerView != null)
            {
                _playerView.Stop();
            }
            
            Debug.Log($"Player reset to spawn position: {_spawnPosition}");
        }

        public PlayerView GetPlayerView()
        {
            return _playerView;
        }

        public Vector3 GetPlayerPosition()
        {
            return _playerModel.Position.Value;
        }

        public LawnmowerPlayerModel GetPlayerModel()
        {
            return _playerModel;
        }

        protected override void OnDispose()
        {
            _tickHandler.PhysicUpdate -= HandleGrassCutting;
            
            if (_playerView != null)
            {
                UnityEngine.Object.Destroy(_playerView.gameObject);
                _playerView = null;
            }
            
            base.OnDispose();
        }
    }
}
