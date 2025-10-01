using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Games._2048.Scripts.View;
using Code.Games._2048.Scripts.Input;
using UnityEngine;
using R3;

namespace Code.Games._2048.Scripts.Gameplay
{
    internal class Game2048GameplayPm : BaseDisposable
    {
        internal struct Ctx
        {
            public Game2048SceneContextView sceneContextView;
            public Game2048InputPm inputPm;
            public GameObject cubePrefab;
            public float launchForce;
            public float spawnDelay;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();
        
        private Game2048CubeSpawnerPm _cubeSpawner;
        private Game2048CubeControllerPm _cubeController;
        private Game2048CubeMergeManagerPm _mergeManager;
        private IDisposable _spawnTimer;

        public Game2048GameplayPm(Ctx ctx)
        {
            _ctx = ctx;
            
            InitializeCubeSpawner();
            InitializeMergeManager();
            InitializeCubeController();
            StartGameCycle();
            
            AddDispose(_compositeDisposable);
        }

        private void InitializeCubeSpawner()
        {
            var spawnerCtx = new Game2048CubeSpawnerPm.Ctx
            {
                spawnPoint = _ctx.sceneContextView.GameSpawnPoint,
                cubePrefab = _ctx.cubePrefab,
                cancellationToken = _ctx.cancellationToken,
                onCubeCollision = null // Будет установлен после создания менеджера мержа
            };
            
            _cubeSpawner = new Game2048CubeSpawnerPm(spawnerCtx);
            AddDispose(_cubeSpawner);
        }

        private void InitializeMergeManager()
        {
            var mergeCtx = new Game2048CubeMergeManagerPm.Ctx
            {
                cubeSpawner = _cubeSpawner,
                cubePrefab = _ctx.cubePrefab,
                spawnPoint = _ctx.sceneContextView.GameSpawnPoint,
                mergeUpwardForce = _ctx.sceneContextView.MergeUpwardForce,
                cancellationToken = _ctx.cancellationToken
            };

            _mergeManager = new Game2048CubeMergeManagerPm(mergeCtx);
            AddDispose(_mergeManager);

            // Устанавливаем обработчик коллизий в спавнер
            _cubeSpawner.SetCollisionHandler(_mergeManager.OnCubeCollision);
        }

        private void InitializeCubeController()
        {
            var controllerCtx = new Game2048CubeControllerPm.Ctx
            {
                inputPm = _ctx.inputPm,
                launchForce = _ctx.launchForce,
                cancellationToken = _ctx.cancellationToken
            };

            _cubeController = new Game2048CubeControllerPm(controllerCtx);
            AddDispose(_cubeController);

            _cubeController.OnCubeLaunched
                .Subscribe(_ => ScheduleNextCube())
                .AddTo(_compositeDisposable);
        }

        private void StartGameCycle()
        {
            SpawnNewCube();
        }

        private void SpawnNewCube()
        {
            if (_ctx.cancellationToken.IsCancellationRequested) return;

            Game2048CubeView newCube = _cubeSpawner.CreateCube();
            if (newCube == null) return;

            // Передаем новый куб в существующий контроллер
            _cubeController.SetCurrentCube(newCube);
        }

        private void ScheduleNextCube()
        {
            _spawnTimer?.Dispose();
            
            _spawnTimer = Observable.Timer(TimeSpan.FromSeconds(_ctx.spawnDelay))
                .Subscribe(_ => SpawnNewCube())
                .AddTo(_compositeDisposable);
        }

        protected override void OnDispose()
        {
            _spawnTimer?.Dispose();
            base.OnDispose();
        }
    }
}
