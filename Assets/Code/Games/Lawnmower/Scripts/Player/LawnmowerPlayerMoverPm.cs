using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.InputManager;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;
using LightDI.Runtime;
using TickHandler;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Player
{
    internal class LawnmowerPlayerMoverPm : BaseDisposable
    {
    internal struct Ctx
    {
        public LawnmowerPlayerModel playerModel;
        public bool useAcceleration;
        public LawnmowerLevelManager levelManager; // Для проверки границ
    }

        private readonly Ctx _ctx;
        private Vector2 _inputDirection;
        private readonly IInputManager _inputManager;
        private readonly ITickHandler _tickHandler;

        public LawnmowerPlayerMoverPm(Ctx ctx, 
            [Inject] IInputManager inputManager, 
            [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _inputManager = inputManager;
            _tickHandler = tickHandler;
            
            _tickHandler.PhysicUpdate += FixedUpdate;
            _tickHandler.FrameUpdate += Update;
        }
        
        private void Update(float deltaTime)
        {
            _inputDirection = GetInput();
        }

        private Vector2 GetInput()
        {
            // Получаем направление джойстика
            Vector2 joystickInput = _inputManager.GetJoystickInput();
            
            // Если джойстик не активен, не двигаемся
            if (joystickInput.magnitude < 0.01f)
            {
                _ctx.playerModel.IsMoving.Value = false;
                return Vector2.zero;
            }

            _ctx.playerModel.IsMoving.Value = true;
            
            // Обновляем направление движения в модели
            _ctx.playerModel.MovementDirection.Value = joystickInput.normalized;
            
            // Вычисляем целевой угол поворота
            if (joystickInput.magnitude > 0.1f) // Увеличил порог для стабильности
            {
                float targetAngle = Mathf.Atan2(joystickInput.y, joystickInput.x) * Mathf.Rad2Deg;
                
                // Нормализуем угол в диапазон [0, 360)
                if (targetAngle < 0)
                    targetAngle += 360f;
                    
                _ctx.playerModel.TargetRotation.Value = targetAngle;
            }
            
            // Возвращаем нормализованное направление и силу нажатия
            return joystickInput;
        }

        protected override void OnDispose()
        {
            _tickHandler.PhysicUpdate -= FixedUpdate;
            _tickHandler.FrameUpdate -= Update;
            base.OnDispose();
        }

        private void FixedUpdate(float deltaTime)
        {
            UpdateMoveSpeed(deltaTime);
            UpdateRotation(deltaTime);
            UpdatePosition(deltaTime);
        }

        private void UpdateMoveSpeed(float deltaTime)
        {
            var requiredSpeed = _inputDirection.magnitude * _ctx.playerModel.MaxSpeed.Value;
            
            if (_ctx.useAcceleration)
            {
                var currentSpeed = _ctx.playerModel.CurrentSpeed.Value;
                
                if (Mathf.Abs(requiredSpeed - currentSpeed) < Mathf.Epsilon)
                {
                    _ctx.playerModel.CurrentSpeed.Value = requiredSpeed;
                    return;
                }
                
                float acceleration = requiredSpeed > currentSpeed 
                    ? _ctx.playerModel.AccelerationSpeed.Value 
                    : -_ctx.playerModel.DecelerationSpeed.Value;
                    
                currentSpeed += acceleration * deltaTime;
                currentSpeed = Mathf.Clamp(currentSpeed, 0, _ctx.playerModel.MaxSpeed.Value);
                _ctx.playerModel.CurrentSpeed.Value = currentSpeed;
            }
            else
            {
                // Мгновенная реакция - более естественно для человека
                _ctx.playerModel.CurrentSpeed.Value = requiredSpeed;
            }
        }

        private void UpdateRotation(float deltaTime)
        {
            if (!_ctx.playerModel.IsMoving.Value) return;
            
            float currentRotation = _ctx.playerModel.CurrentRotation.Value;
            float targetRotation = _ctx.playerModel.TargetRotation.Value;
            
            if (_ctx.playerModel.InstantRotation.Value)
            {
                // Мгновенный поворот
                _ctx.playerModel.CurrentRotation.Value = targetRotation;
            }
            else
            {
                // Улучшенный плавный поворот
                float rotationSpeed = _ctx.playerModel.RotationSpeed.Value;
                
                // Вычисляем кратчайший путь поворота (учитываем переход через 360°)
                float angleDifference = Mathf.DeltaAngle(currentRotation, targetRotation);
                
                // Если разница углов мала, делаем мгновенный поворот для точности
                if (Mathf.Abs(angleDifference) < 2f)
                {
                    _ctx.playerModel.CurrentRotation.Value = targetRotation;
                }
                else
                {
                    // Плавный поворот с адаптивной скоростью
                    float maxRotationThisFrame = rotationSpeed * deltaTime;
                    
                    // Ускоряем поворот при больших углах, замедляем при приближении к цели
                    float speedMultiplier = Mathf.Clamp01(Mathf.Abs(angleDifference) / 90f); // Полная скорость при 90° и больше
                    maxRotationThisFrame *= Mathf.Lerp(0.3f, 1f, speedMultiplier); // Минимум 30% скорости
                    
                    float rotationStep = Mathf.Sign(angleDifference) * Mathf.Min(maxRotationThisFrame, Mathf.Abs(angleDifference));
                    
                    float newRotation = currentRotation + rotationStep;
                    
                    // Нормализуем угол в диапазон [0, 360)
                    if (newRotation < 0)
                        newRotation += 360f;
                    else if (newRotation >= 360f)
                        newRotation -= 360f;
                        
                    _ctx.playerModel.CurrentRotation.Value = newRotation;
                }
            }
        }

        private void UpdatePosition(float deltaTime)
        {
            if (_inputDirection.magnitude < 0.01f)
                return;

            // Для более естественного движения человека используем прямое направление джойстика
            // без нормализации, чтобы сохранить интенсивность нажатия
            var moveDistance = _ctx.playerModel.CurrentSpeed.Value * deltaTime;
            
            // Используем исходное направление джойстика (уже учитывает интенсивность)
            var deltaPosition = _inputDirection * moveDistance;
            
            // Вычисляем новую позицию
            Vector2 newPosition = _ctx.playerModel.Position.Value + deltaPosition;
            
            // Проверяем границы уровня
            newPosition = ClampToLevelBounds(newPosition);
            
            _ctx.playerModel.Position.Value = newPosition;
        }
        
        private Vector2 ClampToLevelBounds(Vector2 position)
        {
            if (_ctx.levelManager == null) return position;
            
            var currentLevel = _ctx.levelManager.GetCurrentLevel();
            if (currentLevel?.LevelBounds == null) return position;
            
            var bounds = currentLevel.LevelBounds.bounds;
            
            // Ограничиваем позицию границами уровня
            float clampedX = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
            float clampedY = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);
            
            return new Vector2(clampedX, clampedY);
        }
    }
}
