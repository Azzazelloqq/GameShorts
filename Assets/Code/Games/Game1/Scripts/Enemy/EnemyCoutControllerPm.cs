using System;
using Asteroids.Code.Games.Game1.Scripts.Enemy;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Asteroids.Code.Games.Game1.Scripts.View;
using Disposable;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Settings;
using TickHandler;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Logic.Enemy
{
    internal class EnemyCoutControllerPm : DisposableBase
    {
        public event Action<EnemySpawnInfo> SpawnEnemy;
        internal struct Ctx
        {
            public MainSceneContextView sceneContextView;
            public IEntitiesController entitiesController;
        }

        private readonly Ctx _ctx;
        private float _lastTimeSpawn;
        private GameSettings _gameSettings;
        private Camera _camera;
        private PlayerModel _playerModel;
        private readonly ITickHandler _tickHandler;

        public EnemyCoutControllerPm(Ctx ctx, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            _gameSettings = _ctx.sceneContextView.GameSettings;
            _camera = _ctx.sceneContextView.Camera;
            _playerModel = _ctx.entitiesController.GetPlayerModel();
            _tickHandler.FrameUpdate += (Tick);
        }

        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= (Tick);
            base.OnDispose();
        }

        private void Tick(float deltaTime)
        {
            // Проверяем, что камера еще существует
            if (_camera == null || !_camera)
            {
                return;
            }
            
            if (_ctx.entitiesController.AllEntities.Count >= _gameSettings.MaxCountEnemies)
            {
                _lastTimeSpawn = Time.time;
                return;
            }
                
            if (_lastTimeSpawn > Time.time - _gameSettings.SpawnEnemyCooldown)
                return;

            _lastTimeSpawn = Time.time;

            var randomIndexAnimal = Random.Range(0, 2);
            var entity = randomIndexAnimal == 0 ? EntityType.Asteroid : EntityType.UFO;
            var startInfo = GetPositionAndDirOutsideScreen(entity == EntityType.UFO);
            SpawnEnemy?.Invoke(new EnemySpawnInfo
            {
                SpawnPosition = startInfo.pos,
                Angle = startInfo.angle,
                entityType = entity
            });
        }
        
        (Vector2 pos, float angle) GetPositionAndDirOutsideScreen(bool dirToPlayer)
        {
            // Проверяем, что камера еще существует
            if (_camera == null || !_camera)
            {
                return (Vector2.zero, 0f);
            }
            
            int sideScreen =  Random.Range(0, 4);
            var angle = Random.Range(-25, 25f);
            float randomPos = Random.Range(0.1f, .9f);
            float randomX = 0f;
            float randomY = 0f;
            
            switch (sideScreen)
            {
                case 0: // Up
                    randomX = randomPos;
                    randomY = 1.04f;
                    angle += 270;
                    break;
                case 1: // Down
                    randomX = randomPos;
                    randomY = -0.04f;
                    angle += 90;
                    break;
                case 2: // Left
                    randomX = -0.04f; 
                    randomY = randomPos;
                    break;
                case 3: // Right
                    randomX = 1.04f; 
                    randomY = randomPos;
                    angle += 180;
                    break;
            }
            
            Vector3 randomPosition = _camera.ViewportToWorldPoint(new Vector3(randomX, randomY, 0));
            if (dirToPlayer)
            {
                var dir = _playerModel.Position.Value - (Vector2)randomPosition;
                dir.Normalize();
                angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                if (angle < 0)
                    angle += 360;
            }
            
            return (randomPosition , angle);
        }
    }
}