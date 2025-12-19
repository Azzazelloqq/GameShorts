using System;
using System.Threading;
using Disposable;
using GameShorts.Gardener.Logic;
using GameShorts.Gardener.View;
using R3;

namespace GameShorts.Gardener.Core
{
    internal class GardenerScenePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public GardenerSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private GardenerMainScenePm _mainScenePm;

        public GardenerScenePm(Ctx ctx) 
        {
            _ctx = ctx;
            
            GardenerMainScenePm.Ctx mainSceneCtx = new GardenerMainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused
            };
            _mainScenePm = new GardenerMainScenePm(mainSceneCtx);
            AddDisposable(_mainScenePm);
        }
    }
}