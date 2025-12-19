using System;
using System.Threading;
using Disposable;
using Code.Core.Tools.Pool;
using GameShorts.CubeRunner.Data;
using GameShorts.CubeRunner.Gameplay;
using GameShorts.CubeRunner.Level;
using GameShorts.CubeRunner.View;
using R3;
using UnityEngine;

namespace GameShorts.CubeRunner.Logic
{
    internal class CubeRunnerMainScenePm : DisposableBase
    {
        internal struct Ctx
        {
            public CubeRunnerSceneContextView sceneContextView;
            public CancellationToken cancellationToken;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private readonly CubeRunnerGameplayPm _gameplayPm;
        private readonly CubeRunnerGameSettings _settings;
        private readonly LevelManager _levelManager;
        private readonly CubeManager _cubeManager;
        private readonly CubeRunnerCameraPm _cameraPm;
        private readonly CubeRunnerInputHandler _inputHandler;

        public CubeRunnerMainScenePm(Ctx ctx)
        {
            _ctx = ctx;
            _settings = _ctx.sceneContextView.GameSettings;

            if (_settings == null)
            {
                Debug.LogError("CubeRunnerGameSettings is not assigned in CubeRunnerSceneContextView.");
                return;
            }

            _levelManager = LevelManagerFactory.CreateLevelManager(new LevelManager.Ctx
            {
                gameSettings = _settings,
                tilesRoot = _ctx.sceneContextView.TilesRoot,
            });
            AddDisposable(_levelManager);

            _cubeManager = CubeManagerFactory.CreateCubeManager(new CubeManager.Ctx()
            {
                sceneContextView = _ctx.sceneContextView,
            });
            AddDisposable(_cubeManager);
            
            var cameraCtx = new CubeRunnerCameraPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cubeManager = _cubeManager,
                fixedHorizontalDistance = 0f
            };
            _cameraPm = CubeRunnerCameraPmFactory.CreateCubeRunnerCameraPm(cameraCtx);
            AddDisposable(_cameraPm);
            
            _inputHandler = CubeRunnerInputHandlerFactory.CreateCubeRunnerInputHandler(new CubeRunnerInputHandler.Ctx
            {
                isPaused = _ctx.isPaused,
            });
            AddDisposable(_inputHandler);
            AddDisposable(_inputHandler.Swipes.Subscribe(direction => _cubeManager.TryMove(direction)));

            CubeRunnerGameplayPm.Ctx gameplayCtx = new CubeRunnerGameplayPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                isPaused = _ctx.isPaused,
                onGameOver = HandleGameOver,
                cubeManager = _cubeManager,
                levelManager = _levelManager,
            };
            _gameplayPm = CubeRunnerGameplayPmFactory.CreateCubeRunnerGameplayPm(gameplayCtx);
            AddDisposable(_gameplayPm);
            
            _gameplayPm.StartNewLevel();
        }

        private void HandleGameOver()
        {
            _ctx.restartGame?.Invoke();
        }

        protected override void OnDispose()
        {
            _gameplayPm?.Dispose();
        }
    }
}

