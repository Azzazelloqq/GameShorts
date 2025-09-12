using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Game1.Scripts.Entities.Core;
using LightDI.Runtime;
using Logic.Entities;
using Logic.Entities.Core;
using UnityEngine;

namespace Logic.Enemy.UFO
{
    public class UFOMoverPm : EntityMoverPm
{
        public struct UFOMoverCtx
        {
            public PlayerModel playerModel;
        }

        private readonly UFOMoverCtx _ufoMoverCtx;
        private PlayerModel _playerModel;

        public UFOMoverPm(UFOMoverCtx ctx, Ctx baseCtx , [Inject] IInputManager inputManager):base (baseCtx, inputManager)
        {
            _ufoMoverCtx = ctx;
            _playerModel = _ufoMoverCtx.playerModel;
        }

        protected override void UpdateDirectionAngle(float deltaTime)
        {
            Vector2 dir = _playerModel.Position.Value - _ctx.model.Position.Value;
            dir.Normalize();
            
            float angle = Mathf.Atan2( dir.y, dir.x) * Mathf.Rad2Deg;
            if (angle < 0)
                angle += 360;

            _requiredAngle = angle;// + _ctx.model.CurrentRotateSpeed.Value * deltaTime;
            var currentAngle = Mathf.LerpAngle(_ctx.model.CurrentAngle.Value, _requiredAngle, .5f);
            _ctx.model.CurrentAngle.Value = currentAngle % 360;
        }
}
}