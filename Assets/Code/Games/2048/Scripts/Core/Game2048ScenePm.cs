using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Games._2048.Scripts.View;
using Code.Games._2048.Scripts.Logic;

namespace Code.Games._2048.Scripts.Core
{
    internal class Game2048ScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public Game2048SceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;

        public Game2048ScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            Game2048MainScenePm.Ctx mainSceneCtx = new Game2048MainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame
            };
            Game2048MainScenePm mainScenePm = new Game2048MainScenePm(mainSceneCtx);
            AddDispose(mainScenePm);
        }
    }
}
