using GameShorts.CubeRunner.View;
using UnityEngine;

namespace GameShorts.CubeRunner.Data
{
    [CreateAssetMenu(fileName = "CubeRunnerGameSettings", menuName = "GameShorts/CubeRunner/GameSettings")]
    public class CubeRunnerGameSettings : ScriptableObject
    {
        [Header("World")]
        [SerializeField]
        private float _worldSpeed = 4f;

        [SerializeField]
        private float _tileSize = 1f;

        [SerializeField]
        private float _minZBoundary = -1f;

        [SerializeField]
        private float _maxZBoundary = 10f;

        [SerializeField]
        private int _laneCount = 3;

        [Header("Generation")]
        [SerializeField]
        private int _tilesAheadToGenerate = 24;

        [SerializeField]
        private int _initialSafeTiles = 6;

        [SerializeField]
        private int _sectionLength = 6;

        [SerializeField]
        private DifficultyConfig[] _difficultySteps;

        [Header("Prefabs")]
        [SerializeField]
        private CubeView _cubePrefab;

        [SerializeField]
        private TileView _tilePrefab;

        public float WorldSpeed => _worldSpeed;
        public float TileSize => _tileSize;
        public float MinZBoundary => _minZBoundary;
        public float MaxZBoundary => _maxZBoundary;
        public int LaneCount => Mathf.Max(1, _laneCount);
        public int TilesAheadToGenerate => Mathf.Max(1, _tilesAheadToGenerate);
        public int InitialSafeTiles => Mathf.Max(0, _initialSafeTiles);
        public int SectionLength => Mathf.Max(1, _sectionLength);
        public DifficultyConfig[] DifficultySteps => _difficultySteps;
        public CubeView CubePrefab => _cubePrefab;
        public TileView TilePrefab => _tilePrefab;
    }
}

