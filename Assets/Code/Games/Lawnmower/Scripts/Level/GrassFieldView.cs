using Code.Games.Lawnmower.Scripts.Grass;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Level
{
    internal class GrassFieldView : MonoBehaviour
    {
        [Header("Field Info")]
        [SerializeField] private string fieldName = "Grass Field";
        
        [Header("Grass Grid")]
        [SerializeField] private GrassGridInstanced grassGrid;
        
        // Field state
        private bool _isCompleted = false;
        private float _lastCompletionCheck = 0f;
        private const float CHECK_INTERVAL = 0.5f; // Проверяем завершенность каждые 0.5 секунды
        private bool _pendingRestore = false;
        
        // Events
        public System.Action<GrassFieldView> OnFieldCompleted;
        
        // Properties
        public string FieldName => fieldName;
        public bool IsCompleted => _isCompleted;
        public GrassGridInstanced GrassGrid => grassGrid;
        
        private void Update()
        {
            if (!_isCompleted && Time.time - _lastCompletionCheck >= CHECK_INTERVAL)
            {
                _lastCompletionCheck = Time.time;
                CheckCompletion();
            }
        }

        private void OnEnable()
        {
            if (_pendingRestore)
            {
                _pendingRestore = false;
                StartCoroutine(WaitForInitializationAndRestore());
            }
        }
        
        public void ResetField()
        {
            _isCompleted = false;
            
            if (grassGrid != null && grassGrid.IsInitialized())
            {
                grassGrid.RestoreAllGrass();
            }
            else if (grassGrid != null)
            {
                // Если GrassGrid не инициализирован, попробуем инициализировать его принудительно
                Debug.LogWarning($"GrassField '{fieldName}': GrassGrid not initialized, forcing initialization");
                if (!isActiveAndEnabled)
                {
                    _pendingRestore = true;
                    return;
                }

                StartCoroutine(WaitForInitializationAndRestore());
            }
        }
        
        private System.Collections.IEnumerator WaitForInitializationAndRestore()
        {
            // Ждем несколько кадров, чтобы дать время GrassGrid инициализироваться
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();
            
            if (grassGrid != null && grassGrid.IsInitialized())
            {
                grassGrid.RestoreAllGrass();
            }
            else
            {
                Debug.LogError($"GrassField '{fieldName}': Failed to initialize GrassGrid");
            }
        }
        
        public int CutGrassAtPosition(Vector3 worldPosition, float radius = 1f)
        {
            if (grassGrid == null || !grassGrid.IsInitialized()) return 0;
            
            int cutTilesCount = 0;
            
            // Используем радиус стрижки
            if (radius <= 0.5f)
            {
                // Если радиус маленький, режем только одну точку
                if (grassGrid.CutGrassAtPosition(worldPosition))
                    cutTilesCount = 1;
            }
            else
            {
                // Если радиус больше, режем в радиусе
                cutTilesCount = CutGrassInRadius(worldPosition, radius);
            }
            
            return cutTilesCount;
        }
        
        private int CutGrassInRadius(Vector3 centerPosition, float radius)
        {
            if (grassGrid == null || !grassGrid.IsInitialized()) return 0;
            
            int cutTilesCount = 0;
            Vector2Int gridSize = grassGrid.GetGridSize();
            
            // Получаем индексы центрального тайла
            Vector2Int centerGridPos = grassGrid.GetGridIndices(centerPosition);
            
            // Вычисляем область поиска (оптимизация - не проверяем весь грид)
            int radiusInTiles = Mathf.CeilToInt(radius / Mathf.Max(grassGrid.tileSize.x, grassGrid.tileSize.y)) + 1;
            
            int minX = Mathf.Max(0, centerGridPos.x - radiusInTiles);
            int maxX = Mathf.Min(gridSize.x - 1, centerGridPos.x + radiusInTiles);
            int minY = Mathf.Max(0, centerGridPos.y - radiusInTiles);
            int maxY = Mathf.Min(gridSize.y - 1, centerGridPos.y + radiusInTiles);
            
            // Проходим только по тайлам в области радиуса
            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    Vector3 tileWorldPos = GetTileWorldPosition(x, y);
                    float distance = Vector3.Distance(centerPosition, tileWorldPos);
                    
                    if (distance <= radius)
                    {
                        if (grassGrid.CutGrassAt(x, y))
                            cutTilesCount++;
                    }
                }
            }
            
            return cutTilesCount;
        }
        
        private Vector3 GetTileWorldPosition(int gridX, int gridY)
        {
            // Используем метод из GrassGridInstanced для точного получения позиции
            return grassGrid.GetTileWorldPosition(gridX, gridY);
        }
        
        private void CheckCompletion()
        {
            if (grassGrid == null) return;
            
            float cutPercentage = CalculateCutPercentage();
            
            if (cutPercentage >= 1 && !_isCompleted)
            {
                _isCompleted = true;
                OnFieldCompleted?.Invoke(this);
                Debug.Log($"Field '{fieldName}' completed! Cut percentage: {cutPercentage:P1}");
            }
        }
        
        private float CalculateCutPercentage()
        {
            if (grassGrid == null || !grassGrid.IsInitialized()) return 0f;
            
            return grassGrid.GetCutPercentage();
        }
        
        public float GetCompletionPercentage()
        {
            return CalculateCutPercentage();
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            // Отрисовываем границы поля
            if (grassGrid != null)
            {
                Gizmos.color = _isCompleted ? Color.green : Color.red;
                Gizmos.DrawWireCube(transform.position, transform.localScale);
            }
        }
#endif
    }
}
