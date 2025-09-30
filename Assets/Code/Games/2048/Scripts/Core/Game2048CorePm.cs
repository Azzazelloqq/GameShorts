using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.Tools.Pool;
using Code.Games._2048.Scripts.View;
using Code.Games._2048.Scripts.Logic;
using LightDI.Runtime;
using R3;

namespace Code.Games._2048.Scripts.Core
{
    internal class Game2048CorePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public Game2048SceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private IDisposable _scene;
        private readonly IDiContainer _diContainer;
        private readonly IPoolManager _poolManager;

        public Game2048CorePm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _diContainer = DiContainerFactory.CreateContainer();
            AddDispose(_diContainer);
            
            Game2048ScenePm.Ctx sceneCtx = new Game2048ScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame
            };
            _scene = new Game2048ScenePm(sceneCtx);
        }

        protected override void OnDispose()
        {
            _scene?.Dispose();
            _poolManager?.Clear();
            base.OnDispose();
        }
    }
}
