using Asteroids.Code.Games.Game1.Scripts.Entities.Core;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using LightDI.Runtime;
using Logic.Entities.Core;
using TickHandler;
using UnityEngine;

namespace Logic.Entities
{
    internal class EntityMoverPm : BaseDisposable
    {
        internal struct Ctx
        {
            public BaseModel model;
            public bool useAcceleration;
            public bool isPlayer ;
        }

        protected readonly Ctx _ctx;
        protected float _requiredAngle;
        protected Vector2 _inputDirection;
        private readonly IInputManager _inputManager;
        private readonly ITickHandler _tickHandler;

        public EntityMoverPm(Ctx ctx, 
            [Inject] IInputManager inputManager, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            _tickHandler = tickHandler;
            _requiredAngle = _ctx.model.CurrentAngle.Value;
            _tickHandler.PhysicUpdate += (FixedUpdate);
            _tickHandler.FrameUpdate += (Update);
        }
        private void Update(float deltaTime)
        {
            _inputDirection = GetInput();
        }

        protected virtual Vector2 GetInput()
        {
            if (!_ctx.isPlayer)
                return Vector2.up;

            // Получаем направление джойстика (абсолютное направление в мировых координатах)
            Vector2 joystickDirection = _inputManager.GetJoystickInput();
            
            // Если джойстик не активен, не двигаемся
            if (joystickDirection.magnitude < 0.01f)
                return Vector2.zero;

            // Устанавливаем целевой угол напрямую из направления джойстика
            float targetAngle = Mathf.Atan2(joystickDirection.y, joystickDirection.x) * Mathf.Rad2Deg;
            _requiredAngle = targetAngle;
            
            // Для совместимости со старой системой возвращаем (0, magnitude)
            // x = 0 так как мы напрямую управляем углом через _requiredAngle
            // y = magnitude для контроля скорости движения
            return new Vector2(0f, joystickDirection.magnitude);
        }

        protected override void OnDispose()
        {
            _tickHandler.PhysicUpdate -= (FixedUpdate);
            _tickHandler.FrameUpdate -= (Update);
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
            // Плавно поворачиваем к целевому углу
            var currentAngle = Mathf.LerpAngle(_ctx.model.CurrentAngle.Value, _requiredAngle, 0.1f);
            _ctx.model.CurrentAngle.Value = currentAngle;
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