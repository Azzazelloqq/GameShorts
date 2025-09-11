using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using Logic.Entities;
using Root.Inputs;

namespace Logic.Player
{
    public class PlayerSpawnerPm : BaseDisposable
    {
        public struct Ctx
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
            var playerController = AddDispose(
                PlayerControllerFactory.CreatePlayerController(new PlayerController.Ctx()));
            
            var playerSettings = _ctx.sceneContextView.PlayerSettings;
            var playerModel = new PlayerModel
            {
                Id = _ctx.entitiesController.GenerateId(),
                EntityType = EntityType.PlayerShip,
                Position = {Value = playerSettings.StartPosition},
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
                PlayerController = playerController,
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