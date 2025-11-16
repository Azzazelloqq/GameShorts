using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using GameShorts.CubeRunner.Gameplay;
using GameShorts.CubeRunner.View;
using R3;

namespace GameShorts.CubeRunner.Logic
{
    internal class CubeRunnerMainScenePm : BaseDisposable
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
        private readonly IPoolManager _poolManager;

        public CubeRunnerMainScenePm(Ctx ctx, IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            CubeRunnerGameplayPm.Ctx gameplayCtx = new CubeRunnerGameplayPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                isPaused = _ctx.isPaused,
                onGameOver = HandleGameOver
            };

            _gameplayPm = new CubeRunnerGameplayPm(gameplayCtx, _poolManager);
            AddDispose(_gameplayPm);
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

