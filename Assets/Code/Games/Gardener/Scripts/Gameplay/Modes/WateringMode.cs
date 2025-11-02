using System;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay.Modes
{
    /// <summary>
    /// Режим полива
    /// Позволяет поливать грядки при нажатии
    /// </summary>
    internal class WateringMode : IGardenerMode
    {
        public struct Ctx
        {
            public Action<PlotPm, Vector3> onPlotInteraction;
        }
        
        public string ModeName => "Watering";
        
        private readonly Ctx _ctx;
        
        public WateringMode(Ctx ctx)
        {
            _ctx = ctx;
        }
        
        public void OnEnter()
        {
            Debug.Log("Watering Mode activated");
        }
        
        public void OnExit()
        {
            Debug.Log("Watering Mode deactivated");
        }
        
        public void OnPlotPressed(PlotPm plot, Vector3 worldPosition)
        {
            if (plot == null)
                return;
            
            // Поливаем грядку
            plot.Water();
            Debug.Log("Watered plot!");
            
            _ctx.onPlotInteraction?.Invoke(plot, worldPosition);
        }
        
        public void OnPlotHeld(PlotPm plot, Vector3 worldPosition, float holdTime)
        {
            // В режиме полива удержание не используется
            // Можно добавить непрерывный полив при удержании, если нужно
        }
        
        public void OnPlotReleased(PlotPm plot)
        {
            // В режиме полива отпускание не обрабатывается
        }
        
        public PlaceableItem[] GetPlaceableItems()
        {
            // Режим полива не имеет элементов для размещения
            return null;
        }
        
        public void OnItemPlaced(PlaceableItem item, Vector3 worldPosition)
        {
            // Режим полива не поддерживает размещение элементов
        }
        
        public void Dispose()
        {
            // Нет ресурсов для освобождения
        }
    }
}

