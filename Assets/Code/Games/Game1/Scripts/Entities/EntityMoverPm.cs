using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.Tools.Pool;
using LightDI.Runtime;
using Logic.Entities.Core;
using Root.Inputs;
using UnityEngine;

namespace Logic.Entities
{
    public class EntityMoverPm : BaseDisposable
    {
        public struct Ctx
        {
            public BaseModel model;
            public SceneContextView sceneContextView;
            public bool useAcceleration;
            public bool isPlayer ;
        }

        protected readonly Ctx _ctx;
        protected float _requiredAngle;
        protected Vector2 _inputDirection;
        private readonly IInputManager _inputManager;

        public EntityMoverPm(Ctx ctx, [Inject] IInputManager inputManager)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            _requiredAngle = _ctx.model.CurrentAngle.Value;
            _ctx.sceneContextView.OnFixedUpdated += FixedUpdate;
            _ctx.sceneContextView.OnUpdated += Update;
        }
        private void Update(float deltaTime)
        {
            _inputDirection = GetInput();
        }

        protected virtual Vector2 GetInput()
        {
           return _ctx.isPlayer 
               ? _inputManager.GetJoystickInput() 
               : Vector2.up;
        }

        protected override void OnDispose()
        {
            _ctx.sceneContextView.OnUpdated -= Update;
            _ctx.sceneContextView.OnFixedUpdated -= FixedUpdate;
            base.OnDispose();
        }

        private void FixedUpdate(float deltaTime)
        {
            UpdateMoveSpeed();
            UpdateRotateSpeed();
            UpdateDirectionAngle(deltaTime);
            UpdatePosition(deltaTime);

            void UpdateRotateSpeed()
            {
                var currentRotateSpeed = _ctx.model.CurrentRotateSpeed.Value;
                var requiredRotateSpeed = _inputDirection.x * _ctx.model.MaxRotateSpeed.Value;
                
                if (_ctx.useAcceleration)
                {
                    if (Mathf.Abs(requiredRotateSpeed - currentRotateSpeed) < Mathf.Epsilon)
                    {
                        _ctx.model.CurrentRotateSpeed.Value = requiredRotateSpeed;
                        return;
                    }
                    float acceleration = requiredRotateSpeed > currentRotateSpeed ? _ctx.model.AccelerationRotateSpeed.Value : -1 * _ctx.model.DecelerationRotateSpeed.Value;
                    currentRotateSpeed += acceleration * deltaTime;
                    currentRotateSpeed = Mathf.Clamp(currentRotateSpeed, -_ctx.model.MaxRotateSpeed.Value, _ctx.model.MaxRotateSpeed.Value);
                }
                else
                {
                    currentRotateSpeed = requiredRotateSpeed;
                }
                _ctx.model.CurrentRotateSpeed.Value = currentRotateSpeed;
            }

            void UpdateMoveSpeed()
            {
                var currentSpeed = _ctx.model.CurrentSpeed.Value;
                var requiredSpeed = _inputDirection.y * _ctx.model.MaxSpeed.Value;
                if (_ctx.useAcceleration)
                {
                    if (Mathf.Abs(requiredSpeed - currentSpeed) < Mathf.Epsilon)
                    {
                        _ctx.model.CurrentSpeed.Value = requiredSpeed;
                        return;
                    }
                    float acceleration = requiredSpeed > currentSpeed ? _ctx.model.AccelerationSpeed.Value : -1 * _ctx.model.DecelerationSpeed.Value;
                    currentSpeed += acceleration * deltaTime;
                    currentSpeed = Mathf.Clamp(currentSpeed, 0, _ctx.model.MaxSpeed.Value);
                }
                else
                {
                    currentSpeed = requiredSpeed;
                }
                _ctx.model.CurrentSpeed.Value = currentSpeed;
            }

        }
        protected virtual void UpdateDirectionAngle(float deltaTime)
        {
            _requiredAngle -= _ctx.model.CurrentRotateSpeed.Value * deltaTime;
            var currentAngle = Mathf.LerpAngle(_ctx.model.CurrentAngle.Value, _requiredAngle, .5f);
            _ctx.model.CurrentAngle.Value = currentAngle % 360;
        }

        private void UpdatePosition(float deltaTime)
        {
            var delta = _ctx.model.CurrentSpeed.Value * deltaTime;
            var radAngle = Mathf.Deg2Rad * _ctx.model.CurrentAngle.Value;
            var dir = new Vector2( Mathf.Cos(radAngle), Mathf.Sin(radAngle));
            _ctx.model.Position.Value += new Vector2(delta * dir.x, delta * dir.y);
        }
    }
}