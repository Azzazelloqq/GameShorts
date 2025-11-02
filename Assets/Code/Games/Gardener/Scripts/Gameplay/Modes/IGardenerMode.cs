using System;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay.Modes
{
    /// <summary>
    /// Интерфейс для режимов игры Gardener
    /// Позволяет легко расширять функционал, добавляя новые режимы
    /// </summary>
    internal interface IGardenerMode : IDisposable
    {
        /// <summary>
        /// Название режима
        /// </summary>
        string ModeName { get; }
        
        /// <summary>
        /// Вызывается при входе в режим
        /// </summary>
        void OnEnter();
        
        /// <summary>
        /// Вызывается при выходе из режима
        /// </summary>
        void OnExit();
        
        /// <summary>
        /// Вызывается при нажатии на грядку
        /// </summary>
        void OnPlotPressed(PlotPm plot, Vector3 worldPosition);
        
        /// <summary>
        /// Вызывается при удержании грядки
        /// </summary>
        void OnPlotHeld(PlotPm plot, Vector3 worldPosition, float holdTime);
        
        /// <summary>
        /// Вызывается при отпускании грядки
        /// </summary>
        void OnPlotReleased(PlotPm plot);
        
        /// <summary>
        /// Возвращает список элементов, доступных для размещения в этом режиме
        /// Может вернуть null, если режим не поддерживает drag-and-drop
        /// </summary>
        PlaceableItem[] GetPlaceableItems();
        
        /// <summary>
        /// Вызывается при размещении элемента на сцене
        /// </summary>
        void OnItemPlaced(PlaceableItem item, Vector3 worldPosition);
    }
}

