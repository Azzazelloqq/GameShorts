using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools.Pool;
using Cysharp.Threading.Tasks;
using GameShorts.CubeRunner.Data;
using GameShorts.CubeRunner.Gameplay;
using GameShorts.CubeRunner.View;
using LightDI.Runtime;
using UnityEngine;

namespace GameShorts.CubeRunner.Level
{
    internal enum Direction
    {
        Up, // ⭡
        Down, // ⭣
        Left, // ⭠
        Right // ⭢
    }

    internal class LevelManager : BaseDisposable
    {
        internal struct Ctx
        {
            public CubeRunnerGameSettings gameSettings;
            public Transform tilesRoot;
        }

        private readonly Ctx _ctx;
        private readonly CubeRunnerGameSettings _settings;
        private readonly List<TileView> _levelTiles;
        private readonly List<TileView> _exitCells = new List<TileView>();

        private int _currentDifficultyIndex = -1;
        private int _consecutiveGapRows;
        private int _consecutiveSolidRows;

        private readonly System.Random _random = new System.Random();
        private readonly IPoolManager _poolManager;

        private int[,] _levelGrid;
        private List<Direction> _winPath;
        private int _currentLevelNumber;
        private Vector2Int _startCellPosition;
        private Vector2Int _targetFootprintMin;
        private Vector2Int _targetFootprintMax;
        private Vector2Int _targetFootprintMinRaw;
        private Vector2Int _targetFootprintMaxRaw;
        private readonly List<BorderView> _levelBorderTiles;

        private const float AxisAlignmentThreshold = 0.985f;
        private const float GridSnapEpsilon = 0.0001f;

        public int[,] LevelGrid => _levelGrid;
        public List<Direction> WinPath => _winPath;
        public Vector2Int StartCellPosition => _startCellPosition;

        public Action PlayerBorderDetected;

        public LevelManager(Ctx ctx, [Inject] IPoolManager poolManager)
        {
            _ctx = ctx;
            _settings = _ctx.gameSettings;
            _poolManager = poolManager;
            _levelTiles = new List<TileView>();
            _levelBorderTiles = new List<BorderView>();
        }

        protected override void OnDispose()
        {
            ClearLevel();
            base.OnDispose();
        }

        public void GenerateLevel(Vector3 currentCubeDimensions)
        {
            _currentLevelNumber++;
            int pathLength = CalculatePathLength(_currentLevelNumber);

            _winPath = GenerateWinPath(pathLength);
            _levelGrid = BuildLevelGrid(_winPath, currentCubeDimensions);
            InstantiateLevel();
        }

        private void InstantiateLevel()
        {
            ClearLevel();

            for (int z = 0; z < _levelGrid.GetLength(0); z++)
            {
                for (int x = 0; x < _levelGrid.GetLength(1); x++)
                {
                    var spawnPos = new Vector3(
                        x - _startCellPosition.x,
                        0f,
                        z - _startCellPosition.y);
                    
                    if (_levelGrid[z, x] == -1)
                    {
                        var tileBorderObject = _poolManager.Get(_ctx.gameSettings.BorderPrefab, spawnPos, _ctx.tilesRoot,
                            Quaternion.identity);
                        var borderView = tileBorderObject.GetComponent<BorderView>();
                        borderView.PlayerDetected += PlayerDetected;
                        _levelBorderTiles.Add(borderView);
                        continue;
                    }
                    var cellValue = _levelGrid[z, x];

                    var tileObject = _poolManager.Get(_ctx.gameSettings.TilePrefab, spawnPos, _ctx.tilesRoot,
                        Quaternion.identity);
                    var tileView = tileObject.GetComponent<TileView>();
                    tileView.IsExitTile = cellValue == 1;

                    if (cellValue == 1)
                        _exitCells.Add(tileView);
                    

                    _levelTiles.Add(tileView);
                }
            }
        }

        private void PlayerDetected()
        {
            PlayerBorderDetected?.Invoke();
        }

        private void ClearLevel()
        {
            foreach (var borderView in _levelBorderTiles)
            {
                borderView.PlayerDetected -=  PlayerDetected;
                _poolManager.Return(_ctx.gameSettings.BorderPrefab, borderView.gameObject);
            }
            
            foreach (var tile in _levelTiles)
            {
                tile.ResetView();
                _poolManager.Return(_ctx.gameSettings.TilePrefab, tile.gameObject);
            }

            _levelTiles.Clear();
            _levelBorderTiles.Clear();
            _exitCells.Clear();
        }

        private int CalculatePathLength(int levelNumber)
        {
            int baseLength = 4;
            int additionalSteps = (levelNumber - 1) / 2;
            return baseLength + additionalSteps;
        }

