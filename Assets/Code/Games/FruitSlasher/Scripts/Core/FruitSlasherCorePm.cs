using System.Threading;
using System;
using Code.Core.Tools.Pool;
using Code.Games.FruitSlasher.Scripts.Logic;
using Code.Games.FruitSlasher.Scripts.View;
using Disposable;
using LightDI.Runtime;
using R3;

namespace Code.Games.FruitSlasher.Scripts.Core
{
    internal class FruitSlasherCorePm: DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public FruitSlasherSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }
        
        private readonly Ctx _ctx;
        private IDisposable _scene;
        private readonly IDiContainer _diContainer;
        private readonly IPoolManager _poolManager;

        public FruitSlasherCorePm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _diContainer = DiContainerFactory.CreateLocalContainer();
            AddDisposable(_diContainer);
            
            FruitSlasherScenePm.Ctx sceneCtx = new FruitSlasherScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused
            };
            _scene = new FruitSlasherScenePm(sceneCtx);
            AddDisposable(_scene);
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            
            _poolManager?.Clear();
        }
    }
}