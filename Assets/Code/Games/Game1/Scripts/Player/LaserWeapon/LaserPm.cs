
using System;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Logic.Entities;
using UnityEngine;

namespace Logic.Player.LaserWeapon
{
    public class LaserPm : BaseDisposable
    {
        public struct Ctx
        {
            public LaserView view;
            public LaserModel laserModel;
            public PlayerModel playerModel;
            public SceneContextView sceneContextView;
            public Action returnView;
            public IEntitiesController entitiesController;
        }

        private readonly Ctx _ctx;
        private LaserView _view;
        private LaserModel _laserModel;
        private PlayerModel _playerModel;
        private float _timer;
        private ContactFilter2D _contactFilter;
        private RaycastHit2D[] _hits;

        public LaserPm(Ctx ctx)
        {
            _ctx = ctx;
            _view = _ctx.view;
            _laserModel = _ctx.laserModel;
            _playerModel = _ctx.playerModel;
            _view.Laser.SetPosition(1, Vector3.right * _laserModel.Length.Value);
            _view.Laser.gameObject.SetActive(true);
            _timer = _laserModel.Duration.Value;
            _contactFilter = new ContactFilter2D();
            _hits = new RaycastHit2D[10];
            _ctx.sceneContextView.OnFixedUpdated += OnFixedUpdated;
            _ctx.sceneContextView.OnUpdated += OnOnUpdated;
        }
        private void OnOnUpdated(float deltaTime)
        {
            _timer -= deltaTime;
            if (_timer <= 0)
                DestroyMe(null);
        }
        private void OnFixedUpdated(float deltaTime)
        {
            float angleRadians = _ctx.playerModel.CurrentAngle.Value * Mathf.Deg2Rad;
            Vector2 directionVector = new Vector2(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians));

            var collisions = Physics2D.Raycast(_view.transform.position, directionVector, _contactFilter.NoFilter(), _hits, _ctx.laserModel.Length.Value);
          //  Debug.DrawLine(_view.transform.position, directionVector * _ctx.laserModel.Length.Value, Color.green,1f);
            Debug.DrawRay(_view.transform.position, directionVector , Color.green,1f);
            
            
            for (int i = 0; i < collisions; i++)
            {
                IEntityView entityView = _hits[i].transform != null ? _hits[i].transform.GetComponent<IEntityView>() : null;

                if (entityView?.Model.EntityType != EntityType.PlayerShip)
                    _ctx.entitiesController.TryDestroyEntity(entityView.Model.Id, _playerModel.Id);
            }
        }
        private void DestroyMe(int? killerId)
        {
            _ctx.entitiesController.TryDestroyEntity(_ctx.laserModel.Id);
        }

        protected override void OnDispose()
        {
            _view.Laser.gameObject.SetActive(false);
            _ctx.sceneContextView.OnUpdated -= OnOnUpdated;
            _ctx.sceneContextView.OnFixedUpdated -= OnFixedUpdated;
            _ctx.returnView?.Invoke();
            base.OnDispose();
        }
    }
}