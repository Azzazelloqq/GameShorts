using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using GameShorts.CubeRunner.Data;
using GameShorts.CubeRunner.Level;
using GameShorts.CubeRunner.View;
using LightDI.Runtime;
using R3;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameShorts.CubeRunner.Gameplay
{
    internal class CubeRunnerGameplayPm : BaseDisposable
    {
        internal struct Ctx
        {
            public CubeRunnerSceneContextView sceneContextView;
            public CancellationToken cancellationToken;
            public ReactiveProperty<bool> isPaused;
            public Action onGameOver;
        }

        private readonly Ctx _ctx;
        private readonly CubeRunnerGameSettings _settings;
        private readonly CubeRunnerInputHandler _inputHandler;
        private readonly CubeController _cubeController;
        private readonly LevelGenerator _levelGenerator;
        private readonly WorldMover _worldMover;
        private readonly CubeRunnerGameState _gameState;
        private readonly IDisposable _inputSubscription;
        private readonly CancellationTokenRegistration _cancellationRegistration;
        private DifficultyConfig _currentDifficulty;
        private CubeView _spawnedCubeView;
        private readonly IPoolManager _poolManager;

        private Vector2Int _startGridPosition;
        private readonly float _swipeThresholdPixels = 50f;
        private bool _worldMovementStarted;

        public CubeRunnerGameplayPm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _settings = _ctx.sceneContextView.GameSettings;

            if (_settings == null)
            {
                Debug.LogError("CubeRunnerGameSettings is not assigned in CubeRunnerSceneContextView.");
                return;
            }

            _startGridPosition = CalculateStartGridPosition();

            Vector3 originLocal = _ctx.sceneContextView.CubeSpawnPoint != null
                ? _ctx.sceneContextView.CubeSpawnPoint.localPosition
                : Vector3.zero;

            _levelGenerator = new LevelGenerator(new LevelGenerator.Ctx
            {
                gameSettings = _settings,
                tilesRoot = _ctx.sceneContextView.TilesRoot,
                poolManager = _poolManager,
                originLocalPosition = originLocal
            });
            AddDispose(_levelGenerator);
            _levelGenerator.DifficultyLevelChanged += OnDifficultyChanged;
            _levelGenerator.Initialize();
            _levelGenerator.EnsureTilesAhead(_startGridPosition);

            CubeView cubeViewInstance = SpawnCube(originLocal);
            if (cubeViewInstance == null)
            {
                Debug.LogError("CubeRunnerGameplayPm: Failed to spawn cube view.");
                return;
            }

            _cubeController = new CubeController(new CubeController.Ctx
            {
                cubeView = cubeViewInstance,
                originLocalPosition = originLocal,
                startGridPosition = _startGridPosition,
                tileSize = _settings.TileSize,
                resolveOccupancy = _levelGenerator.ResolveOccupancy
            });
            AddDispose(_cubeController);

            _inputHandler = new CubeRunnerInputHandler(new CubeRunnerInputHandler.Ctx
            {
                isPaused = _ctx.isPaused,
                swipeThreshold = _swipeThresholdPixels
            });
            AddDispose(_inputHandler);

            _inputSubscription = _inputHandler.Swipes.Subscribe(OnSwipe);
            AddDispose(_inputSubscription);

            _worldMover = new WorldMover(new WorldMover.Ctx
            {
                worldRoot = _ctx.sceneContextView.WorldRoot,
                gameSettings = _settings,
                isPaused = _ctx.isPaused
            });
            AddDispose(_worldMover);
            _worldMover.ResetDistance();
            _worldMover.SetSpeedMultiplier(1f);
            _worldMover.SetMovementEnabled(false);
            _worldMovementStarted = false;

            _gameState = new CubeRunnerGameState(new CubeRunnerGameState.Ctx
            {
                cubeController = _cubeController,
                isPaused = _ctx.isPaused,
                hasTileAtGridPosition = _levelGenerator.HasTile
            });
            AddDispose(_gameState);
            AddDispose(_gameState.GameOver.Subscribe(_ => HandleGameOver()));

            if (_ctx.cancellationToken.CanBeCanceled)
            {
                _cancellationRegistration = _ctx.cancellationToken.Register(OnCancellation);
            }
        }

        private void OnSwipe(CubeRunnerSwipeDirection direction)
        {
            Vector2Int delta = direction switch
            {
                CubeRunnerSwipeDirection.Up => Vector2Int.up,
                CubeRunnerSwipeDirection.Down => Vector2Int.down,
                CubeRunnerSwipeDirection.Left => Vector2Int.left,
                CubeRunnerSwipeDirection.Right => Vector2Int.right,
                _ => Vector2Int.zero
            };

            if (delta == Vector2Int.zero)
            {
                return;
            }

            CubeMoveResult result = _cubeController.TryMove(delta);
            if (result == CubeMoveResult.Blocked || result == CubeMoveResult.None)
            {
                return;
            }

            if (!_worldMovementStarted)
            {
                _worldMover.EnableMovement();
                _worldMovementStarted = true;
            }

            _levelGenerator.EnsureTilesAhead(_cubeController.CurrentGridPosition);
            _levelGenerator.CullTilesBehind(_cubeController.CurrentGridPosition.y);
        }

        private void HandleGameOver()
        {
            _worldMover.DisableMovement();
            _worldMovementStarted = false;
            _ctx.onGameOver?.Invoke();
        }

        private void OnDifficultyChanged(DifficultyConfig config)
        {
            _currentDifficulty = config;
            float multiplier = config != null && config.WorldSpeedMultiplier > 0f
                ? config.WorldSpeedMultiplier
                : 1f;
            _worldMover.SetSpeedMultiplier(multiplier);
        }

        private void OnCancellation()
        {
            Dispose();
        }

        protected override void OnDispose()
        {
            _worldMover.DisableMovement();
            if (_levelGenerator != null)
            {
                _levelGenerator.DifficultyLevelChanged -= OnDifficultyChanged;
            }

            if (_spawnedCubeView != null)
            {
                if (_settings?.CubePrefab != null)
                {
                    _poolManager.Return(_settings.CubePrefab.gameObject, _spawnedCubeView.gameObject);
                }
                _spawnedCubeView = null;
            }

            _cancellationRegistration.Dispose();
            base.OnDispose();
        }

        private Vector2Int CalculateStartGridPosition()
        {
            int laneCount = Mathf.Max(1, _settings.LaneCount);
            int desiredLaneIndex = Mathf.Min(2, laneCount - 1); // third lane (0-based)
            int minLaneIndex = -(laneCount / 2);
            int lane = minLaneIndex + desiredLaneIndex;
            return new Vector2Int(lane, 0);
        }

        private CubeView SpawnCube(Vector3 originLocal)
        {
            CubeView cubeView = null;
            Transform parent = _ctx.sceneContextView.WorldRoot != null
                ? _ctx.sceneContextView.WorldRoot
                : _ctx.sceneContextView.transform;

            GameObject cubePrefabObject = _settings?.CubePrefab != null
                ? _settings.CubePrefab.gameObject
                : null;

            if (cubePrefabObject != null)
            {
                GameObject cubeObject = _poolManager.Get(cubePrefabObject, parent);
                if (cubeObject != null)
                {
                    cubeView = cubeObject.GetComponent<CubeView>() ?? cubeObject.GetComponentInChildren<CubeView>();
                    if (cubeView == null)
                    {
                        cubeView = cubeObject.AddComponent<CubeView>();
                    }
                    cubeObject.SetActive(true);
                }
            }

            if (cubeView == null)
            {
                Debug.LogError("CubeRunnerGameplayPm: Cube prefab is missing or pool did not provide an instance.");
                return null;
            }

            if (cubeView.transform.parent != parent)
            {
                cubeView.transform.SetParent(parent, false);
            }

            Vector3 startLocalPosition = originLocal;
            startLocalPosition.x += _startGridPosition.x * _settings.TileSize;
            startLocalPosition.z += _startGridPosition.y * _settings.TileSize;
            cubeView.LocalPosition = startLocalPosition;
            cubeView.SetRotation(Quaternion.identity);
            cubeView.SetScale(Vector3.one);
            _spawnedCubeView = cubeView;
            return cubeView;
        }
    }
}

