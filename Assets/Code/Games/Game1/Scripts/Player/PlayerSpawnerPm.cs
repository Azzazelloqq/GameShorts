using System;
using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.Entities;
using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using Logic.Entities;
using R3;
using UnityEngine;

namespace Logic.Player
{
    internal class PlayerSpawnerPm : BaseDisposable
    {
        internal struct Ctx
        {
            public MainSceneContextView sceneContextView;
            public IEntitiesController entitiesController;
            public Action playerDead;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private PlayerModel _playerModel;
        private float _baseMaxSpeed;
        private float _baseAcceleration;
        private float _baseDeceleration;

        public PlayerSpawnerPm(Ctx ctx)
        {
            _ctx = ctx;
            var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            var startPos = _ctx.sceneContextView.Camera != null && _ctx.sceneContextView.Camera 
                ? _ctx.sceneContextView.Camera.ScreenToWorldPoint(screenCenter) 
                : Vector3.zero;
            
            var playerSettings = _ctx.sceneContextView.PlayerSettings;
            
            // Сохраняем базовые значения скоростей
            _baseMaxSpeed = playerSettings.MaxSpeed;
            _baseAcceleration = playerSettings.Acceleration;
            _baseDeceleration = playerSettings.Deceleration;
            
            _playerModel = new PlayerModel
            {
                Id = _ctx.entitiesController.GenerateId(),
                EntityType = EntityType.PlayerShip,
                Position = {Value = startPos},
                MaxSpeed = {Value = _baseMaxSpeed},
                AccelerationSpeed = {Value = _baseAcceleration},
                DecelerationSpeed = {Value = _baseDeceleration},
                MaxRotateSpeed = {Value = playerSettings.MaxRotationSpeed},
                AccelerationRotateSpeed = {Value = playerSettings.RotationAcceleration},
                DecelerationRotateSpeed = {Value = playerSettings.RotationDeceleration},
            };

            // Подписываемся на изменения множителя скорости игрока
            AddDispose(_playerModel.DifficultyScaler.PlayerSpeedMultiplier.Subscribe(OnPlayerSpeedMultiplierChanged));
            PlayerPm.Ctx playerCtx = new PlayerPm.Ctx
            {
                playerModel = _playerModel,
                sceneContextView = _ctx.sceneContextView,
                entitiesController = _ctx.entitiesController,
                Dead = _ctx.playerDead,
                cancellationToken = _ctx.cancellationToken
            };
            var player = PlayerPmFactory.CreatePlayerPm(playerCtx);
            
            _ctx.entitiesController.AddEntity(_playerModel.Id, new EntityInfo
            {
                Logic = player,
                Model = _playerModel,
            });
        }

        private void OnPlayerSpeedMultiplierChanged(float multiplier)
        {
            if (_playerModel == null) return;
            
            // Применяем множитель к базовым значениям скоростей
            _playerModel.MaxSpeed.Value = _baseMaxSpeed * multiplier;
            _playerModel.AccelerationSpeed.Value = _baseAcceleration * multiplier;
            _playerModel.DecelerationSpeed.Value = _baseDeceleration * multiplier;
        }

    }
}