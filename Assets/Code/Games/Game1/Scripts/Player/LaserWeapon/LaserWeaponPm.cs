using System.Linq;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Settings;
using ResourceLoader;
using UnityEngine;

namespace Logic.Player.LaserWeapon
{
    internal class LaserWeaponPm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public PlayerModel playerModel;
            public PlayerView playerView;
            public MainSceneContextView sceneContextView;
            public LaserSettings laserSettings;
            public IEntitiesController entitiesController;
        }

        private readonly Ctx _ctx;
        private bool _inputFire;
        private Transform _spawnPosition;
        private int _indexator;
        private LaserSettings _laserSettings;
        private GameObject _laserPref;
        private readonly IPoolManager _poolManager;
        private readonly IResourceLoader _resourceLoader;
        private bool _isInited;

        public LaserWeaponPm(Ctx ctx,
            [Inject] IPoolManager poolManager,
            [Inject] IResourceLoader resourceLoader)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _resourceLoader = resourceLoader;
            _spawnPosition = _ctx.playerView.ShootPoint;
            _laserSettings = _ctx.laserSettings;
            _ctx.playerModel.InitLaserBattary(_laserSettings.CountLaserShots, _laserSettings.LaserCooldown);
            LoadPref();
            _ctx.sceneContextView.OnUpdated += OnUpdated;
        }

        private void OnUpdated(float deltaTime)
        {
            foreach (var battery in _ctx.playerModel.Charges)
            {
                battery.UpdateMe(deltaTime);
            }

            Fire();
        }

        private void Fire()
        {
            if (!_isInited)
                return;
            
            LaserBattery readyBattary = _ctx.playerModel.Charges.FirstOrDefault(battary => battary.IsReady);
            if (readyBattary == null)
                return;
            readyBattary.LastShot.Value = Time.time;
            CreateLaser();
        }

        private void CreateLaser()
        {
            var position = _spawnPosition.position;
            var model = new LaserModel()
            {
                EntityType = EntityType.Laser,
                Id = _ctx.entitiesController.GenerateId(),
                Duration = { Value = _laserSettings.LaserShotDuration },
                Length = { Value = _laserSettings.LaserLength },
                RotationSpeed = {Value = _laserSettings.LaserRotationSpeed}
            };

            var view = _poolManager.Get(_laserPref, _spawnPosition);
            var laserView = view.GetComponent<LaserView>();
            laserView.SetCtx(new BaseView.Ctx
            {
                model = model
            });

            LaserPm.Ctx laserCtx = new LaserPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                view = laserView,
                laserModel = model,
                returnView = () => { _poolManager.Return(_laserPref, view); },
                entitiesController = _ctx.entitiesController,
                playerModel = _ctx.playerModel
            };

            var laser = new LaserPm(laserCtx);
            AddDispose(laser);
            _ctx.entitiesController.AddEntity(model.Id, new EntityInfo
            {
                Logic = laser,
                Model = model
            });
        }

        private void LoadPref()
        {
            _resourceLoader.LoadResource<GameObject>(ResourceIdsContainer.GameAsteroids.Laser,
                pref =>
                {
                    _isInited = true;
                    _laserPref = pref;
                }, _ctx.cancellationToken);
        }


        protected override void OnDispose()
        {
            foreach (var entitiesControllerAllEntity in _ctx.entitiesController.AllEntities)
            {
                entitiesControllerAllEntity.Value.Logic?.Dispose();
            }
            _ctx.sceneContextView.OnUpdated -= OnUpdated;
            base.OnDispose();
        }
    }
}