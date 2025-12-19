using System;
using Disposable;
using GameShorts.Gardener.Data;
using GameShorts.Gardener.UI;
using LightDI.Runtime;
using TickHandler;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay.Modes
{
    /// <summary>
    /// Режим сбора урожая Harvey
    /// Позволяет собирать урожай удержанием и размещать грядки через drag-and-drop
    /// </summary>
    internal class HarveyMode : DisposableBase, IGardenerMode
    {
        public struct Ctx
        {
            public PlaceableItem[] placeableItems;
            public HarvestProgressBar harvestProgressBar;
            public GardenerGameSettings gameSettings;
            public Action<PlotPm, Vector3> onPlotInteraction;
            public Action<PlaceableItem, Vector3> onItemPlaced;
            public Action<PlotPm> onPlotRemoved;
            public Camera camera;
        }
        
        public string ModeName => "Harvey";
        
        private readonly Ctx _ctx;
        private readonly ITickHandler _tickHandler;
        private PlotPm _currentHeldPlot;
        private float _holdTime;
        private bool _isHolding;
        private Vector3 _holdPosition;
        
        public HarveyMode(Ctx ctx, [Inject] ITickHandler tickHandler)
        {
            _ctx = ctx;
            _tickHandler = tickHandler;
            _tickHandler.FrameUpdate += OnUpdate;
        }
        
        public void OnEnter()
        {
            Debug.Log("Harvey Mode activated");
            _isHolding = false;
            _holdTime = 0f;
            _currentHeldPlot = null;
        }
        
        public void OnExit()
        {
            Debug.Log("Harvey Mode deactivated");
            
            // Сбрасываем состояние удержания
            if (_isHolding)
            {
                ResetHold();
            }
        }
        
        public void OnPlotPressed(PlotPm plot, Vector3 worldPosition, Vector2 screenPosition)
        {
            if (plot == null)
                return;
            
            // Если растение зрелое - собираем сразу по клику без удержания
            if (plot.IsPlantMature())
            {
                ProcessHarvest(plot);
                _ctx.onPlotInteraction?.Invoke(plot, worldPosition);
                return;
            }
            
            // Для незрелых или гнилых растений - начинаем удержание
            _currentHeldPlot = plot;
            _holdPosition = worldPosition;
            _isHolding = true;
            _holdTime = 0f;
            
            // Показываем прогресс-бар в позиции курсора (используем screenPosition напрямую)
            if (_ctx.harvestProgressBar != null)
            {
                _ctx.harvestProgressBar.Show(screenPosition);
            }
            
            _ctx.onPlotInteraction?.Invoke(plot, worldPosition);
        }
        
        public void OnPlotHeld(PlotPm plot, Vector3 worldPosition, float holdTime)
        {
            // Обработка происходит в OnUpdate
        }
        
        public void OnPlotReleased(PlotPm plot)
        {
            if (!_isHolding)
                return;
            
            // Проверяем, достаточно ли удерживали
            if (_holdTime >= _ctx.gameSettings.HarvestHoldTime && _currentHeldPlot != null)
            {
                ProcessHarvest(_currentHeldPlot);
            }
            
            ResetHold();
        }
        
        public PlaceableItem[] GetPlaceableItems()
        {
            return _ctx.placeableItems;
        }
        
        public void OnItemPlaced(PlaceableItem item, Vector3 worldPosition)
        {
            _ctx.onItemPlaced?.Invoke(item, worldPosition);
        }
        
        private void OnUpdate(float deltaTime)
        {
            if (!_isHolding || _currentHeldPlot == null)
                return;
            
            _holdTime += deltaTime;
            
            // Обновляем прогресс-бар
            float progress = Mathf.Clamp01(_holdTime / _ctx.gameSettings.HarvestHoldTime);
            if (_ctx.harvestProgressBar != null)
            {
                _ctx.harvestProgressBar.UpdateProgress(progress);
            }
            
            // Автоматически собираем, когда достигли нужного времени
            if (_holdTime >= _ctx.gameSettings.HarvestHoldTime)
            {
                ProcessHarvest(_currentHeldPlot);
                ResetHold();
            }
        }
        
        private void ProcessHarvest(PlotPm plot)
        {
            if (plot == null)
                return;
            
            // Если созрело - собираем урожай
            if (plot.IsPlantMature())
            {
                plot.Harvest();
                Debug.Log("Harvested mature plant!");
                _ctx.onPlotRemoved?.Invoke(plot);
            }
            // Если сгнило или еще растет - просто удаляем
            else if (plot.IsPlantRotten() || plot.CurrentState != Data.PlantState.Empty)
            {
                plot.Clear();
                Debug.Log("Cleared plot!");
                _ctx.onPlotRemoved?.Invoke(plot);
            }
        }
        
        private void ResetHold()
        {
            _isHolding = false;
            _holdTime = 0f;
            _currentHeldPlot = null;
            
            if (_ctx.harvestProgressBar != null)
            {
                _ctx.harvestProgressBar.Hide();
            }
        }
        
        protected override void OnDispose()
        {
            _tickHandler.FrameUpdate -= OnUpdate;
            ResetHold();
        }
    }
}

