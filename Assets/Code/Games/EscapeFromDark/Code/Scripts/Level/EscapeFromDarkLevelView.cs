using Disposable;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level
{
    internal class EscapeFromDarkLevelView : MonoBehaviourDisposable
    {
        [Header("Tilemap Settings")]
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileBase _brushTiles;
        [SerializeField] private Tilemap _wallsTilemap;
        [SerializeField] private TileBase _floorTile;
        [SerializeField] private Vector2Int origin = Vector2Int.zero;
        
        [FormerlySerializedAs("_exitSpot")]
        [Header("Exit Spot")]
        [SerializeField] private GameObject _exitSpotPrefab;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        private int[,] _currentMaze;
        private int _mazeWidth;
        private int _mazeHeight;
        private ExitSpotController _spawnedExitSpot;

        public ExitSpotController SpawnedExitSpot => _spawnedExitSpot;

        internal struct Ctx
        {
            public int[,] mazeData;
            public int mazeWidth;
            public int mazeHeight;
            public Transform playerTransform; // Ссылка на игрока для ExitSpot
        }

        public void SetCtx(Ctx ctx)
        {
            _currentMaze = ctx.mazeData;
            _mazeWidth = ctx.mazeWidth;
            _mazeHeight = ctx.mazeHeight;
            
            if (_tilemap == null)
            {
                Debug.LogError("EscapeFromDarkLevelView: Tilemap is not assigned!");
                return;
            }
            
            if (_brushTiles == null)
            {
                Debug.LogError("EscapeFromDarkLevelView: BrushTiles is not assigned!");
                return;
            }
            
            if (_wallsTilemap == null)
            {
                Debug.LogError("EscapeFromDarkLevelView: WallsTilemap is not assigned!");
                return;
            }
            
            if (_floorTile == null)
            {
                Debug.LogError("EscapeFromDarkLevelView: FloorTile is not assigned!");
                return;
            }
            
            if (_exitSpotPrefab == null)
            {
                Debug.LogWarning("EscapeFromDarkLevelView: ExitSpot prefab is not assigned!");
            }
            
            RenderMaze();
            SpawnExitSpot(ctx.playerTransform);
            
            if (showDebugInfo)
            {
                Debug.Log($"EscapeFromDarkLevelView: Rendered maze {_mazeWidth}x{_mazeHeight} at origin {origin}");
            }
        }

        private void RenderMaze()
        {
            // Очищаем предыдущие тайлы
            _tilemap.SetTilesBlock(_tilemap.cellBounds, new TileBase[_tilemap.cellBounds.size.x * _tilemap.cellBounds.size.y]);
            _wallsTilemap.SetTilesBlock(_wallsTilemap.cellBounds, new TileBase[_wallsTilemap.cellBounds.size.x * _wallsTilemap.cellBounds.size.y]);
            
            // Рисуем новый лабиринт
            for (int y = 0; y < _mazeHeight; y++)
            {
                for (int x = 0; x < _mazeWidth; x++)
                {
                    // Конвертируем координаты: y инвертируем, так как массив идет сверху вниз, а tilemap снизу вверх
                    Vector3Int tilePosition = new Vector3Int(
                        origin.x + x, 
                        origin.y + (_mazeHeight - 1 - y), 
                        0
                    );
                    
                    if (_currentMaze[y, x] == 0)
                    {
                        // Рисуем тайлы там, где можно ходить (проходы = 0)
                        _tilemap.SetTile(tilePosition, _brushTiles);
                    }
                    else
                    {
                        // Рисуем стены там, где нельзя ходить (стены = 1)
                        _wallsTilemap.SetTile(tilePosition, _floorTile);
                    }
                }
            }
            
            // Принудительно обновляем оба tilemap
            _tilemap.RefreshAllTiles();
            _wallsTilemap.RefreshAllTiles();
        }

        public Vector3 GetWorldPosition(int mazeX, int mazeY)
        {
            if (_tilemap == null)
                return Vector3.zero;

            // Конвертируем координаты лабиринта в мировые координаты
            Vector3Int tilePosition = new Vector3Int(
                origin.x + mazeX, 
                origin.y + (_mazeHeight - 1 - mazeY), 
                0
            );
            
            return _tilemap.CellToWorld(tilePosition) + _tilemap.cellSize * 0.5f; // Центр тайла
        }

        public Vector2Int GetMazePosition(Vector3 worldPosition)
        {
            if (_tilemap == null)
                return new Vector2Int(-1, -1);

            // Конвертируем мировые координаты в координаты лабиринта
            Vector3Int cellPosition = _tilemap.WorldToCell(worldPosition);
            
            int mazeX = cellPosition.x - origin.x;
            int mazeY = _mazeHeight - 1 - (cellPosition.y - origin.y);
            
            return new Vector2Int(mazeX, mazeY);
        }

        public bool IsValidMazePosition(int mazeX, int mazeY)
        {
            if (_currentMaze == null)
                return false;

            return mazeX >= 0 && mazeX < _mazeWidth && 
                   mazeY >= 0 && mazeY < _mazeHeight &&
                   _currentMaze[mazeY, mazeX] == 0; // Можно ходить только по проходам
        }

        public Vector2Int GetStartPosition()
        {
            // Ищем вход на периметре лабиринта
            for (int y = 0; y < _mazeHeight; y++)
            {
                // Проверяем левую сторону
                if (_currentMaze[y, 0] == 0)
                    return new Vector2Int(0, y);
                    
                // Проверяем правую сторону
                if (_currentMaze[y, _mazeWidth - 1] == 0)
                    return new Vector2Int(_mazeWidth - 1, y);
            }
            
            for (int x = 0; x < _mazeWidth; x++)
            {
                // Проверяем верхнюю сторону
                if (_currentMaze[0, x] == 0)
                    return new Vector2Int(x, 0);
                    
                // Проверяем нижнюю сторону
                if (_currentMaze[_mazeHeight - 1, x] == 0)
                    return new Vector2Int(x, _mazeHeight - 1);
            }
            
            // Fallback - центр левой стороны
            return new Vector2Int(0, _mazeHeight / 2);
        }

        public Vector3 GetPlayerSpawnWorldPosition()
        {
            Vector2Int startPos = GetStartPosition();
            return GetWorldPosition(startPos.x, startPos.y);
        }

        public Vector2Int GetExitPosition()
        {
            // Ищем выход на периметре лабиринта (исключаем вход)
            Vector2Int startPos = GetStartPosition();
            
            for (int y = 0; y < _mazeHeight; y++)
            {
                // Проверяем левую сторону
                Vector2Int leftPos = new Vector2Int(0, y);
                if (_currentMaze[y, 0] == 0 && leftPos != startPos)
                    return leftPos;
                    
                // Проверяем правую сторону
                Vector2Int rightPos = new Vector2Int(_mazeWidth - 1, y);
                if (_currentMaze[y, _mazeWidth - 1] == 0 && rightPos != startPos)
                    return rightPos;
            }
            
            for (int x = 0; x < _mazeWidth; x++)
            {
                // Проверяем верхнюю сторону
                Vector2Int topPos = new Vector2Int(x, 0);
                if (_currentMaze[0, x] == 0 && topPos != startPos)
                    return topPos;
                    
                // Проверяем нижнюю сторону
                Vector2Int bottomPos = new Vector2Int(x, _mazeHeight - 1);
                if (_currentMaze[_mazeHeight - 1, x] == 0 && bottomPos != startPos)
                    return bottomPos;
            }
            
            // Fallback - центр правой стороны (если не на левой стороне)
            return startPos.x == 0 ? new Vector2Int(_mazeWidth - 1, _mazeHeight / 2) : new Vector2Int(0, _mazeHeight / 2);
        }

        private void SpawnExitSpot(Transform playerTransform)
        {
            // Удаляем предыдущий ExitSpot если он есть
            if (_spawnedExitSpot != null)
            {
                DestroyImmediate(_spawnedExitSpot.gameObject);
                _spawnedExitSpot = null;
            }

            if (_exitSpotPrefab == null)
            {
                Debug.LogWarning("EscapeFromDarkLevelView: ExitSpot prefab is not assigned!");
                return;
            }

            // Получаем позицию выхода
            Vector2Int exitMazePos = GetExitPosition();
            Vector3 exitWorldPos = GetWorldPosition(exitMazePos.x, exitMazePos.y);

            // Вычисляем поворот в сторону лабиринта
            Quaternion exitRotation = GetExitRotation(exitMazePos);

            // Спавним ExitSpot
            var spawnedExitSpot = Instantiate(_exitSpotPrefab, exitWorldPos, exitRotation);

            // Инициализируем контроллер ExitSpot
            _spawnedExitSpot = spawnedExitSpot.GetComponent<ExitSpotController>();
            if (_spawnedExitSpot != null && playerTransform != null)
            {
                ExitSpotController.Ctx exitCtx = new ExitSpotController.Ctx
                {
                    playerTransform = playerTransform
                };
                _spawnedExitSpot.SetCtx(exitCtx);
            }
            else
            {
                Debug.LogWarning("EscapeFromDarkLevelView: ExitSpotController not found on prefab or player transform is null!");
            }

            Debug.Log($"EscapeFromDarkLevelView: ExitSpot spawned at {exitWorldPos} (maze pos {exitMazePos}) with rotation {exitRotation.eulerAngles}");
        }

        private Quaternion GetExitRotation(Vector2Int exitPosition)
        {
            // Определяем, на какой стороне находится выход, и поворачиваем в сторону лабиринта
            if (exitPosition.x == 0) // Левая сторона
            {
                return Quaternion.Euler(0, 0, 0); // Смотрит вправо (в лабиринт)
            }
            else if (exitPosition.x == _mazeWidth - 1) // Правая сторона
            {
                return Quaternion.Euler(0, 0, 180); // Смотрит влево (в лабиринт)
            }
            else if (exitPosition.y == 0) // Верхняя сторона
            {
                return Quaternion.Euler(0, 0, -90); // Смотрит вниз (в лабиринт)
            }
            else if (exitPosition.y == _mazeHeight - 1) // Нижняя сторона
            {
                return Quaternion.Euler(0, 0, 90); // Смотрит вверх (в лабиринт)
            }

            return Quaternion.identity; // По умолчанию
        }

        // Метод для отладки - показать координаты в консоли
        [ContextMenu("Debug Maze Info")]
        private void DebugMazeInfo()
        {
            if (_currentMaze == null)
            {
                Debug.Log("No maze data available");
                return;
            }
            
            Vector2Int start = GetStartPosition();
            Vector2Int exit = GetExitPosition();
            
            Debug.Log($"Maze size: {_mazeWidth}x{_mazeHeight}");
            Debug.Log($"Start position: {start} -> World: {GetWorldPosition(start.x, start.y)}");
            Debug.Log($"Exit position: {exit} -> World: {GetWorldPosition(exit.x, exit.y)}");
            Debug.Log($"Origin: {origin}");
            Debug.Log($"Floor Tilemap: {(_tilemap != null ? _tilemap.name : "Not assigned")}");
            Debug.Log($"Walls Tilemap: {(_wallsTilemap != null ? _wallsTilemap.name : "Not assigned")}");
            Debug.Log($"Brush Tiles: {(_brushTiles != null ? _brushTiles.name : "Not assigned")}");
            Debug.Log($"Floor Tile: {(_floorTile != null ? _floorTile.name : "Not assigned")}");
            Debug.Log($"Exit Spot: {(_exitSpotPrefab != null ? _exitSpotPrefab.name : "Not assigned")}");
            Debug.Log($"Spawned Exit Spot: {(_spawnedExitSpot != null ? _spawnedExitSpot.name : "Not spawned")}");
        }

        private void OnDestroy()
        {
            // Очищаем спавненный ExitSpot при уничтожении компонента
            if (_spawnedExitSpot != null)
            {
                DestroyImmediate(_spawnedExitSpot.gameObject);
                _spawnedExitSpot = null;
            }
        }

        // Метод для ручного пересоздания ExitSpot
        [ContextMenu("Respawn Exit Spot")]
        private void RespawnExitSpot()
        {
            // Для ручного пересоздания без игрока
            SpawnExitSpot(null);
        }
    }
}
