using System;
using UnityEngine;

namespace GameShorts.CubeRunner.Gameplay
{
    internal enum CubeOccupancyState
    {
        Walkable = 0,
        Gap = 1,
        Blocked = 2
    }

    internal enum CubeMoveResult
    {
        None = 0,
        Success = 1,
        Blocked = 2,
        Falling = 3
    }

    internal readonly struct CubeMovementEvent
    {
        public CubeMovementEvent(Vector2Int previousGrid, Vector2Int currentGrid, Vector3 worldPosition, CubeMoveResult result)
        {
            PreviousGrid = previousGrid;
            CurrentGrid = currentGrid;
            WorldPosition = worldPosition;
            Result = result;
        }

        public Vector2Int PreviousGrid { get; }
        public Vector2Int CurrentGrid { get; }
        public Vector3 WorldPosition { get; }
        public CubeMoveResult Result { get; }
        public bool IsFalling => Result == CubeMoveResult.Falling;
    }
}

