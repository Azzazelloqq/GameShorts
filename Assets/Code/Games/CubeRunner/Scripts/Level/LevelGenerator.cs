using System;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using GameShorts.CubeRunner.Data;
using GameShorts.CubeRunner.Gameplay;
using GameShorts.CubeRunner.View;
using UnityEngine;

namespace GameShorts.CubeRunner.Level
{
    internal class LevelGenerator : BaseDisposable
    {
        internal struct Ctx
        {
            public CubeRunnerGameSettings gameSettings;
            public Transform tilesRoot;
            public IPoolManager poolManager;
            public Vector3 originLocalPosition;
        }

        private readonly Ctx _ctx;
        private readonly TileManager _tileManager;
        private readonly CubeRunnerGameSettings _settings;

        private int _lastGeneratedRow;
        private int _currentDifficultyIndex = -1;
        private DifficultyConfig _activeDifficulty;
        private int _consecutiveGapRows;
        private int _consecutiveSolidRows;

        private readonly System.Random _random = new System.Random();

        public Vector3 OriginLocalPosition => _ctx.originLocalPosition;
        public event Action<DifficultyConfig> DifficultyLevelChanged;

        public LevelGenerator(Ctx ctx)
        {
            _ctx = ctx;
            _settings = _ctx.gameSettings;

            _tileManager = TileManagerFactory.CreateTileManager(new TileManager.Ctx
            {
                tilesRoot = _ctx.tilesRoot,
                gameSettings = _ctx.gameSettings
            });
            AddDispose(_tileManager);
        }

        public void Initialize()
        {
            _tileManager.RemoveTilesBeforeRow(int.MaxValue);
            _lastGeneratedRow = -1;
            _currentDifficultyIndex = -1;
            _activeDifficulty = null;
            _consecutiveGapRows = 0;
            _consecutiveSolidRows = 0;

            int safeRows = Mathf.Max(_settings.InitialSafeTiles, 1);
            for (int row = 0; row < safeRows; row++)
            {
                GenerateFullRow(row);
            }

            _lastGeneratedRow = safeRows - 1;
        }

        public void EnsureTilesAhead(Vector2Int cubeGridPosition)
        {
            int targetRow = cubeGridPosition.y + _settings.TilesAheadToGenerate;
            while (_lastGeneratedRow < targetRow)
            {
                _lastGeneratedRow++;
                GenerateRow(_lastGeneratedRow);
            }
        }

        public void CullTilesBehind(int cubeRow, int keepRowsBehind = 5)
        {
            int minRow = cubeRow - Mathf.Max(1, keepRowsBehind);
            if (minRow <= 0)
            {
                return;
            }

            _tileManager.RemoveTilesBeforeRow(minRow);
        }

        public CubeOccupancyState ResolveOccupancy(Vector2Int gridPosition)
        {
            if (gridPosition.y < 0)
            {
                return CubeOccupancyState.Walkable;
            }

            if (_tileManager.HasTile(gridPosition))
            {
                return CubeOccupancyState.Walkable;
            }

            return CubeOccupancyState.Gap;
        }

        public bool HasTile(Vector2Int gridPosition)
        {
            return _tileManager.HasTile(gridPosition);
        }

        public Vector3 GridToLocal(Vector2Int gridPosition)
        {
            float tileSize = _settings.TileSize;
            int laneOffset = gridPosition.x;
            float x = _ctx.originLocalPosition.x + laneOffset * tileSize;
            float z = _ctx.originLocalPosition.z + gridPosition.y * tileSize;
            return new Vector3(x, _ctx.originLocalPosition.y, z);
        }

        private void GenerateRow(int rowIndex)
        {
            if (rowIndex < _settings.InitialSafeTiles)
            {
                GenerateFullRow(rowIndex);
                return;
            }

            UpdateDifficulty(rowIndex);
            if (_activeDifficulty == null)
            {
                GenerateFullRow(rowIndex);
                return;
            }

            int minSolid = Mathf.Max(0, _activeDifficulty.MinSolidStreak);
            int maxGap = _activeDifficulty.MaxGapStreak <= 0 ? int.MaxValue : _activeDifficulty.MaxGapStreak;

            if (_consecutiveSolidRows < minSolid)
            {
                GenerateFullRow(rowIndex);
                return;
            }

            if (_consecutiveGapRows >= maxGap)
            {
                GenerateFullRow(rowIndex);
                return;
            }

            bool[] rowTiles = new bool[_settings.LaneCount];

            int walkableCount = 0;
            for (int lane = 0; lane < _settings.LaneCount; lane++)
            {
                bool createTile = _random.NextDouble() > _activeDifficulty.GapProbability;
                if (createTile)
                {
                    walkableCount++;
                    rowTiles[lane] = true;
                }
            }

            if (walkableCount == 0)
            {
                int centerLane = _settings.LaneCount / 2;
                rowTiles[centerLane] = true;
                walkableCount = 1;
            }

            SpawnRowTiles(rowIndex, rowTiles);

            if (walkableCount == _settings.LaneCount)
            {
                RegisterSolidRow();
            }
            else
            {
                RegisterGapRow();
            }
        }

        private void GenerateFullRow(int rowIndex)
        {
            bool[] rowTiles = new bool[_settings.LaneCount];
            for (int lane = 0; lane < _settings.LaneCount; lane++)
            {
                rowTiles[lane] = true;
            }

            SpawnRowTiles(rowIndex, rowTiles);
            RegisterSolidRow();
        }

        private void SpawnRowTiles(int rowIndex, bool[] rowTiles)
        {
            int minLane = -(_settings.LaneCount / 2);
            for (int lane = 0; lane < _settings.LaneCount; lane++)
            {
                if (!rowTiles[lane])
                {
                    continue;
                }

                int laneIndex = minLane + lane;
                var gridPosition = new Vector2Int(laneIndex, rowIndex);
                Vector3 localPosition = GridToLocal(gridPosition);
                _tileManager.SpawnTile(gridPosition, localPosition);
            }
        }

        private void UpdateDifficulty(int distance)
        {
            DifficultyConfig[] steps = _settings.DifficultySteps;
            if (steps == null || steps.Length == 0)
            {
                _activeDifficulty = null;
                _currentDifficultyIndex = -1;
                return;
            }

            int newIndex = 0;
            for (int i = 0; i < steps.Length; i++)
            {
                if (distance >= steps[i].DistanceThreshold)
                {
                    newIndex = i;
                }
                else
                {
                    break;
                }
            }

            if (newIndex != _currentDifficultyIndex || _activeDifficulty == null)
            {
                _currentDifficultyIndex = newIndex;
                _activeDifficulty = steps[newIndex];
                DifficultyLevelChanged?.Invoke(_activeDifficulty);
            }
        }

        private void RegisterSolidRow()
        {
            _consecutiveSolidRows++;
            _consecutiveGapRows = 0;
        }

        private void RegisterGapRow()
        {
            _consecutiveGapRows++;
            _consecutiveSolidRows = 0;
        }

        protected override void OnDispose()
        {
            base.OnDispose();
        }
    }
}

