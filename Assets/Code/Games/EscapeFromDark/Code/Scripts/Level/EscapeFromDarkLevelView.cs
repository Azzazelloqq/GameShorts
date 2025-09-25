using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Level
{
    internal class EscapeFromDarkLevelView : BaseMonoBehaviour
    {
        [Header("Tilemap Settings")]
        [SerializeField] private Tilemap _tilemap;
        [SerializeField] private TileBase _brushTiles;
        [SerializeField] private Tilemap _wallsTilemap;
        [SerializeField] private TileBase _floorTile;
        [SerializeField] private Vector2Int origin = Vector2Int.zero;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;
        
        private int[,] _currentMaze;
        private int _mazeWidth;
        private int _mazeHeight;

        internal struct Ctx
        {
            public int[,] mazeData;
            public int mazeWidth;
            public int mazeHeight;
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
            
            RenderMaze();
            
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
            // Конвертируем мировые координаты в координаты лабиринта
            Vector3Int cellPosition = _tilemap.WorldToCell(worldPosition);
            
            int mazeX = cellPosition.x - origin.x;
            int mazeY = _mazeHeight - 1 - (cellPosition.y - origin.y);
            
            return new Vector2Int(mazeX, mazeY);
        }

        public bool IsValidMazePosition(int mazeX, int mazeY)
        {
            return mazeX >= 0 && mazeX < _mazeWidth && 
                   mazeY >= 0 && mazeY < _mazeHeight &&
                   _currentMaze[mazeY, mazeX] == 0; // Можно ходить только по проходам
        }

        public Vector2Int GetStartPosition()
        {
            // Стартовая позиция всегда [0, 5] или центр по высоте
            return new Vector2Int(0, Mathf.Min(5, _mazeHeight / 2));
        }

        public Vector3 GetPlayerSpawnWorldPosition()
        {
            Vector2Int startPos = GetStartPosition();
            return GetWorldPosition(startPos.x, startPos.y);
        }

        public Vector2Int GetExitPosition()
        {
            // Ищем выход на правой стороне
            for (int y = 0; y < _mazeHeight; y++)
            {
                if (_currentMaze[y, _mazeWidth - 1] == 0)
                {
                    return new Vector2Int(_mazeWidth - 1, y);
                }
            }
            
            // Если не найден, возвращаем центр правой стороны
            return new Vector2Int(_mazeWidth - 1, _mazeHeight / 2);
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
        }
    }
}
