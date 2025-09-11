using System.Linq;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Settings;
using ResourceLoader;
using Root.Inputs;
using UnityEngine;

namespace Logic.Player.LaserWeapon
{
    public class LasetWeaponPm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public PlayerModel playerModel;
            public PlayerView playerView;
            public PlayerController PlayerController;
            public MainSceneContextView sceneContextView;
            public LaserSettings laserSettings;
            public IEntitiesController entitiesController;
        }

        private const string LASER_PREF = "Laser";
        private readonly Ctx _ctx;
        private bool _inputFire;
        private Transform _spawnPosition;
        private int _indexator;
        private LaserSettings _laserSettings;
        private GameObject _laserPref;
        private readonly IPoolManager _poolManager;
        private readonly IResourceLoader _resourceLoader;

        public LasetWeaponPm(Ctx ctx, 
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
            _ctx.PlayerController.Fire2 += Fire;
            _ctx.sceneContextView.OnFixedUpdated += FixedUpdate;
            _ctx.sceneContextView.OnUpdated += OnUpdated;
        }
        private void OnUpdated(float deltaTime)
        {
            foreach (var battery in _ctx.playerModel.Charges)
            {
                battery.UpdateMe(deltaTime);
            }
        }

        private void Fire()
        {
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
                Duration = {Value = _laserSettings.LaserShotDuration},
                Length = {Value = _laserSettings.LaserLength}
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
            _ctx.entitiesController.AddEntity(model.Id, new EntityInfo
            {
                Logic = laser,
                Model = model
            } );
        }
        
        private void LoadPref()
        {
            _resourceLoader.LoadResource<GameObject>(LASER_PREF, pref =>
            {
                _laserPref = pref;
            }, _ctx.cancellationToken);
        }

        private void FixedUpdate(float deltaTime)
        {
            
        }

        protected override void OnDispose()
        {
            _ctx.sceneContextView.OnUpdated -= OnUpdated;
            _ctx.PlayerController.Fire1 -= Fire;
            _ctx.sceneContextView.OnFixedUpdated -= FixedUpdate;
            base.OnDispose();
        }
        
    }
}