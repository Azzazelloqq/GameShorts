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
using Logic.Player.LaserWeapon;
using Logic.Player.ProjectileWeapon;
using ResourceLoader;
using Root.Inputs;
using UnityEngine;

namespace Logic.Player
{
    public class PlayerPm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public MainSceneContextView sceneContextView;
            public PlayerModel playerModel;
            public PlayerController PlayerController;
            public Action Dead;
            public IEntitiesController entitiesController;
        }

        public PlayerView View => _view;
        private readonly Ctx _ctx;
        private GameObject _pref;
        private PlayerView _view;
        private readonly IResourceLoader _resourceLoader;
        private readonly IPoolManager _poolManager;

        public PlayerPm(Ctx ctx,
            [Inject] IResourceLoader resourceLoader, 
            [Inject] IPoolManager poolManager )
        {
            _ctx = ctx;
            _resourceLoader = resourceLoader;
            _poolManager = poolManager;
            
            _resourceLoader.LoadResource<GameObject>(ResourceIdsContainer.GameAsteroids.Player,pref =>
            {
                _pref = pref;
                var spawnPlayer = _poolManager.Get(pref);
                _view = spawnPlayer.GetComponent<PlayerView>();
                _view.SetCtx(new BaseView.Ctx
                {
                    model = _ctx.playerModel,
                });
                OnViewLoaded();
            }, _ctx.cancellationToken);
        }
        private void OnViewLoaded()
        {
            InitLogic();
            _view.Collided += Collided;
            _ctx.sceneContextView.OnUpdated += UpdateView;
        }
        private void InitLogic()
        {
            EntityMoverPm.Ctx entityMoverCtx = new EntityMoverPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                model = _ctx.playerModel,
                useAcceleration = true,
                isPlayer = true
            };
            AddDispose(EntityMoverPmFactory.CreateEntityMoverPm(entityMoverCtx));
            
            ScreenWraperPm.Ctx screenWraperCtx = new ScreenWraperPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                playerModel = _ctx.playerModel
            };
            AddDispose(new ScreenWraperPm(screenWraperCtx));

            ProjectileWeaponPm.Ctx projectileWeaponCtx = new ProjectileWeaponPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                playerModel = _ctx.playerModel,
                PlayerController = _ctx.PlayerController,
                playerView = _view,
                projectileSettings = _ctx.sceneContextView.ProjectileSettings,
                entitiesController = _ctx.entitiesController,
                cancellationToken = _ctx.cancellationToken
            };
            AddDispose(ProjectileWeaponPmFactory.CreateProjectileWeaponPm(projectileWeaponCtx));

            LaserWeaponPm.Ctx laserWeaponCtx = new LaserWeaponPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                playerModel = _ctx.playerModel,
                PlayerController = _ctx.PlayerController,
                playerView = _view,
                entitiesController = _ctx.entitiesController,
                laserSettings = _ctx.sceneContextView.laserSettings,
                cancellationToken = _ctx.cancellationToken
            };
            AddDispose(LaserWeaponPmFactory.CreateLaserWeaponPm(laserWeaponCtx));
        }

        private void Collided(CollidedInfo collidedInfo)
        {
            if (!_ctx.entitiesController.TryGetEntityInfo(collidedInfo.defenderId, out var entityInfo))
             return;
            
            if (entityInfo.Model.EntityType == EntityType.Projectile)
            {
                if (((ProjectileModel)entityInfo.Model).OwnerId == _ctx.playerModel.Id)
                    return;
            }
            
            _ctx.Dead?.Invoke();
        }

        protected override void OnDispose()
        {
            _view.Collided -= Collided;
            _ctx.sceneContextView.OnUpdated -= UpdateView;
            _poolManager.Return(_pref, _view.gameObject);
            base.OnDispose();
        }
        private void UpdateView(float deltaTime)
        {
            _view.transform.position = _ctx.playerModel.Position.Value;
            _view.transform.rotation = Quaternion.Euler(0, 0, _ctx.playerModel.CurrentAngle.Value);
        }
    }
}