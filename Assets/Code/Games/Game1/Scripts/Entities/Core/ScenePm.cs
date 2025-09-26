using System;
using System.Linq;
using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Logic;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Core.ShortGamesCore.Game1.Scripts.Core
{
    internal class ScenePm: BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public MainSceneContextView sceneContextView;
            public Action restartGame;
        }

        private readonly Ctx _ctx;

        public ScenePm(Ctx ctx)
        {
            _ctx = ctx;
            MainScenePm.Ctx mainSceneCtx = new MainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = _ctx.restartGame
            };
            MainScenePm mainScenePm = MainScenePmFactory.CreateMainScenePm(mainSceneCtx);
            AddDispose(mainScenePm);
        }
    }
}