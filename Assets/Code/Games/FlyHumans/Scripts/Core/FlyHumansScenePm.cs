using System;
using System.Threading;
using Disposable;
using Disposable;
using GameShorts.FlyHumans.Logic;
using GameShorts.FlyHumans.View;
using R3;

namespace GameShorts.FlyHumans.Core
{
    internal class FlyHumansScenePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public FlyHumansSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;

        public FlyHumansScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            FlyHumansMainScenePm.Ctx mainSceneCtx = new FlyHumansMainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused
            };
            FlyHumansMainScenePm mainScenePm = new FlyHumansMainScenePm(mainSceneCtx);
            AddDisposable(mainScenePm);
        }
    }
}

