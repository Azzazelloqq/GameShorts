using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using R3;

namespace Code.Games
{
    internal class Game2048ScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public Game2048SceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;

        public Game2048ScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            Game2048MainScenePm.Ctx mainSceneCtx = new Game2048MainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame,
                isPaused = _ctx.isPaused
            };
            Game2048MainScenePm mainScenePm = new Game2048MainScenePm(mainSceneCtx);
            AddDispose(mainScenePm);
        }
    }
}
