using System;
using System.Threading;
using Disposable;
using Cysharp.Threading.Tasks;
using GameShorts.Gardener.Core;
using GameShorts.Gardener.Gameplay;
using GameShorts.Gardener.View;
using R3;

namespace GameShorts.Gardener.Logic
{
    internal class GardenerMainScenePm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public GardenerSceneContextView sceneContextView;
            public Action restartGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private GardenerGameplayPm _gameplayPm;
        private GardenerUIPm _uiPm;

        public GardenerMainScenePm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Создаем Gameplay Presenter
            GardenerGameplayPm.Ctx gameplayCtx = new GardenerGameplayPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                cancellationToken = _ctx.cancellationToken,
                isPaused = _ctx.isPaused,
                availablePlants = _ctx.sceneContextView.AvailablePlants,
                gameSettings = _ctx.sceneContextView.GameSettings
            };
            _gameplayPm = GardenerGameplayPmFactory.CreateGardenerGameplayPm(gameplayCtx);
            
            // Создаем UI Presenter
            GardenerUIPm.Ctx uiCtx = new GardenerUIPm.Ctx
            {
                MainUIView = _ctx.sceneContextView.MainUIView,
                ShopUIView = _ctx.sceneContextView.ShopUIView,
                PlaceableItemsPanel = _ctx.sceneContextView.PlaceableItemsPanel,
                CancellationToken = _ctx.cancellationToken,
                Money = _gameplayPm.Money,
                AvailablePlants = _gameplayPm.AvailablePlants,
                OnBuyPlant = (plant) => _gameplayPm.BuyPlant(plant),
                ModeManager = _gameplayPm.ModeManager,
                MainCamera = _ctx.sceneContextView.MainCamera,
                GardenBounds = _ctx.sceneContextView.GardenBounds,
                FindPlotAtPosition = (pos) => _gameplayPm.FindPlotAtPosition(pos)
            };
            _uiPm = new GardenerUIPm(uiCtx);
        }
        
        protected override void OnDispose()
        {
            _uiPm?.Dispose();
            _gameplayPm?.Dispose();
        }

        public async UniTask PreloadAsync(CancellationToken cancellationToken = default)
        {
            if (_gameplayPm != null)
            {
                await _gameplayPm.PreloadAsync(cancellationToken);
            }

            if (_uiPm != null)
            {
                await _uiPm.PreloadAsync(cancellationToken);
            }
        }
    }
}