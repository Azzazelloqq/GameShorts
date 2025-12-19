using System;
using System.Threading;
using System.Threading.Tasks;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using Disposable;
using InGameLogger;
using LightDI.Runtime;
using ResourceLoader;
using TickHandler;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Logic.UI
{
    internal class FinishScreenPm : DisposableBase
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
        private readonly IPoolManager _poolManager;
        private readonly IInGameLogger _logger;
        private readonly ITickHandler _tickHandler;

        public FinishScreenPm(Ctx ctx,
            [Inject] IPoolManager poolManager,
            [Inject] IInGameLogger logger, 
            [Inject] IResourceLoader resourceLoader, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _resourceLoader = resourceLoader;
            _logger = logger;
            _tickHandler = tickHandler;
            _ = Load();
        }
        
        private async Task Load()
        {
            try
            {
                await LoadBaseUI();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load MainScreen: {ex.Message}");
                throw;
            }
        }

        protected override void OnDispose()
        {
            base.OnDispose();
            
            Object.Destroy(_view.gameObject);
        }

        private async Task LoadBaseUI()
        {
            var prefab =
                await _resourceLoader.LoadResourceAsync<GameObject>(ResourceIdsContainer.GameAsteroids.FinishScreen,
                    _ctx.cancellationToken);
            var objView = Object.Instantiate(prefab, _ctx.mainSceneContextView.UiParent, false);
            _view = objView.GetComponent<FinishScreenView>();
            var player = _ctx.entitiesController.GetPlayerModel();
            _view.SetCtx(new FinishScreenView.Ctx
            {
                reloadClicked = _ctx.restartGame
            });
            _view.ScoreLabel.text = $"YOUR SCORE: {player.Score.Value}";
        }
    }
}