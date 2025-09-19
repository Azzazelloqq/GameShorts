using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using LightDI.Runtime;

namespace Code.Core.ShortGamesCore.Game2
{
    public class BoxTowerCorePm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public BoxTowerSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private IDisposable _gameScene;
        private readonly IDiContainer _diContainer;
        private readonly IPoolManager _poolManager;

        public BoxTowerCorePm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _diContainer = DiContainerFactory.CreateContainer();
            AddDispose(_diContainer);
            
            // Create game scene presenter
            BoxTowerGameScenePm.Ctx gameSceneCtx = new BoxTowerGameScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame
            };
            _gameScene = new BoxTowerGameScenePm(gameSceneCtx);
            AddDispose(_gameScene);
        }

        protected override void OnDispose()
        {
            _gameScene?.Dispose();
            _poolManager.Clear();
            base.OnDispose();
        }
    }
}
