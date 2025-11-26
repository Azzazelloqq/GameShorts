using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using GameShorts.CubeRunner.View;
using R3;

namespace GameShorts.CubeRunner.Core
{
    internal class CubeRunnerCorePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CubeRunnerSceneContextView sceneContextView;
            public CancellationToken cancellationToken;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly CubeRunnerScenePm _scene;

        public CubeRunnerCorePm(Ctx ctx)
        {
            CubeRunnerScenePm.Ctx sceneCtx = new CubeRunnerScenePm.Ctx
            {
                sceneContextView = ctx.sceneContextView,
                cancellationToken = ctx.cancellationToken,
                restartGame = ctx.restartGame,
                isPaused = ctx.isPaused
            };

            _scene = new CubeRunnerScenePm(sceneCtx);
            AddDispose(_scene);
        }
    }
}

