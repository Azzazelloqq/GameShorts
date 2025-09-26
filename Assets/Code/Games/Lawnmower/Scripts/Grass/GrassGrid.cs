using Code.Games.Lawnmower.Scripts.Grass;
using UnityEngine;

public class GrassGrid : MonoBehaviour, IGrassGrid
{
    [Header("Grid Settings")]
    [SerializeField] private GameObject grassTilePrefab;
    [SerializeField] private int gridWidth = 10;
    [SerializeField] private int gridHeight = 10;
    [SerializeField] private float tileSize = 1f;
    [SerializeField] private Vector2 gridOffset = Vector2.zero;
    [SerializeField] private float baseSeed = 1.0f;
    
    [Header("Randomization")]
    [SerializeField] private float lengthVariation = 3f; // ±3 пикселя для длины
    [SerializeField] private float widthVariation = 2f;  // ±2 пикселя для ширины
    
    [Header("Input")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask grassLayerMask = -1;
    
    [Header("Debug")]
    [SerializeField] private bool showGridGizmos = true;
    [SerializeField] private Color gizmoColor = Color.green;
    
    private GrassTile[,] grassTiles;
    private bool isInitialized = false;
    
    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }
    
    private void Start()
    {
        InitializeGrid();
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    /// <summary>
    /// Инициализация сетки травы
    /// </summary>
    private void InitializeGrid()
    {
        if (grassTilePrefab == null)
        {
            Debug.LogError("GrassGrid: grassTilePrefab is not assigned!");
            return;
        }
        
        // Очищаем старую сетку если есть
        ClearGrid();
        
        // Создаем массив
        grassTiles = new GrassTile[gridWidth, gridHeight];
        
        // Создаем тайлы
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 position = GetTileWorldPosition(x, y);
                GameObject tileObject = Instantiate(grassTilePrefab, position, Quaternion.identity, transform);
                tileObject.name = $"GrassTile_{x}_{y}";
                
                GrassTile grassTile = tileObject.GetComponent<GrassTile>();
                if (grassTile == null)
                {
                    grassTile = tileObject.AddComponent<GrassTile>();
                }
                
                // Устанавливаем уникальный seed для каждого тайла
                SetUniqueSeedForTile(grassTile, x, y);
                
                // Принудительно обновляем материал тайла
                grassTile.RefreshMaterial();
                
                grassTiles[x, y] = grassTile;
            }
        }
        
