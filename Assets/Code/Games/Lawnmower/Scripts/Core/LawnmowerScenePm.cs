using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Logic;
using LightDI;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Core
{
    internal class LawnmowerScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public LawnmowerSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;
        private IDisposable _mainScene;

        public LawnmowerScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            var mainSceneCtx = new LawnmowerMainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame
            };
            
            _mainScene = LawnmowerMainScenePmFactory.CreateLawnmowerMainScenePm(mainSceneCtx);
            AddDispose(_mainScene);
        }
    }
}
