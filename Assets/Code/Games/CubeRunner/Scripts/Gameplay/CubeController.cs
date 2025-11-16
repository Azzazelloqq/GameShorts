using System;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.CubeRunner.View;
using R3;
using UnityEngine;

namespace GameShorts.CubeRunner.Gameplay
{
    internal class CubeController : BaseDisposable
    {
        internal struct Ctx
        {
            public CubeView cubeView;
            public Vector3 originLocalPosition;
            public Vector2Int startGridPosition;
            public float tileSize;
            public Func<Vector2Int, CubeOccupancyState> resolveOccupancy;
        }

        private readonly Ctx _ctx;
        private Vector2Int _currentGridPosition;
        private readonly Subject<CubeMovementEvent> _movementStream = new Subject<CubeMovementEvent>();

        public Subject<CubeMovementEvent> Movements => _movementStream;
        public CubeView View => _ctx.cubeView;

        public Vector2Int CurrentGridPosition => _currentGridPosition;

        public Vector3 CurrentLocalPosition => GridToLocal(_currentGridPosition);

        public CubeController(Ctx ctx)
        {
            _ctx = ctx;
            _currentGridPosition = _ctx.startGridPosition;

            if (_ctx.cubeView != null)
            {
                _ctx.cubeView.LocalPosition = GridToLocal(_currentGridPosition);
            }
        }

        public CubeMoveResult TryMove(Vector2Int direction)
        {
            if (direction == Vector2Int.zero)
            {
                return CubeMoveResult.None;
            }

            Vector2Int targetGrid = _currentGridPosition + direction;
            CubeOccupancyState occupancy = _ctx.resolveOccupancy != null
                ? _ctx.resolveOccupancy.Invoke(targetGrid)
                : CubeOccupancyState.Walkable;

            if (occupancy == CubeOccupancyState.Blocked)
            {
                return CubeMoveResult.Blocked;
            }

            Vector2Int previousGrid = _currentGridPosition;
            _currentGridPosition = targetGrid;

            Vector3 localPosition = GridToLocal(_currentGridPosition);
            if (_ctx.cubeView != null)
            {
                _ctx.cubeView.LocalPosition = localPosition;
            }

            CubeMoveResult result = occupancy == CubeOccupancyState.Gap
                ? CubeMoveResult.Falling
                : CubeMoveResult.Success;

            _movementStream.OnNext(new CubeMovementEvent(previousGrid, _currentGridPosition, _ctx.cubeView != null ? _ctx.cubeView.WorldPosition : Vector3.zero, result));
            return result;
        }

        public void ResetToStart(Vector2Int startGridPosition)
        {
            _currentGridPosition = startGridPosition;
            if (_ctx.cubeView != null)
            {
                _ctx.cubeView.LocalPosition = GridToLocal(_currentGridPosition);
            }
        }

        public Vector3 GridToLocal(Vector2Int gridPosition)
        {
            float x = _ctx.originLocalPosition.x + gridPosition.x * _ctx.tileSize;
            float z = _ctx.originLocalPosition.z + gridPosition.y * _ctx.tileSize;
            return new Vector3(x, _ctx.originLocalPosition.y, z);
        }

        protected override void OnDispose()
        {
            _movementStream.OnCompleted();
            _movementStream.Dispose();
        }
    }
}