        isInitialized = true;
        Debug.Log($"GrassGrid: Initialized {gridWidth}x{gridHeight} grid with {gridWidth * gridHeight} tiles");
    }
    
    /// <summary>
    /// Установить уникальный seed для тайла
    /// </summary>
    private void SetUniqueSeedForTile(GrassTile grassTile, int x, int y)
    {
        if (grassTile == null) return;
        
        // Генерируем уникальный seed на основе позиции и базового seed
        float uniqueSeed = baseSeed + (x * 17.3f) + (y * 23.7f) + (x * y * 0.1f);
        
        // Получаем материал травы и устанавливаем seed
        SpriteRenderer spriteRenderer = grassTile.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null && spriteRenderer.material != null)
        {
            // Создаем уникальный экземпляр материала для каждого тайла
            Material uniqueMaterial = new Material(spriteRenderer.material);
            uniqueMaterial.SetFloat("_Seed", uniqueSeed);
            
            // Убеждаемся, что трава изначально видна (Shrink = 0)
            uniqueMaterial.SetFloat("_Shrink", 0f);
            
            // Генерируем случайные параметры для каждого тайла
            System.Random random = new System.Random((int)(uniqueSeed * 1000));
            
            // Случайная длина травы
            float baseLength = uniqueMaterial.GetFloat("_GrassLength");
            if (baseLength == 0) baseLength = 5f; // Значение по умолчанию
            float randomLength = baseLength + (float)(random.NextDouble() * lengthVariation * 2 - lengthVariation);
            randomLength = Mathf.Max(randomLength, 2f); // Минимум 2 пикселя
            uniqueMaterial.SetFloat("_GrassLength", randomLength);
            
            // Случайная ширина травы
            float baseWidth = uniqueMaterial.GetFloat("_GrassWidth");
            if (baseWidth == 0) baseWidth = 0.8f; // Значение по умолчанию
            float randomWidth = baseWidth + (float)(random.NextDouble() * widthVariation * 2 - widthVariation);
            randomWidth = Mathf.Max(randomWidth, 0.3f); // Минимум 0.3 пикселя
            uniqueMaterial.SetFloat("_GrassWidth", randomWidth);
            
            spriteRenderer.material = uniqueMaterial;
            
            Debug.Log($"Set unique seed {uniqueSeed} for tile ({x}, {y}) - Length: {randomLength:F1}, Width: {randomWidth:F1}");
        }
    }
    
    /// <summary>
    /// Очистка старой сетки
    /// </summary>
    private void ClearGrid()
    {
        if (grassTiles != null)
        {
            for (int x = 0; x < grassTiles.GetLength(0); x++)
            {
                for (int y = 0; y < grassTiles.GetLength(1); y++)
                {
                    if (grassTiles[x, y] != null && grassTiles[x, y].gameObject != null)
                    {
                        DestroyImmediate(grassTiles[x, y].gameObject);
                    }
                }
            }
        }
        
        // Удаляем все дочерние объекты
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            DestroyImmediate(transform.GetChild(i).gameObject);
        }
    }
    
    /// <summary>
    /// Получить мировую позицию тайла по индексам сетки
    /// </summary>
    private Vector3 GetTileWorldPosition(int gridX, int gridY)
    {
        float worldX = gridX * tileSize + gridOffset.x;
        float worldY = gridY * tileSize + gridOffset.y;
        return new Vector3(worldX, worldY, 0f);
    }
    
    /// <summary>
    /// Получить индексы сетки по мировой позиции
    /// </summary>
    private Vector2Int GetGridIndices(Vector3 worldPosition)
    {
        int gridX = Mathf.FloorToInt((worldPosition.x - gridOffset.x) / tileSize);
        int gridY = Mathf.FloorToInt((worldPosition.y - gridOffset.y) / tileSize);
        return new Vector2Int(gridX, gridY);
    }
    
    /// <summary>
    /// Проверить, находятся ли индексы в пределах сетки
    /// </summary>
    private bool IsValidGridPosition(int gridX, int gridY)
    {
        return gridX >= 0 && gridX < gridWidth && gridY >= 0 && gridY < gridHeight;
    }
    
    /// <summary>
    /// Обработка пользовательского ввода отключена - трава стрижется только при движении игрока
    /// </summary>
    private void HandleInput()
    {
        // Ввод отключен
    }
    
    /// <summary>
    /// Обработка клика по позиции
    /// </summary>
    private void HandleClick(Vector3 worldPosition)
    {
        Vector2Int gridPos = GetGridIndices(worldPosition);
        
        if (IsValidGridPosition(gridPos.x, gridPos.y))
        {
            GrassTile tile = grassTiles[gridPos.x, gridPos.y];
            if (tile != null)
            {
                tile.CutGrass();
                Debug.Log($"Cut grass at grid position ({gridPos.x}, {gridPos.y})");
            }
        }
    }
    
    /// <summary>
    /// Убрать траву в определенной позиции сетки
    /// </summary>
    public bool CutGrassAt(int gridX, int gridY)
    {
        if (IsValidGridPosition(gridX, gridY) && grassTiles[gridX, gridY] != null)
        {
            bool wasVisible = grassTiles[gridX, gridY].IsGrassVisible;
            grassTiles[gridX, gridY].CutGrass();
            return wasVisible;
        }
        return false;
    }
    
    /// <summary>
    /// Восстановить траву в определенной позиции сетки
    /// </summary>
    public void RestoreGrassAt(int gridX, int gridY)
    {
        if (IsValidGridPosition(gridX, gridY) && grassTiles[gridX, gridY] != null)
        {
            grassTiles[gridX, gridY].RestoreGrass();
        }
    }
    
    /// <summary>
    /// Переключить состояние травы в определенной позиции
    /// </summary>
    public void ToggleGrassAt(int gridX, int gridY)
    {
        if (IsValidGridPosition(gridX, gridY) && grassTiles[gridX, gridY] != null)
        {
            grassTiles[gridX, gridY].ToggleGrass();
        }
    }
    
    /// <summary>
    /// Восстановить всю траву
    /// </summary>
    public void RestoreAllGrass()
    {
        if (!isInitialized) return;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grassTiles[x, y] != null)
                {
                    grassTiles[x, y].RestoreGrass();
                }
            }
        }
    }
    
    /// <summary>
    /// Убрать всю траву
    /// </summary>
    public void CutAllGrass()
    {
        if (!isInitialized) return;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                if (grassTiles[x, y] != null)
                {
                    grassTiles[x, y].CutGrass();
                }
            }
        }
    }
    
    /// <summary>
    /// Получить состояние травы в определенной позиции
    /// </summary>
    public bool IsGrassVisibleAt(int gridX, int gridY)
    {
        if (IsValidGridPosition(gridX, gridY) && grassTiles[gridX, gridY] != null)
        {
            return grassTiles[gridX, gridY].IsGrassVisible;
        }
        return false;
    }
    
    /// <summary>
    /// Получить тайл травы по индексам
    /// </summary>
    public GrassTile GetGrassTileAt(int gridX, int gridY)
    {
        if (IsValidGridPosition(gridX, gridY))
        {
            return grassTiles[gridX, gridY];
        }
        return null;
    }
    
    /// <summary>
    /// Изменить базовый seed и обновить все тайлы
    /// </summary>
    public void SetBaseSeed(float newSeed)
    {
        baseSeed = newSeed;
        
        if (isInitialized && grassTiles != null)
        {
            // Обновляем seed для всех существующих тайлов
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    if (grassTiles[x, y] != null)
                    {
                        SetUniqueSeedForTile(grassTiles[x, y], x, y);
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Получить текущий базовый seed
    /// </summary>
    public float GetBaseSeed()
    {
        return baseSeed;
    }
    
    /// <summary>
    /// Убрать траву в мировой позиции
    /// </summary>
    public bool CutGrassAtPosition(Vector3 worldPosition)
    {
        Vector2Int gridPos = GetGridIndices(worldPosition);
        return CutGrassAt(gridPos.x, gridPos.y);
    }
    
    /// <summary>
    /// Получить размер сетки
    /// </summary>
    public Vector2Int GetGridSize()
    {
        return new Vector2Int(gridWidth, gridHeight);
    }
    
    /// <summary>
    /// Проверить, инициализована ли сетка
    /// </summary>
    public bool IsInitialized()
    {
        return isInitialized;
    }
    
    // Editor methods
    #if UNITY_EDITOR
    [ContextMenu("Regenerate Grid")]
    private void RegenerateGrid()
    {
        InitializeGrid();
    }
    
    [ContextMenu("Randomize Seeds")]
    private void RandomizeSeedsEditor()
    {
        SetBaseSeed(Random.Range(1f, 1000f));
    }
    #endif
    
    private void OnDrawGizmos()
    {
        if (!showGridGizmos) return;
        
        Gizmos.color = gizmoColor;
        
        for (int x = 0; x <= gridWidth; x++)
        {
            Vector3 start = GetTileWorldPosition(x, 0) - Vector3.right * tileSize * 0.5f;
            Vector3 end = GetTileWorldPosition(x, gridHeight - 1) + Vector3.up * tileSize - Vector3.right * tileSize * 0.5f;
            Gizmos.DrawLine(start, end);
        }
        
        for (int y = 0; y <= gridHeight; y++)
        {
            Vector3 start = GetTileWorldPosition(0, y) - Vector3.up * tileSize * 0.5f;
            Vector3 end = GetTileWorldPosition(gridWidth - 1, y) + Vector3.right * tileSize - Vector3.up * tileSize * 0.5f;
            Gizmos.DrawLine(start, end);
        }
    }
}
