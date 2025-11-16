using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using GameShorts.CubeRunner.Logic;
using GameShorts.CubeRunner.View;
using R3;

namespace GameShorts.CubeRunner.Core
{
    internal class CubeRunnerScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CubeRunnerSceneContextView sceneContextView;
            public CancellationToken cancellationToken;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly CubeRunnerMainScenePm _mainScenePm;
        private readonly IPoolManager _poolManager;

        public CubeRunnerScenePm(Ctx ctx, IPoolManager poolManager)
        {
            _poolManager = poolManager;
            CubeRunnerMainScenePm.Ctx mainSceneCtx = new CubeRunnerMainScenePm.Ctx
            {
                sceneContextView = ctx.sceneContextView,
                cancellationToken = ctx.cancellationToken,
                restartGame = ctx.restartGame,
                isPaused = ctx.isPaused
            };

            _mainScenePm = new CubeRunnerMainScenePm(mainSceneCtx, _poolManager);
            AddDispose(_mainScenePm);
        }
    }
}

