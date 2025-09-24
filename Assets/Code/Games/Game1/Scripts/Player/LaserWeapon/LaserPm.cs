
using System;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Code.Core.BaseDMDisposable.Scripts;
using LightDI.Runtime;
using Logic.Entities;
using TickHandler;
using UnityEngine;

namespace Logic.Player.LaserWeapon
{
    internal class LaserPm : BaseDisposable
    {
        internal struct Ctx
        {
            public LaserView view;
            public LaserModel laserModel;
            public PlayerModel playerModel;
            public Action returnView;
            public IEntitiesController entitiesController;
        }

        private readonly Ctx _ctx;
        private LaserView _view;
        private LaserModel _laserModel;
        private PlayerModel _playerModel;
        private float _timer;
        private RaycastHit2D[] _hits;
        private float _currentRotationAngle;
        private readonly ITickHandler _tickHandler;

        public LaserPm(Ctx ctx, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            _view = _ctx.view;
            _laserModel = _ctx.laserModel;
            _playerModel = _ctx.playerModel;
            // Setup 3-point laser: start -> center -> end
            _view.Laser.positionCount = 3;
            float halfLength = _laserModel.Length.Value / 2f;
            _view.Laser.SetPosition(0, Vector3.left * halfLength);  // Start point
            _view.Laser.SetPosition(1, Vector3.zero);               // Center point (spawn position)
            _view.Laser.SetPosition(2, Vector3.right * halfLength); // End point
            _view.Laser.gameObject.SetActive(true);
            _timer = _laserModel.Duration.Value;
            _hits = new RaycastHit2D[10];
            _currentRotationAngle = _ctx.playerModel.CurrentAngle.Value; // Start from player's direction
            _tickHandler.PhysicUpdate += (OnFixedUpdated);
            _tickHandler.FrameUpdate += (OnOnUpdated);
        }
        private void OnOnUpdated(float deltaTime)
        {
            if (isDisposed || _ctx.entitiesController == null)
                return;
                
            _timer -= deltaTime;
            if (_timer <= 0)
                DestroyMe(null);
        }
        private void OnFixedUpdated(float deltaTime)
        {
            // Exit early if disposed or entities controller is null
            if (isDisposed || _ctx.entitiesController == null)
                return;
                
            // Update rotation angle (clockwise rotation in world space)
            _currentRotationAngle += _laserModel.RotationSpeed.Value * deltaTime;
            
            // Convert angle to radians and create direction vectors
            float angleRadians = _currentRotationAngle * Mathf.Deg2Rad;
            Vector2 directionVector = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));
            Vector2 oppositeDirection = -directionVector;
            
            // Update laser visual direction - bidirectional from center
            float halfLength = _laserModel.Length.Value / 2f;
            Vector3 startPosition = new Vector3(
                -halfLength * Mathf.Cos(angleRadians),
                -halfLength * Mathf.Sin(angleRadians),
                0
            );
            Vector3 endPosition = new Vector3(
                halfLength * Mathf.Cos(angleRadians),
                halfLength * Mathf.Sin(angleRadians),
                0
            );
            
            _view.Laser.SetPosition(0, startPosition);  // Start point
            _view.Laser.SetPosition(1, Vector3.zero);   // Center point (spawn position)
            _view.Laser.SetPosition(2, endPosition);    // End point

            // Perform raycast for collision detection in both directions
            var collisions1 = Physics2D.Raycast(_view.transform.position, directionVector, ContactFilter2D.noFilter, _hits, halfLength);
            
            // Process collisions in positive direction
            for (int i = 0; i < collisions1; i++)
            {
                IEntityView entityView = _hits[i].transform != null ? _hits[i].transform.GetComponent<IEntityView>() : null;

                if (entityView?.Model.EntityType != EntityType.PlayerShip)
                {
                    // Проверяем, что EntitiesController еще существует
                    if (_ctx.entitiesController != null)
                        _ctx.entitiesController.TryDestroyEntity(entityView.Model.Id, _playerModel.Id);
                }
            }
            
            // Raycast in opposite direction
            var collisions2 = Physics2D.Raycast(_view.transform.position, oppositeDirection, ContactFilter2D.noFilter, _hits, halfLength);
            
            // Process collisions in negative direction
            for (int i = 0; i < collisions2; i++)
            {
                IEntityView entityView = _hits[i].transform != null ? _hits[i].transform.GetComponent<IEntityView>() : null;

                if (entityView?.Model.EntityType != EntityType.PlayerShip)
                {
                    // Проверяем, что EntitiesController еще существует
                    if (_ctx.entitiesController != null)
                        _ctx.entitiesController.TryDestroyEntity(entityView.Model.Id, _playerModel.Id);
                }
            }
        }
        private void DestroyMe(int? killerId)
        {
            if (!isDisposed && _ctx.entitiesController != null)
                _ctx.entitiesController.TryDestroyEntity(_ctx.laserModel.Id);
        }

        protected override void OnDispose()
        {
            _tickHandler.PhysicUpdate -= (OnFixedUpdated);
            _tickHandler.FrameUpdate -= (OnOnUpdated);
            _view.Laser.gameObject.SetActive(false);
            _ctx.returnView?.Invoke();
            base.OnDispose();
        }
    }
}