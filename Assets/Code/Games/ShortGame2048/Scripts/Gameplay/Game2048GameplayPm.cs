using System;
using System.Collections.Generic;
using System.Threading;
using Disposable;
using LightDI.Runtime;
using UnityEngine;
using R3;
using TickHandler;
using CompositeDisposable = R3.CompositeDisposable;

namespace Code.Games
{
    internal class Game2048GameplayPm : DisposableBase
    {
        internal struct Ctx
        {
            public Game2048SceneContextView sceneContextView;
            public Game2048InputPm inputPm;
            public GameObject cubePrefab;
            public float launchForce;
            public float spawnDelay;
            public CancellationToken cancellationToken;
            public ReadOnlyReactiveProperty<bool> isPaused;
            public Action<int> gameOver;
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();
        private Dictionary<Guid, CubePm> _cubes = new();
        
        private Game2048CubeSpawnerPm _cubeSpawner;
        private Game2048CubeControllerPm _cubeController;
        private Game2048CubeMergeManagerPm _mergeManager;
        private float _spawnTimer;
        private CubePm _currentCube;
        
        private ReactiveProperty<int> _currentScore = new();
        
        public ReadOnlyReactiveProperty<int> CurrentScore => _currentScore;
        
        private readonly Dictionary<Rigidbody, Vector3> _savedVelocities = new();
        private readonly Dictionary<Rigidbody, Vector3> _savedAngularVelocities = new();
        private bool _requestScheduleNextCube;
        private readonly ITickHandler _tickHandler;

        public Game2048GameplayPm(Ctx ctx, [Inject] ITickHandler  tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            
            InitializeCubeSpawner();
            InitializeMergeManager();
            InitializeCubeController();
            SubscribeToPause();
            StartGameCycle();
            
            AddDisposable(_compositeDisposable);
            _tickHandler.FrameUpdate += TrySpawnNewCube;
        }

        private void TrySpawnNewCube(float deltaTime)
        {
            if (_ctx.isPaused.CurrentValue)
                return;
            
            if (!_requestScheduleNextCube)
                return;

            if (_spawnTimer > 0)
            {
                _spawnTimer -= deltaTime;
                return;
            }

            _requestScheduleNextCube = false;
            SpawnNewCube();
        }

        private void SubscribeToPause()
        {
            AddDisposable(_ctx.isPaused
                .Subscribe(isPaused =>
                {
                    if (isPaused)
                        OnPause();
                    else
                        OnResume();
                }));
        }

        private void InitializeCubeSpawner()
        {
            var spawnerCtx = new Game2048CubeSpawnerPm.Ctx
            {
                spawnPoint = _ctx.sceneContextView.GameSpawnPoint,
                cubePrefab = _ctx.cubePrefab,
                cancellationToken = _ctx.cancellationToken,
                onCubeCreated = (id,cube) => { _currentCube = cube;} ,
                onMegredCubeCreated = (id, cube) =>
                {
                    _cubes.Add(id, cube);
                    UpdateScore(cube);
                } ,
            };
            
            _cubeSpawner = new Game2048CubeSpawnerPm(spawnerCtx);
            AddDisposable(_cubeSpawner);
        }

        private void UpdateScore(CubePm cube)
        {
            var maxNumber = _cubeSpawner.UsedNumbers.Max;
            _currentScore.Value = maxNumber;
            Debug.Log($"Game2048GameplayPm: Score updated to {maxNumber}");
        }


        private void InitializeMergeManager()
        {
            var mergeCtx = new Game2048CubeMergeManagerPm.Ctx
            {
                cubeSpawner = _cubeSpawner,
                cubePrefab = _ctx.cubePrefab,
                spawnPoint = _ctx.sceneContextView.GameSpawnPoint,
                mergeUpwardForce = _ctx.sceneContextView.MergeUpwardForce,
                mergeForwardForce = _ctx.sceneContextView.MergeForwardForce,
                cancellationToken = _ctx.cancellationToken,
                cubes = _cubes,
                disposeCube = DisposeCube
            };

            _mergeManager = new Game2048CubeMergeManagerPm(mergeCtx);
            AddDisposable(_mergeManager);

            // Устанавливаем обработчик коллизий в спавнер
            _cubeSpawner.SetCollisionHandler( (cubeId1,cubeId2) =>
            {
                if (_currentCube != null && (cubeId1 == _currentCube.Id || cubeId2 == _currentCube.Id))
                {
                    _cubes.Add(_currentCube.Id, _currentCube);
                    _currentCube = null;
                }
                _mergeManager.OnCubeCollision(cubeId1, cubeId2);
            });
            
           
        }

        private void InitializeCubeController()
        {
            var controllerCtx = new Game2048CubeControllerPm.Ctx
            {
                inputPm = _ctx.inputPm,
                launchForce = _ctx.launchForce,
                minX = _ctx.sceneContextView.MinX,
                maxX = _ctx.sceneContextView.MaxX,
                cancellationToken = _ctx.cancellationToken
            };

            _cubeController = Game2048CubeControllerPmFactory.CreateGame2048CubeControllerPm(controllerCtx);
            AddDisposable(_cubeController);

            _cubeController.OnCubeLaunched
                .Subscribe(_ =>
                {
                    _requestScheduleNextCube = true;
                    _spawnTimer = _ctx.spawnDelay;
                })
                .AddTo(_compositeDisposable);
        }

