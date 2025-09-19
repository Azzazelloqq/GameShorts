using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using Logic.Entities;
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
        private readonly IPoolManager _poolManager;

        public PlayerSpawnerPm(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _poolManager = poolManager;
            
            var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 0);
            var startPos = _ctx.sceneContextView.Camera != null && _ctx.sceneContextView.Camera 
                ? _ctx.sceneContextView.Camera.ScreenToWorldPoint(screenCenter) 
                : Vector3.zero;
            
            var playerSettings = _ctx.sceneContextView.PlayerSettings;
            var playerModel = new PlayerModel
            {
                Id = _ctx.entitiesController.GenerateId(),
                EntityType = EntityType.PlayerShip,
                Position = {Value = startPos},
                MaxSpeed = {Value = playerSettings.MaxSpeed},
                AccelerationSpeed = {Value = playerSettings.Acceleration},
                DecelerationSpeed = {Value = playerSettings.Deceleration},
                MaxRotateSpeed = {Value = playerSettings.MaxRotationSpeed},
                AccelerationRotateSpeed = {Value = playerSettings.RotationAcceleration},
                DecelerationRotateSpeed = {Value = playerSettings.RotationDeceleration},
            };
            PlayerPm.Ctx playerCtx = new PlayerPm.Ctx
            {
                playerModel = playerModel,
                sceneContextView = _ctx.sceneContextView,
                entitiesController = _ctx.entitiesController,
                Dead = _ctx.playerDead,
                cancellationToken = _ctx.cancellationToken
            };
            var player = PlayerPmFactory.CreatePlayerPm(playerCtx);
            
            _ctx.entitiesController.AddEntity(playerModel.Id, new EntityInfo
            {
                Logic = player,
                Model = playerModel,
            });
        }

    }
}