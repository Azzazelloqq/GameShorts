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

        public ReadOnlyReactiveProperty<Vector3> Position => _position;
        public ReadOnlyReactiveProperty<bool> IsMoving => _isMoving;

        private float _moveSpeed;
        
        public float MoveSpeed => _moveSpeed;

        public EscapeFromDarkPlayerModel(Ctx ctx)
        {
            _moveSpeed = ctx.moveSpeed > 0 ? ctx.moveSpeed : 5f;
            
            _position.Value = ctx.startPosition;
            _isMoving.Value = false;

            AddDispose(_position);
            AddDispose(_isMoving);

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

        public void ResetToPosition(Vector3 newPosition)
        {
            _position.Value = newPosition;
            _isMoving.Value = false;
            
            Debug.Log($"EscapeFromDarkPlayerModel: Player reset to position {newPosition}");
        }
    }
}
