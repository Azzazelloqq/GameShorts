using System;
using System.Threading;
using Disposable;
using Disposable;
using GameShorts.FlyHumans.Gameplay;
using GameShorts.FlyHumans.Presenters;
using GameShorts.FlyHumans.View;
using R3;

namespace GameShorts.FlyHumans.Logic
{
    internal class FlyHumansMainScenePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public FlyHumansSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private CameraPm _cameraPm;
        private WorldBlocksPm _worldBlocksPm;
        private FlyHumansGameplayPm _gameplayPm;
        private FlyHumansUIPm _uiPm;

        public FlyHumansMainScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Создаем Camera Presenter
            if (_ctx.sceneContextView.CameraView != null && _ctx.sceneContextView.Character != null)
            {
                _cameraPm = new CameraPm(new CameraPm.Ctx
                {
                    cameraView = _ctx.sceneContextView.CameraView,
                    targetTransform = _ctx.sceneContextView.Character.transform
                });
                AddDisposable(_cameraPm);
            }
            
            // Создаем World Blocks Presenter (включая трафик на блоках)
            if (_ctx.sceneContextView.WorldBlocksView != null && _ctx.sceneContextView.Character != null)
            {
                _worldBlocksPm = WorldBlocksPmFactory.CreateWorldBlocksPm(new WorldBlocksPm.Ctx
                {
                    worldBlocksView = _ctx.sceneContextView.WorldBlocksView,
                    characterTransform = _ctx.sceneContextView.Character.transform
                });
                AddDisposable(_worldBlocksPm);
            }
            
            // Создаем Gameplay Presenter
            FlyHumansGameplayPm.Ctx gameplayCtx = new FlyHumansGameplayPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                isPaused = _ctx.isPaused,
                cameraPm = _cameraPm,
                worldBlocksPm = _worldBlocksPm
            };
            _gameplayPm = new FlyHumansGameplayPm(gameplayCtx);
            AddDisposable(_gameplayPm);
            
            // Создаем UI Presenter
            FlyHumansUIPm.Ctx uiCtx = new FlyHumansUIPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame,
                startGame = () => _gameplayPm.StartGame(),
                isPaused = _ctx.isPaused
            };
            _uiPm = new FlyHumansUIPm(uiCtx);
            AddDisposable(_uiPm);
        }
    }
}

