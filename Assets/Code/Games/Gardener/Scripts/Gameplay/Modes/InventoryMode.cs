using System;
using System.Collections.Generic;
using System.Linq;
using Disposable;
using GameShorts.Gardener.Data;
using GameShorts.Gardener.UI;
using R3;
using UnityEngine;

namespace GameShorts.Gardener.Gameplay.Modes
{
    /// <summary>
    /// Режим инвентаря - позволяет сажать семена из инвентаря на грядки
    /// </summary>
    internal class InventoryMode : DisposableBase, IGardenerMode
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
        
        // Observable для отслеживания изменений списка элементов
        private readonly ReactiveProperty<PlaceableItem[]> _placeableItemsChanged = new ReactiveProperty<PlaceableItem[]>(Array.Empty<PlaceableItem>());
        public ReadOnlyReactiveProperty<PlaceableItem[]> PlaceableItemsChanged => _placeableItemsChanged;

        public InventoryMode(Ctx ctx)
        {
            _ctx = ctx;
            
            // Subscribe to inventory changes to update placeable items
            if (_ctx.inventoryManager != null)
            {
                AddDisposable(_ctx.inventoryManager.InventoryChanged
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

        public void OnPlotPressed(PlotPm plot, Vector3 worldPosition, Vector2 screenPosition)
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
            
            // Принудительно обновляем список элементов
            UpdatePlaceableItems();
        }

        private void UpdatePlaceableItems()
        {
            if (_ctx.inventoryManager == null)
            {
                _cachedPlaceableItems = Array.Empty<PlaceableItem>();
                _placeableItemsChanged.Value = _cachedPlaceableItems;
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

                // Определяем префаб для превью (зрелое растение)
                GameObject previewPrefab = plantSettings.HasFruits 
                    ? plantSettings.FruitModel 
                    : plantSettings.FloweringModel;
                
                // Создаем PlaceableItem для каждого типа семян
                var item = new PlaceableItem
                {
                    ItemName = plantSettings.PlantName,
                    Icon = GetSeedIcon(plantSettings),
                    Prefab = previewPrefab, // Используем модель зрелого растения для превью
                    Count = count,
                    PlantSettings = plantSettings
                };

                items.Add(item);
            }

            _cachedPlaceableItems = items.ToArray();
            _placeableItemsChanged.Value = _cachedPlaceableItems; // Уведомляем об изменении
            Debug.Log($"Updated placeable items: {_cachedPlaceableItems.Length} types available");
        }

        private Sprite GetSeedIcon(PlantSettings plantSettings)
        {
            // Берем иконку из настроек растения
            return plantSettings.SeedIcon;
        }

        protected override void OnDispose()
        {
            _placeableItemsChanged?.Dispose();
            // Базовый dispose очистит все подписки через AddDispose
        }
    }
}

