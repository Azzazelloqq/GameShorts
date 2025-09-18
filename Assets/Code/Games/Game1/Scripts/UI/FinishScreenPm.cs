using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Scene;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Logic.UI
{
    internal class FinishScreenPm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public Action restartGame;
            public IEntitiesController entitiesController;
            public MainSceneContextView mainSceneContextView;
        }

        private readonly Ctx _ctx;
        private FinishScreenView _view;
        private readonly IResourceLoader _resourceLoader;

        public FinishScreenPm(Ctx ctx,
            [Inject] IResourceLoader resourceLoader)
        {
            _ctx = ctx;
            _resourceLoader = resourceLoader;
            var player = _ctx.entitiesController.GetPlayerModel();
            
            _resourceLoader.LoadResource<GameObject>(ResourceIdsContainer.GameAsteroids.FinishScreen, prefab =>
            {
                GameObject objView = AddComponent(Object.Instantiate(prefab, _ctx.mainSceneContextView.UiParent, false));
                _view = objView.GetComponent<FinishScreenView>();
                
                _view.SetCtx(new FinishScreenView.Ctx
                {
                    reloadClicked = _ctx.restartGame
                });
                _view.ScoreLabel.text = $"YOUR SCORE: {player.Score.Value}";

            }, _ctx.cancellationToken);
        }
    }
}