        private List<Direction> GenerateWinPath(int pathLength)
        {
            List<Direction> path = new List<Direction>();

            if (pathLength <= 0)
                return path;

            Direction[] allDirections = { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            Direction lastDirection = allDirections[_random.Next(allDirections.Length)];
            path.Add(lastDirection);

            int currentSequenceLength = 1;
            int maxSequenceLength = _random.Next(2, 6);
            bool canReverse = false;

            for (int i = 1; i < pathLength; i++)
            {
                Direction nextDirection;

                if (currentSequenceLength < maxSequenceLength)
                {
                    nextDirection = lastDirection;
                    currentSequenceLength++;
                }
                else
                {
                    Direction[] availableDirections;

                    if (canReverse)
                    {
                        availableDirections = allDirections;
                    }
                    else
                    {
                        availableDirections = GetPerpendicularDirections(lastDirection);
                    }

                    nextDirection = availableDirections[_random.Next(availableDirections.Length)];
                    currentSequenceLength = 1;
                    maxSequenceLength = _random.Next(2, 6);
                    canReverse = true;
                }

                if (IsOppositeDirection(nextDirection, lastDirection))
                {
                    canReverse = false;
                }

                path.Add(nextDirection);
                lastDirection = nextDirection;
            }

            return path;
        }

        private bool IsOppositeDirection(Direction dir1, Direction dir2)
        {
            return (dir1 == Direction.Up && dir2 == Direction.Down) ||
                   (dir1 == Direction.Down && dir2 == Direction.Up) ||
                   (dir1 == Direction.Left && dir2 == Direction.Right) ||
                   (dir1 == Direction.Right && dir2 == Direction.Left);
        }

        private Direction[] GetPerpendicularDirections(Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                case Direction.Down:
                    return new[] { Direction.Left, Direction.Right };
                case Direction.Left:
                case Direction.Right:
                    return new[] { Direction.Up, Direction.Down };
                default:
                    return new[] { Direction.Up, Direction.Down, Direction.Left, Direction.Right };
            }
        }

        private struct FootprintState
        {
            public Vector2Int Anchor;
            public int SizeX;
            public int SizeZ;
        }

        private void AddFootprintCells(FootprintState footprint, HashSet<Vector2Int> coveredCells)
        {
            for (int dx = 0; dx < footprint.SizeX; dx++)
            {
                for (int dz = 0; dz < footprint.SizeZ; dz++)
                {
                    coveredCells.Add(new Vector2Int(footprint.Anchor.x + dx, footprint.Anchor.y + dz));
                }
            }
        }

        private void SimulateRoll(Direction direction, ref int anchorX, ref int anchorZ, ref int axisX, ref int axisY,
            ref int axisZ)
        {
            switch (direction)
            {
                case Direction.Up:
                    RollAlongAxis(ref anchorZ, ref axisZ, ref axisY, true);
                    break;
                case Direction.Down:
                    RollAlongAxis(ref anchorZ, ref axisZ, ref axisY, false);
                    break;
                case Direction.Right:
                    RollAlongAxis(ref anchorX, ref axisX, ref axisY, true);
                    break;
                case Direction.Left:
                    RollAlongAxis(ref anchorX, ref axisX, ref axisY, false);
                    break;
            }
        }

        private void RollAlongAxis(ref int anchorCoord, ref int axisAlong, ref int axisVertical,
            bool isPositiveDirection)
        {
            int horizontalBefore = axisAlong;
            int verticalBefore = axisVertical;

            if (isPositiveDirection)
            {
                anchorCoord += horizontalBefore;
            }
            else
            {
                anchorCoord -= verticalBefore;
            }

            axisAlong = verticalBefore;
            axisVertical = horizontalBefore;
        }

        private int[,] BuildLevelGrid(List<Direction> path, Vector3 cubeDimensions)
        {
            int roundedX = Mathf.Max(1, Mathf.RoundToInt(cubeDimensions.x));
            int roundedY = Mathf.Max(1, Mathf.RoundToInt(cubeDimensions.y));
            int roundedZ = Mathf.Max(1, Mathf.RoundToInt(cubeDimensions.z));

            int anchorX = 0;
            int anchorZ = 0;
            int axisX = roundedX;
            int axisY = roundedY;
            int axisZ = roundedZ;

            List<FootprintState> footprints = new List<FootprintState>();
            footprints.Add(new FootprintState
            {
                Anchor = new Vector2Int(anchorX, anchorZ),
                SizeX = axisX,
                SizeZ = axisZ
            });

            HashSet<Vector2Int> coveredCells = new HashSet<Vector2Int>();
            AddFootprintCells(footprints[0], coveredCells);

            foreach (Direction dir in path)
            {
                SimulateRoll(dir, ref anchorX, ref anchorZ, ref axisX, ref axisY, ref axisZ);
                var state = new FootprintState
                {
                    Anchor = new Vector2Int(anchorX, anchorZ),
                    SizeX = axisX,
                    SizeZ = axisZ
                };
                footprints.Add(state);
                AddFootprintCells(state, coveredCells);
            }

            int minX = int.MaxValue, maxX = int.MinValue;
            int minZ = int.MaxValue, maxZ = int.MinValue;

            foreach (Vector2Int cell in coveredCells)
            {
                minX = Mathf.Min(minX, cell.x);
                maxX = Mathf.Max(maxX, cell.x);
                minZ = Mathf.Min(minZ, cell.y);
                maxZ = Mathf.Max(maxZ, cell.y);
            }

            int width = maxX - minX + 1;
            int height = maxZ - minZ + 1;
            int offsetX = -minX;
            int offsetZ = -minZ;

            int[,] grid = new int[height, width];
            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    grid[z, x] = -1;
                }
            }