        private void StartGameCycle()
        {
            SpawnNewCube();
            _tickHandler.PhysicUpdate += CheckGameOver;
        }

        private void CheckGameOver(float fixedDeltaTime)
        {
            if (_ctx.isPaused.CurrentValue)
                return;
            
            var isGameOver = false;
            var finishPoint = _ctx.sceneContextView != null ? _ctx.sceneContextView.FinishGamePoint : null;
            if (finishPoint == null)
                return;

            float spawnZ = finishPoint.position.z;
            float gameOverThreshold = spawnZ;//- 1f; // Offset в 1 единицу назад от точки спавна
            
            if (_currentCube != null && _currentCube.View != null &&
                _currentCube.View.transform.position.z > gameOverThreshold)
            {
                _cubes.Add(_currentCube.Id, _currentCube);
                _currentCube = null;
            }
            
            foreach (var cube in _cubes.Values)
            {
                // Проверяем только кубы которые уже были запущены (не под контролем)
                if (cube.View != null)
                {
                    var rb = cube.View.GetComponent<Rigidbody>();
                    
                    // Пропускаем кубы под контролем игрока (kinematic = true)
                    if (rb != null && rb.isKinematic)
                        continue;
                    
                    float cubeZ = cube.View.transform.position.z;
                    
                    // Куб откатился назад за линию Game Over
                    if (cubeZ < gameOverThreshold)
                    {
                        isGameOver = true;
                        break;
                    }
                }
            }
            
            if (isGameOver)
                GameOver();
        }

        private void GameOver()
        {
            _tickHandler.PhysicUpdate -= CheckGameOver;
            _tickHandler.FrameUpdate -= TrySpawnNewCube;
            if (_cubeController.CurrentCube != null)
            {
                DisposeCube(_cubeController.CurrentCube.Id);
            }
            
            var finalScore = _currentScore.Value;
            Debug.Log($"Game2048GameplayPm: Game Over with score {finalScore}");
            _ctx.gameOver.Invoke(finalScore);
        }

        private void SpawnNewCube()
        {
            Debug.Log("Game2048GameplayPm: SpawnNewCube called");
            
            if (_ctx.cancellationToken.IsCancellationRequested)
            {
                Debug.LogWarning("Game2048GameplayPm: CancellationToken is requested, aborting spawn");
                return;
            }

            var newCube = _cubeSpawner.CreateCube();
            if (newCube == null)
            {
                Debug.LogError("Game2048GameplayPm: Failed to create cube - newCube is null");
                return;
            }

            _cubeController.SetCurrentCube(newCube);
        }
        
        private void OnPause()
        {
            // Замораживаем физику всех кубов
            _savedVelocities.Clear();
            _savedAngularVelocities.Clear();
            
            foreach (var cube in _cubes.Values)
            {
                if (cube?.View != null)
                {
                    var rb = cube.View.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        // Сохраняем скорости
                        _savedVelocities[rb] = rb.linearVelocity;
                        _savedAngularVelocities[rb] = rb.angularVelocity;
                        
                        // Замораживаем
                        rb.isKinematic = true;
                    }
                }
            }
            _tickHandler.FrameUpdate -= TrySpawnNewCube;
            
            Debug.Log($"Game2048GameplayPm: Paused - frozen {_savedVelocities.Count} rigidbodies");
        }
        
        private void OnResume()
        {
            Debug.Log("Game2048GameplayPm: Resuming game - unfreezing physics for all cubes");
            
            // Размораживаем физику всех кубов
            foreach (var cube in _cubes.Values)
            {
                if (cube?.View != null)
                {
                    var rb = cube.View.GetComponent<Rigidbody>();
                    if (rb != null && _savedVelocities.ContainsKey(rb))
                    {
                        // Размораживаем
                        rb.isKinematic = false;
                        
                        // Восстанавливаем скорости
                        rb.linearVelocity = _savedVelocities[rb];
                        rb.angularVelocity = _savedAngularVelocities[rb];
                    }
                }
            }
            
            _savedVelocities.Clear();
            _savedAngularVelocities.Clear();
            
            _tickHandler.FrameUpdate += TrySpawnNewCube;
            Debug.Log("Game2048GameplayPm: Resumed - unfrozen all rigidbodies");
            
            // Возобновляем таймер спавна если нет текущего куба
            // (проверяем через контроллер - есть ли активный куб)
            // Для простоты просто ничего не делаем - следующий spawn произойдет после запуска следующего куба
        }

        private void DisposeCube(Guid cube1)
        {
            if (_cubes.TryGetValue(cube1, out var cube))
            {
                cube.Dispose();
                _cubes.Remove(cube1);
            }
        }
        
        protected override void OnDispose()
        {
            _tickHandler.PhysicUpdate -= CheckGameOver;
            _tickHandler.FrameUpdate -= TrySpawnNewCube;
            foreach (var cube in _cubes.Values)
            {
                cube?.Dispose();
            }
            _cubes.Clear();
            _currentCube?.Dispose();
            _currentScore?.Dispose();
            base.OnDispose();
        }
    }
}
