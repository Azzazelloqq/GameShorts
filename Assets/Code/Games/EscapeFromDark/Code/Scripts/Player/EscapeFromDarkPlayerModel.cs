using System;
using Code.Core.BaseDMDisposable.Scripts;
using R3;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Player
{
    internal class EscapeFromDarkPlayerModel : BaseDisposable
    {
        internal struct Ctx
        {
            public float moveSpeed;
            public Vector3 startPosition;
        }

        private readonly ReactiveProperty<Vector3> _position = new();
        private readonly ReactiveProperty<bool> _isMoving = new();
        private readonly ReactiveProperty<Vector2> _movementDirection = new();
        private readonly ReactiveProperty<float> _currentRotation = new();

        public ReadOnlyReactiveProperty<Vector3> Position => _position;
        public ReadOnlyReactiveProperty<bool> IsMoving => _isMoving;
        public ReadOnlyReactiveProperty<Vector2> MovementDirection => _movementDirection;
        public ReadOnlyReactiveProperty<float> CurrentRotation => _currentRotation;

        private float _moveSpeed;
        
        public float MoveSpeed => _moveSpeed;

        public EscapeFromDarkPlayerModel(Ctx ctx)
        {
            _moveSpeed = ctx.moveSpeed > 0 ? ctx.moveSpeed : 5f;
            
            _position.Value = ctx.startPosition;
            _isMoving.Value = false;
            _movementDirection.Value = Vector2.zero;
            _currentRotation.Value = 0f;

            AddDispose(_position);
            AddDispose(_isMoving);
            AddDispose(_movementDirection);
            AddDispose(_currentRotation);

            Debug.Log($"EscapeFromDarkPlayerModel: Initialized with speed={_moveSpeed}");
        }

        public void SetPosition(Vector3 newPosition)
        {
            _position.Value = newPosition;
        }

        public void SetMoving(bool isMoving)
        {
            _isMoving.Value = isMoving;
        }

        public void SetMovementDirection(Vector2 direction)
        {
            _movementDirection.Value = direction;
            
            // Вычисляем угол поворота на основе направления движения
            if (direction.magnitude > 0.1f)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                _currentRotation.Value = angle;
            }
        }

        public void ResetToPosition(Vector3 newPosition)
        {
            _position.Value = newPosition;
            _isMoving.Value = false;
            _movementDirection.Value = Vector2.zero;
            _currentRotation.Value = 0f;
            
            Debug.Log($"EscapeFromDarkPlayerModel: Player reset to position {newPosition}");
        }
    }
}
