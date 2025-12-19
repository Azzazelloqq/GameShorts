using System;
using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.Enemy.Asteroid;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Asteroids.Code.Games.Game1.Scripts.View;
using Disposable;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Entities.Core;
using Logic.Scene;
using ResourceLoader;
using TickHandler;
using UnityEngine;

namespace Logic.Enemy.Asteroid
{
    internal class AsteroidPm : DisposableBase
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
        private readonly ITickHandler _tickHandler;

        public AsteroidPm(Ctx ctx,
            [Inject] IPoolManager poolManager,
            [Inject] IResourceLoader resourceLoader, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _resourceLoader = resourceLoader;
            _tickHandler = tickHandler;
            EntityMoverPm.Ctx entityMoverCtx = new EntityMoverPm.Ctx
            {
                model = _ctx.asteroidModel,
                useAcceleration = false
            };
            AddDisposable(EntityMoverPmFactory.CreateEntityMoverPm(entityMoverCtx));
            BorderControllerPm.Ctx borderCtx = new BorderControllerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                model = _ctx.asteroidModel,
                entitiesController = _ctx.entitiesController
            };
            AddDisposable(BorderControllerPmFactory.CreateBorderControllerPm(borderCtx));
            
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
                _tickHandler.FrameUpdate += UpdateView;
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
            _tickHandler.FrameUpdate -= UpdateView;
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