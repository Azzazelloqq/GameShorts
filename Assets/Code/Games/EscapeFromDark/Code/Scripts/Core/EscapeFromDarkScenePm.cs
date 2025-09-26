using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Logic;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using LightDI;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Core
{
    internal class EscapeFromDarkScenePm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public EscapeFromDarkSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;

        public EscapeFromDarkScenePm(Ctx ctx)
        {
            _ctx = ctx;
            var mainSceneCtx = new EscapeFromDarkMainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame
            };
            var mainScenePm = EscapeFromDarkMainScenePmFactory.CreateEscapeFromDarkMainScenePm(mainSceneCtx);
            AddDispose(mainScenePm);
        }
    }
}
