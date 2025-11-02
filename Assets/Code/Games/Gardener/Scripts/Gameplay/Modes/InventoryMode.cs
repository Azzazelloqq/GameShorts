using System;
using System.Collections.Generic;
using System.Linq;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Data;
using GameShorts.Gardener.UI;
using R3;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay.Modes
{
    /// <summary>
    /// Режим инвентаря - позволяет сажать семена из инвентаря на грядки
    /// </summary>
    internal class InventoryMode : BaseDisposable, IGardenerMode
    {
        public struct Ctx
        {
            public InventoryManager inventoryManager;
            public Func<Vector3, PlotPm> findPlotAtPosition;
            public PlotUIBarManager  plotUIBarManager;
        }
        
        public string ModeName => "Inventory";
        
        private readonly Ctx _ctx;
        private PlaceableItem[] _cachedPlaceableItems;

        public InventoryMode(Ctx ctx)
        {
            _ctx = ctx;
            
            // Subscribe to inventory changes to update placeable items
            if (_ctx.inventoryManager != null)
            {
                AddDispose(_ctx.inventoryManager.InventoryChanged
                    .Subscribe(_ => UpdatePlaceableItems()));
            }
            
            UpdatePlaceableItems();
        }

        public void OnEnter()
        {
            Debug.Log("Inventory Mode activated");
            UpdatePlaceableItems();
        }

        public void OnExit()
        {
            Debug.Log("Inventory Mode deactivated");
        }

        public void OnPlotPressed(PlotPm plot, Vector3 worldPosition)
        {
            // В режиме инвентаря не делаем ничего при клике на грядку
            // Взаимодействие происходит только через drag-and-drop
        }

        public void OnPlotHeld(PlotPm plot, Vector3 worldPosition, float holdTime)
        {
            // Не используется в режиме инвентаря
        }

        public void OnPlotReleased(PlotPm plot)
        {
            // Не используется в режиме инвентаря
        }

        public PlaceableItem[] GetPlaceableItems()
        {
            return _cachedPlaceableItems;
        }

        public void OnItemPlaced(PlaceableItem item, Vector3 worldPosition)
        {
            if (item == null || item.PlantSettings == null)
            {
                Debug.LogWarning("Cannot place item - item or PlantSettings is null");
                return;
            }

            // Находим грядку в указанной позиции
            var plot = _ctx.findPlotAtPosition?.Invoke(worldPosition);
            
            if (plot == null)
            {
                Debug.LogWarning("No plot found at position");
                return;
            }

            // Проверяем, что грядка пустая
            if (plot.CurrentState != PlantState.Empty)
            {
                Debug.LogWarning($"Plot is not empty! Current state: {plot.CurrentState}");
                return;
            }

            // Проверяем, что грядка подготовлена
            if (!plot.IsPreparationComplete)
            {
                Debug.LogWarning("Plot is not prepared yet!");
                return;
            }

            // Проверяем, есть ли семена в инвентаре
            if (!_ctx.inventoryManager.HasSeeds(item.PlantSettings))
            {
                Debug.LogWarning($"No seeds of {item.PlantSettings.PlantName} in inventory");
                return;
            }

            // Сажаем семена
            plot.PlantSeed(item.PlantSettings);
            Debug.Log($"Planted {item.PlantSettings.PlantName} on plot at {worldPosition}");

            // Уменьшаем количество в инвентаре
            _ctx.inventoryManager.RemoveSeeds(item.PlantSettings, 1);
            _ctx.plotUIBarManager.CreateBarForPlot(plot, plot.GameObject.transform);
        }

        private void UpdatePlaceableItems()
        {
            if (_ctx.inventoryManager == null)
            {
                _cachedPlaceableItems = Array.Empty<PlaceableItem>();
                return;
            }

            var availableSeeds = _ctx.inventoryManager.GetAvailableSeeds();
            var items = new List<PlaceableItem>();

            foreach (var kvp in availableSeeds)
            {
                var plantSettings = kvp.Key;
                var count = kvp.Value;

                if (plantSettings == null || count <= 0)
                    continue;

                // Создаем PlaceableItem для каждого типа семян
                var item = new PlaceableItem
                {
                    ItemName = plantSettings.PlantName,
                    Icon = GetSeedIcon(plantSettings),
                    Prefab = null, // Не нужен префаб, т.к. мы сажаем на существующую грядку
                    Count = count,
                    PlantSettings = plantSettings
                };

                items.Add(item);
            }

            _cachedPlaceableItems = items.ToArray();
            Debug.Log($"Updated placeable items: {_cachedPlaceableItems.Length} types");
        }

        private Sprite GetSeedIcon(PlantSettings plantSettings)
        {
            // Берем иконку из настроек растения
            return plantSettings.SeedIcon;
        }

        protected override void OnDispose()
        {
            // Базовый dispose очистит все подписки через AddDispose
        }
    }
}

