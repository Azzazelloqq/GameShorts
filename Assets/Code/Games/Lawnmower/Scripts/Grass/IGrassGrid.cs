using UnityEngine;

/// <summary>
/// Интерфейс для работы с разными типами сеток травы
/// </summary>
public interface IGrassGrid
{
    /// <summary>
    /// Убрать траву в определенной позиции сетки
    /// </summary>
    void CutGrassAt(int gridX, int gridY);
    
    /// <summary>
    /// Восстановить траву в определенной позиции сетки
    /// </summary>
    void RestoreGrassAt(int gridX, int gridY);
    
    /// <summary>
    /// Убрать траву в мировой позиции
    /// </summary>
    void CutGrassAtPosition(Vector3 worldPosition);
    
    /// <summary>
    /// Восстановить всю траву
    /// </summary>
    void RestoreAllGrass();
    
    /// <summary>
    /// Убрать всю траву
    /// </summary>
    void CutAllGrass();
    
    /// <summary>
    /// Получить размер сетки
    /// </summary>
    Vector2Int GetGridSize();
    
    /// <summary>
    /// Проверить, инициализована ли сетка
    /// </summary>
    bool IsInitialized();
}
