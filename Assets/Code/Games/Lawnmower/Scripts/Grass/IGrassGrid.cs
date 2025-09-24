using UnityEngine;

namespace Code.Games.Lawnmower.Scripts.Grass
{
    /// <summary>
    /// Интерфейс для работы с разными типами сеток травы
    /// </summary>
    internal interface IGrassGrid
    {
    /// <summary>
    /// Убрать траву в определенной позиции сетки
    /// </summary>
    bool CutGrassAt(int gridX, int gridY);
    
        /// <summary>
        /// Восстановить траву в определенной позиции сетки
        /// </summary>
        void RestoreGrassAt(int gridX, int gridY);
    
        /// <summary>
        /// Убрать траву в мировой позиции
        /// </summary>
        bool CutGrassAtPosition(Vector3 worldPosition);
    
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
}
