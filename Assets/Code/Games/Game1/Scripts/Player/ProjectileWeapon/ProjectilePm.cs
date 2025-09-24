using System;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Scene;
using TickHandler;

namespace Logic.Player.ProjectileWeapon
{
    internal class ProjectilePm : BaseDisposable
    {
        internal struct Ctx
        {
            public int ownewId;
            public ProjectileView view;
            public ProjectileModel projectileModel;
            public SceneContextView sceneContextView;
            public Action returnView;
            public IEntitiesController entitiesController;
        }

        private readonly Ctx _ctx;
        private ProjectileView _view;
        private ProjectileModel _projectileModel;
        private readonly ITickHandler _tickHandler;

        public ProjectilePm(Ctx ctx, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            _view = _ctx.view;
            _projectileModel = _ctx.projectileModel;
            
            EntityMoverPm.Ctx entityMoverCtx = new EntityMoverPm.Ctx
            {
                model = _ctx.projectileModel,
                useAcceleration = false
            };
            AddDispose(EntityMoverPmFactory.CreateEntityMoverPm(entityMoverCtx));
            
            BorderControllerPm.Ctx borderCtx = new BorderControllerPm.Ctx
            {
                sceneContextView = _ctx.sceneContextView,
                model = _ctx.projectileModel,
                entitiesController = _ctx.entitiesController
            };
            AddDispose( BorderControllerPmFactory.CreateBorderControllerPm(borderCtx));
            
            //_ctx.projectileModel.OnDestroy += DestroyMe;
            _view.Collided += OnCollided;
            _tickHandler.FrameUpdate += (UpdateMe);
        }
        private void DestroyMe(int? killerId)
        {
            // Проверяем, что EntitiesController еще существует
            if (_ctx.entitiesController != null)
                _ctx.entitiesController.TryDestroyEntity(_ctx.projectileModel.Id);
        }
        private void UpdateMe(float obj)
        {
            _view.transform.position = _projectileModel.Position.Value;
        }
        private void OnCollided(CollidedInfo collidedInfo)
        {
            // Проверяем, что EntitiesController еще существует
            if (_ctx.entitiesController == null)
                return;
                
            if (!_ctx.entitiesController.TryGetEntityInfo(collidedInfo.defenderId, out var entityInfo))
                return;
            
            if (entityInfo.Model.EntityType is EntityType.Asteroid or EntityType.AsteroidPart or EntityType.UFO)
            {
                _ctx.entitiesController.TryDestroyEntity(entityInfo.Model.Id, _ctx.projectileModel.OwnerId);
            }
            
            if (entityInfo.Model.EntityType is EntityType.PlayerShip)
                return;
            DestroyMe(null);
        }
        
        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= (UpdateMe);
            _view.Collided -= OnCollided;
            _ctx.returnView?.Invoke();
            base.OnDispose();
        }
    }
}