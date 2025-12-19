using System;
using System.Threading;
using Disposable;
using Code.Core.Tools.Pool;
using GameShorts.Gardener.View;
using LightDI.Runtime;
using R3;

namespace GameShorts.Gardener.Core
{
    internal class GardenerCorePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public GardenerSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private IDisposable _scene;
        private readonly IPoolManager _poolManager;

        public GardenerCorePm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            GardenerScenePm.Ctx sceneCtx = new GardenerScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused
            };
            _scene = new GardenerScenePm(sceneCtx);
            AddDisposable(_scene);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            _poolManager?.Clear();
        }
    }
}