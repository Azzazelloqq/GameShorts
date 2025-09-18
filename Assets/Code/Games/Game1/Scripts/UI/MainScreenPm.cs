using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using InGameLogger;
using LightDI.Runtime;
using Logic.Entities;
using R3;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Logic.UI
{
    internal class MainScreenPm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public IEntitiesController entitiesController;
            public MainSceneContextView mainSceneContextView;
        }

        private readonly Ctx _ctx;
        private PlayerModel _playerModel;
        private MainScreenView _view;
        private List<LaserChargeUiView> _battaries;
        private readonly IPoolManager _poolManager;
        private readonly IResourceLoader _resourceLoader;
        private readonly IInGameLogger _logger;

        public MainScreenPm(Ctx ctx,
            [Inject] IPoolManager poolManager,
            [Inject] IInGameLogger logger, 
            [Inject] IResourceLoader resourceLoader)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _resourceLoader = resourceLoader;
            _logger = logger;
            _playerModel = _ctx.entitiesController.GetPlayerModel();

            Load();
        }

        protected override void OnDispose()
        {
            _ctx.mainSceneContextView.OnUpdated -= OnUpdated;
            base.OnDispose();
        }

        private async Task Load()
        {
            try
            {
                await LoadBaseUI();
                await LoadLasers();
                InitSubscribes();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load MainScreen: {ex.Message}");
                throw;
            }
        }

        private async Task LoadBaseUI()
        {
            var prefab =
                await _resourceLoader.LoadResourceAsync<GameObject>(ResourceIdsContainer.GameAsteroids.MainScreen,
                    _ctx.cancellationToken);
            var objView = AddComponent(Object.Instantiate(prefab, _ctx.mainSceneContextView.UiParent, false));
            _view = objView.GetComponent<MainScreenView>();
        }

        private async Task LoadLasers()
        {
            var prefab =
                await _resourceLoader.LoadResourceAsync<GameObject>(ResourceIdsContainer.GameAsteroids.LaserChargeUI,
                    _ctx.cancellationToken);

            _battaries = new List<LaserChargeUiView>(_ctx.mainSceneContextView.laserSettings.CountLaserShots);
            for (int i = 0; i < _ctx.mainSceneContextView.laserSettings.CountLaserShots; i++)
            {
                GameObject objView = AddComponent(Object.Instantiate(prefab, _view.LaserChargeHolder, false));
                var view = objView.GetComponent<LaserChargeUiView>();
                _battaries.Add(view);
            }
        }

        private void InitSubscribes()
        {
            _ctx.mainSceneContextView.OnUpdated += OnUpdated;
            AddDispose(_playerModel.Position.Subscribe(UpdatePos));
            AddDispose(_playerModel.Score.Subscribe(ScoreOnChanged));
            AddDispose(_playerModel.CurrentSpeed.Subscribe(UpdateCurSpeed));
            AddDispose(_playerModel.CurrentAngle.Subscribe(UpdateCurAngle));
        }

        private void ScoreOnChanged(int score)
        {
            _view.Score.text = $"Score: {score}";
        }

        private void OnUpdated(float deltaTime)
        {
            if (_playerModel.Charges.Count == 0)
                return;

            for (int i = 0; i < _battaries.Count; i++)
            {
                var valueCharge = _playerModel.Charges[i].Charge.Value;
                _battaries[i].Slider.value = valueCharge;
                _battaries[i].FillImage.color = valueCharge < 1f ? Color.yellow : Color.green;
            }
        }

        private void UpdateCurAngle(float angle)
        {
            _view.Angle.text = $"Angle: {Mathf.Abs(Mathf.Floor(angle))}";
        }

        private void UpdateCurSpeed(float speed)
        {
            _view.Speed.text = $"Speed: {Mathf.Floor(speed)}";
        }

        private void UpdatePos(Vector2 position)
        {
            _view.PosX.text = $"X: {position.x:N0}";
            _view.PosY.text = $"X: {position.y:N0}";
        }
    }
}