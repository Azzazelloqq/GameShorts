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
using Logic.Entities.Core;
using Logic.Scene;
using Logic.Settings;
using ResourceLoader;
using Root.Inputs;
using UnityEngine;

namespace Logic.Player.ProjectileWeapon
{
    public class ProjectileWeaponPm : BaseDisposable
    {
        public struct Ctx
        {
            public CancellationToken cancellationToken;
            public PlayerModel playerModel;
            public PlayerView playerView;
            public PlayerController PlayerController;
            public MainSceneContextView sceneContextView;
            public ProjectileSettings projectileSettings;
            public IEntitiesController entitiesController;
        }

        private readonly Ctx _ctx;
        private bool _inputFire;
        private Transform _spawnPosition;
        private int _indexator;
        private ProjectileSettings _projectileSettings;
        private GameObject _projectilePref;
        private readonly IPoolManager _poolManager;
        private readonly IResourceLoader _resourceLoader;

        public ProjectileWeaponPm(Ctx ctx, 
            [Inject] IPoolManager poolManager,
            [Inject] IResourceLoader resourceLoader)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _resourceLoader = resourceLoader;
            _spawnPosition = _ctx.playerView.ShootPoint;
            _projectileSettings = _ctx.projectileSettings;
            LoadPref();
            _ctx.PlayerController.Fire1 += Fire;
            _ctx.sceneContextView.OnFixedUpdated += FixedUpdate;
        }
        
        private void Fire()
        {
            CreateProjectile();
        }
        private void CreateProjectile()
        { 
            var position = _spawnPosition.position;
            var model = new ProjectileModel
            {
                EntityType = EntityType.Projectile,
                Id = _ctx.entitiesController.GenerateId(),
                Position = {Value = position},
                CurrentAngle = {Value = _ctx.playerModel.CurrentAngle.Value},
                MaxSpeed = {Value = _projectileSettings.ProjectileMaxSpeed},
            };

            var view = _poolManager.Get(_projectilePref, position, _ctx.playerModel.CurrentAngle.Value);
            var projectileView = view.GetComponent<ProjectileView>();
            projectileView.SetCtx(new BaseView.Ctx
            {
                model = model
            });
           
            ProjectilePm.Ctx projectileCtx = new ProjectilePm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                view = projectileView,
                ownewId = _ctx.playerModel.Id,
                projectileModel = model,
                returnView = () => { _poolManager.Return(_projectilePref, view); },
                entitiesController = _ctx.entitiesController
            };

            var projectile = new ProjectilePm(projectileCtx);
            _ctx.entitiesController.AddEntity(model.Id, new EntityInfo
            {
                Logic = projectile,
                Model = model
            } );
        }
        
        private void LoadPref()
        {
            _resourceLoader.LoadResource<GameObject>(ResourceIdsContainer.GameAsteroids.Projectile, pref =>
            {
                _projectilePref = pref;
            }, _ctx.cancellationToken);
        }

        private void FixedUpdate(float deltaTime)
        {
            
        }

        protected override void OnDispose()
        {
            _ctx.PlayerController.Fire1 -= Fire;
            _ctx.sceneContextView.OnFixedUpdated -= FixedUpdate;
            base.OnDispose();
        }
        
    }
}