            Direction lastDirection = path.Count > 0 ? path[path.Count - 1] : Direction.Up;

            for (int i = 0; i < footprints.Count; i++)
            {
                bool isLast = i == footprints.Count - 1;
                var footprint = footprints[i];

                for (int dx = 0; dx < footprint.SizeX; dx++)
                {
                    for (int dz = 0; dz < footprint.SizeZ; dz++)
                    {
                        Vector2Int cell = new Vector2Int(footprint.Anchor.x + dx, footprint.Anchor.y + dz);
                        int gridX = cell.x + offsetX;
                        int gridZ = cell.y + offsetZ;

                        if (gridX < 0 || gridX >= width || gridZ < 0 || gridZ >= height)
                            continue;

                        if (isLast)
                        {
                            bool isExitCell = IsExitCell(dx, dz, lastDirection, footprint.SizeX, footprint.SizeZ);
                            grid[gridZ, gridX] = isExitCell ? 1 : 0;
                        }
                        else if (grid[gridZ, gridX] != 1)
                        {
                            grid[gridZ, gridX] = 0;
                        }
                    }
                }
            }

            var lastFootprint = footprints[footprints.Count - 1];
            _targetFootprintMinRaw = new Vector2Int(lastFootprint.Anchor.x + offsetX, lastFootprint.Anchor.y + offsetZ);
            _targetFootprintMaxRaw = new Vector2Int(
                _targetFootprintMinRaw.x + lastFootprint.SizeX - 1,
                _targetFootprintMinRaw.y + lastFootprint.SizeZ - 1);

            grid = AddBorderAroundPath(grid);
            _targetFootprintMin = _targetFootprintMinRaw + Vector2Int.one;
            _targetFootprintMax = _targetFootprintMaxRaw + Vector2Int.one;
          
            Vector2Int startAnchor = footprints[0].Anchor;
            _startCellPosition = new Vector2Int(startAnchor.x + offsetX + 2, startAnchor.y + offsetZ + 2);
            return grid;
        }

        private int[,] AddBorderAroundPath(int[,] grid)
        {
            int height = grid.GetLength(0);
            int width = grid.GetLength(1);
            int[,] newGrid = new int[height + 4, width + 4];

            for (int z = 0; z < height + 4; z++)
            {
                for (int x = 0; x < width + 4; x++)
                {
                    newGrid[z, x] = -1;
                }
            }

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    newGrid[z + 2, x + 2] = grid[z, x];
                }
            }

            for (int z = 0; z < height; z++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (grid[z, x] >= 0)
                    {
                        Vector2Int[] neighbors = new Vector2Int[]
                        {
                            new Vector2Int(0, -1),
                            new Vector2Int(0, 1),
                            new Vector2Int(-1, 0),
                            new Vector2Int(1, 0)
                        };

                        foreach (Vector2Int offset in neighbors)
                        {
                            int nz = z + 2 + offset.y;
                            int nx = x + 2 + offset.x;
                            if (nz >= 0 && nz < height + 4 && nx >= 0 && nx < width + 4)
                            {
                                if (newGrid[nz, nx] == -1)
                                {
                                    newGrid[nz, nx] = 0;
                                }
                            }
                        }
                    }
                }
            }

            return newGrid;
        }

        private bool IsExitCell(int dx, int dz, Direction lastDirection, int cubeX, int cubeZ)
        {
            // Финиш должен повторять всю последнюю опорную площадь куба,
            // чтобы приземление габаритных кубов (2x2, 2x3 и т.п.) фиксировалось полностью.
            return dx >= 0 && dx < cubeX && dz >= 0 && dz < cubeZ;
        }

        public Vector2Int GetStartPosition()
        {
            if (_levelGrid == null)
                return Vector2Int.zero;

            for (int z = 0; z < _levelGrid.GetLength(0); z++)
            {
                for (int x = 0; x < _levelGrid.GetLength(1); x++)
                {
                    if (_levelGrid[z, x] == 0)
                    {
                        return new Vector2Int(x, z);
                    }
                }
            }

            return Vector2Int.zero;
        }

        public Vector3 GridToWorldPosition(Vector2Int gridPos)
        {
            if (_settings == null)
                return Vector3.zero;

            float tileSize = _settings.TileSize;
            Vector2Int relativeGridPos = gridPos - _startCellPosition;
            return new Vector3(relativeGridPos.x * tileSize, 0f, relativeGridPos.y * tileSize);
        }

        public bool IsWin(CubeView cubeView)
        {
            if (_levelGrid == null || cubeView == null || _exitCells.Count == 0)
                return false;

            var isWin = false;
            foreach (var cell in _exitCells)
            {
                isWin = cell.IsPlayerEnter;
                if (!isWin)
                    break;
            }
            return isWin;
        }
    }
}