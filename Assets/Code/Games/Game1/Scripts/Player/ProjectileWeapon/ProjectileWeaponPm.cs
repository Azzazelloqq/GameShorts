using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities;
using Code.Core.Tools.Pool;
using Code.Generated.Addressables;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Settings;
using ResourceLoader;
using TickHandler;
using UnityEngine;

namespace Logic.Player.ProjectileWeapon
{
    internal class ProjectileWeaponPm : BaseDisposable
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public PlayerModel playerModel;
            public PlayerView playerView;
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
        private float _startFire;
        private readonly ITickHandler _tickHandler;

        public ProjectileWeaponPm(Ctx ctx, 
            [Inject] IPoolManager poolManager,
            [Inject] IResourceLoader resourceLoader, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            _resourceLoader = resourceLoader;
            _tickHandler = tickHandler;
            _spawnPosition = _ctx.playerView.ShootPoint;
            _projectileSettings = _ctx.projectileSettings;
            LoadPref();
        }
        
        private void Fire(float deltaTime)
        {
            // Проверяем, что объекты еще существуют (не уничтожены при свайпе)
            if (_spawnPosition == null || _ctx.playerView == null)
                return;
                
            if (Time.time - _startFire < _ctx.projectileSettings.ProjectileRate)
                return;
            
            _startFire = Time.time;
            CreateProjectile();
        }
        private void CreateProjectile()
        { 
            // Проверяем, что EntitiesController еще существует
            if (_ctx.entitiesController == null)
                return;
            
            // Дополнительная проверка на случай, если объект был уничтожен между вызовами
            if (_spawnPosition == null)
                return;
                
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

            var projectile = ProjectilePmFactory.CreateProjectilePm(projectileCtx);
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
                _tickHandler.FrameUpdate += Fire;
            }, _ctx.cancellationToken);
        }

        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= Fire;
            base.OnDispose();
        }
    }
}