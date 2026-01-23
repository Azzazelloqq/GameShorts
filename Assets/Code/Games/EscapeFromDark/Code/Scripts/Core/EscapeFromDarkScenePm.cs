using System;
using System.Threading;
using Disposable;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Logic;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using Code.Core.Tools;
using LightDI;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Core
{
    internal class EscapeFromDarkScenePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public EscapeFromDarkSceneContextView sceneContextView;
            public IReadOnlyReactiveTrigger startGame;
        }

        private readonly Ctx _ctx;

        public EscapeFromDarkScenePm(Ctx ctx)
        {
            _ctx = ctx;
            var mainSceneCtx = new EscapeFromDarkMainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                startGame = _ctx.startGame
            };
            var mainScenePm = EscapeFromDarkMainScenePmFactory.CreateEscapeFromDarkMainScenePm(mainSceneCtx);
            AddDisposable(mainScenePm);
        }
    }
}
