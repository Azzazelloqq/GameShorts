using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using R3;

namespace Code.Games
{
    internal class Game2048CorePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public Game2048SceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
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
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused
            };
            _scene = new Game2048ScenePm(sceneCtx);
            AddDispose(_scene);
        }

        protected override void OnDispose()
        {
            // Сначала вызываем base.OnDispose() - это disposal всех AddDispose элементов
            // включая _scene и _diContainer, что приведет к возврату всех кубов в пул
            base.OnDispose();
            
            // ПОСЛЕ того как все кубы вернулись в пул, очищаем его
            _poolManager?.Clear();
        }
    }
}
