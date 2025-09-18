using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Entities.Core;
using Logic.Scene;
using ResourceLoader;
using UnityEngine;

namespace Logic.Enemy.Asteroid
{
    internal class AsteroidPm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public MainSceneContextView sceneContextView;
            public AsteroidModel asteroidModel;
            public Action<Vector3> requestSpawn;
            public IEntitiesController entitiesController;
        }

        public BaseView View
            => _view;

        private readonly Ctx _ctx;
        private GameObject _pref;
        private AsteroidView _view;
        private float _rotateSide;
        private readonly IPoolManager _poolManager;
        private readonly IResourceLoader _resourceLoader;

        public AsteroidPm(Ctx ctx,
        [Inject] IPoolManager poolManager,
        [Inject] IResourceLoader resourceLoader)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _resourceLoader = resourceLoader;
            EntityMoverPm.Ctx entityMoverCtx = new EntityMoverPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                model = _ctx.asteroidModel,
                useAcceleration = false
            };
            AddDispose(EntityMoverPmFactory.CreateEntityMoverPm(entityMoverCtx));
            BorderControllerPm.Ctx borderCtx = new BorderControllerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                model = _ctx.asteroidModel,
                entitiesController = _ctx.entitiesController
            };
            AddDispose(new BorderControllerPm(borderCtx));
            
            _resourceLoader.LoadResource<GameObject>(ResourceIdsContainer.GameAsteroids.Asteroid, pref =>
            {
                _pref = pref;
                var spawnPlayer = _poolManager.Get(pref, _ctx.asteroidModel.Position.Value);
                _view = spawnPlayer.GetComponent<AsteroidView>();
                _view.SetCtx(new AsteroidView.Ctx
                {
                    model = _ctx.asteroidModel
                });
                
                _ctx.asteroidModel.OnDestroy += TryCollapse;
                _ctx.sceneContextView.OnUpdated += UpdateView;
            }, _ctx.cancellationToken);
        }
        
        private void TryCollapse(int? killerId)
        {
            if (killerId != null && _ctx.asteroidModel.CanCollapse.Value)
            {
                _ctx.requestSpawn?.Invoke(_ctx.asteroidModel.Position.Value);
            }
        }

        protected override void OnDispose()
        {
            
            _ctx.asteroidModel.OnDestroy -= TryCollapse;
            _ctx.sceneContextView.OnUpdated -= UpdateView;
            _poolManager.Return(_pref, _view.gameObject);
            base.OnDispose();
        }
        private void UpdateView(float deltaTime)
        {
            _view.transform.position = _ctx.asteroidModel.Position.Value;
            var curRotate = _view.Holder.rotation;
            curRotate = Quaternion.Euler(0, 0, curRotate.eulerAngles.z - _ctx.asteroidModel.MaxRotateSpeed.Value * deltaTime);
            _view.Holder.rotation = curRotate;
        }

    }
}