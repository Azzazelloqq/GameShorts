using UnityEngine;

namespace GameShorts.CubeRunner.Data
{
    public enum CubeRunnerTileType
    {
        Solid = 0,
        Gap = 1,
        Boundary = 2
    }

    [System.Serializable]
    public struct TileData
    {
        [SerializeField]
        private Vector2Int _gridPosition;

        [SerializeField]
        private CubeRunnerTileType _tileType;

        public TileData(Vector2Int gridPosition, CubeRunnerTileType tileType)
        {
            _gridPosition = gridPosition;
            _tileType = tileType;
        }

        public Vector2Int GridPosition => _gridPosition;
        public CubeRunnerTileType TileType => _tileType;

        public bool IsWalkable => _tileType == CubeRunnerTileType.Solid;
    }
}

