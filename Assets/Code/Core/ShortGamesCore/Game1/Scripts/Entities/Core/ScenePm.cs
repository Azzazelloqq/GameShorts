using System.Linq;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Logic;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Code.Core.ShortGamesCore.Game1.Scripts.Core
{
    internal class ScenePm: BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public MainSceneContextView sceneContextView;
        }

        private readonly Ctx _ctx;

        public ScenePm(Ctx ctx)
        {
            _ctx = ctx;
            MainScenePm.Ctx mainSceneCtx = new MainScenePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                restartGame = () => SceneManager.LoadScene(SceneManager.GetActiveScene().name)
            };
            MainScenePm mainScenePm = new MainScenePm(mainSceneCtx);
            AddDispose(mainScenePm);
        }
    }
}