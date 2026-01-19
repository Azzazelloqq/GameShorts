using Code.Games.Lawnmower.Scripts.Grass;
using UnityEngine;

/// <summary>
/// Дополнительный контроллер для управления травой с расширенным функционалом
/// </summary>
internal class GrassController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MonoBehaviour grassGridComponent;
    
    [Header("Input Settings")]
    [SerializeField] private bool enableTouchInput = true;
    [SerializeField] private bool enableMouseInput = true;
    [SerializeField] private float touchRadius = 1f;
    
    [Header("Cutting Patterns")]
    [SerializeField] private bool cutInRadius = false;
    [SerializeField] private int cutRadius = 1;
    
    
    private Camera mainCamera;
    private IGrassGrid grassGrid;
    
    private void Awake()
    {
        if (grassGridComponent == null)
            grassGridComponent = GetComponent<MonoBehaviour>();
            
        grassGrid = grassGridComponent as IGrassGrid;
        if (grassGrid == null)
        {
            Debug.LogError("GrassController: grassGridComponent must implement IGrassGrid interface!");
        }
            
        mainCamera = Camera.main;
        if (mainCamera == null)
            mainCamera = FindObjectOfType<Camera>();
    }
    
    private void Update()
    {
        HandleInput();
    }
    
    private void HandleInput()
    {
        if (mainCamera == null || grassGrid == null) return;
        
        Vector3 inputWorldPos = Vector3.zero;
        bool hasInput = false;
        
        // Обработка мыши отключена - трава стрижется только при движении игрока
        
        // Обработка тач-ввода также отключена
        
        if (hasInput)
        {
            HandleInputAtPosition(inputWorldPos);
        }
    }
    
    private void HandleInputAtPosition(Vector3 worldPosition)
    {
        if (cutInRadius)
        {
            CutGrassInRadius(worldPosition, cutRadius);
        }
        else
        {
            CutGrassAtPosition(worldPosition);
        }
    }
    
    /// <summary>
    /// Убрать траву в определенной позиции
    /// </summary>
    public void CutGrassAtPosition(Vector3 worldPosition)
    {
        if (grassGrid == null) return;
        
        grassGrid.CutGrassAtPosition(worldPosition);
    }
    
    /// <summary>
    /// Убрать траву в радиусе от позиции
    /// </summary>
    public void CutGrassInRadius(Vector3 worldPosition, int radius)
    {
        Vector2Int centerGrid = WorldToGridPosition(worldPosition);
        
        for (int x = -radius; x <= radius; x++)
        {
            for (int y = -radius; y <= radius; y++)
            {
                // Проверяем, что точка находится в круге
                if (x * x + y * y <= radius * radius)
                {
                    int gridX = centerGrid.x + x;
                    int gridY = centerGrid.y + y;
                    
                    grassGrid.CutGrassAt(gridX, gridY);
                }
            }
        }
    }
    
    /// <summary>
    /// Преобразование мировых координат в координаты сетки
    /// </summary>
    private Vector2Int WorldToGridPosition(Vector3 worldPosition)
    {
        // Эта логика должна совпадать с GrassGrid.GetGridIndices
        // Если у GrassGrid есть публичный метод, лучше использовать его
        int gridX = Mathf.FloorToInt(worldPosition.x);
        int gridY = Mathf.FloorToInt(worldPosition.y);
        return new Vector2Int(gridX, gridY);
    }
    
    
    /// <summary>
    /// Установить радиус резки
    /// </summary>
    public void SetCutRadius(int radius)
    {
        cutRadius = Mathf.Max(0, radius);
        cutInRadius = radius > 0;
    }
    
    /// <summary>
    /// Включить/выключить резку в радиусе
    /// </summary>
    public void SetCutInRadius(bool enabled)
    {
        cutInRadius = enabled;
    }
}
