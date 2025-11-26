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

        public CubeRunnerScenePm(Ctx ctx)
        {
            CubeRunnerMainScenePm.Ctx mainSceneCtx = new CubeRunnerMainScenePm.Ctx
            {
                sceneContextView = ctx.sceneContextView,
                cancellationToken = ctx.cancellationToken,
                restartGame = ctx.restartGame,
                isPaused = ctx.isPaused
            };

            _mainScenePm = new CubeRunnerMainScenePm(mainSceneCtx);
            AddDispose(_mainScenePm);
        }
    }
}

