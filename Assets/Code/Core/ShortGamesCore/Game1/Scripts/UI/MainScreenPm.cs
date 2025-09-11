using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using LightDI.Runtime;
using Logic.Entities;
using R3;
using ResourceLoader;
using UnityEngine;

namespace Logic.UI
{
    public class MainScreenPm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public IEntitiesController entitiesController;
            public MainSceneContextView mainSceneContextView;
        }

        private readonly Ctx _ctx;
        private PlayerModel _playerModel;
        private MainScreenView _view;
        private LaserChargeUiView[] _battaries;
        private readonly IPoolManager _poolManager;
        private readonly IResourceLoader _resourceLoader;

        public MainScreenPm(Ctx ctx, 
            [Inject] IPoolManager poolManager,
            [Inject] IResourceLoader resourceLoader)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _resourceLoader = resourceLoader;
            _resourceLoader.LoadResource<GameObject>(ResourceIdsContainer.GameAsteroids.MainScreen,
                prefab =>
            {
                GameObject objView = AddComponent(Object.Instantiate(prefab, _ctx.mainSceneContextView.UiParent, false));
                _view = objView.GetComponent<MainScreenView>();
            }, _ctx.cancellationToken);
            
            _playerModel = _ctx.entitiesController.GetPlayerModel();
            _battaries = new LaserChargeUiView[_playerModel.Charges.Length];
            
            _resourceLoader.LoadResource<GameObject>(ResourceIdsContainer.GameAsteroids.LaserChargeUI, prefab =>
            {
                for (int i = 0; i < _battaries.Length; i++)
                {
                    GameObject objView = AddComponent(Object.Instantiate(prefab, _view.LaserChargeHolder, false));
                    _battaries[i] = objView.GetComponent<LaserChargeUiView>();
                }
            }, _ctx.cancellationToken);
            
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
            for (int i = 0; i < _battaries.Length; i++)
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