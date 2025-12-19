using System;
using System.Threading;
using Disposable;
using GameShorts.FlyHumans.View;
using R3;

namespace GameShorts.FlyHumans.Core
{
    internal class FlyHumansCorePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public FlyHumansSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private IDisposable _scene;

        public FlyHumansCorePm(Ctx ctx)
        {
            _ctx = ctx;
            
            FlyHumansScenePm.Ctx sceneCtx = new FlyHumansScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused
            };
            _scene = new FlyHumansScenePm(sceneCtx);
            AddDisposable(_scene);
        }
    }
}

