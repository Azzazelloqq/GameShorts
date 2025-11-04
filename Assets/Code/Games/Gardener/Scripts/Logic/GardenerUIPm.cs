using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Data;
using GameShorts.Gardener.UI;
using GameShorts.Gardener.Core;
using GameShorts.Gardener.Gameplay;
using GameShorts.Gardener.Gameplay.Modes;
using GameShorts.Gardener.View;
using R3;
using UnityEngine;

namespace GameShorts.Gardener.Logic
{
    internal class GardenerUIPm : BaseDisposable
    {
        public class Ctx
        {
            public GardenerMainUIView MainUIView { get; set; }
            public GardenerShopUIView ShopUIView { get; set; }
            public PlaceableItemsPanel PlaceableItemsPanel { get; set; }
            public CancellationToken CancellationToken { get; set; }
            public Action<PlantSettings> OnBuyPlant { get; set; }
            public ReactiveProperty<int> Money { get; set; }
            public PlantSettings[] AvailablePlants { get; set; }
            public GardenerModeManager ModeManager { get; set; }
            public Camera MainCamera { get; set; }
            public GardenBounds GardenBounds { get; set; }
            public Func<Vector3, PlotPm> FindPlotAtPosition { get; set; }
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _disposable = new CompositeDisposable();
        private IDisposable _currentModeSubscription; // Подписка на текущий режим

        public GardenerUIPm(Ctx ctx) 
        {
            _ctx = ctx;
            
            // Подписываемся на изменение денег
            _ctx.Money
                .Subscribe(money => _ctx.MainUIView.SetMoney(money))
                .AddTo(_disposable);

            // Настраиваем кнопки режимов
            if (_ctx.MainUIView.HarveyModeButton != null)
            {
                _ctx.MainUIView.HarveyModeButton.onClick.AddListener(() => SwitchMode("Harvey"));
            }

            if (_ctx.MainUIView.WateringModeButton != null)
            {
                _ctx.MainUIView.WateringModeButton.onClick.AddListener(() => SwitchMode("Watering"));
            }

            if (_ctx.MainUIView.InventoryModeButton != null)
            {
                _ctx.MainUIView.InventoryModeButton.onClick.AddListener(() => SwitchMode("Inventory"));
            }

            if (_ctx.MainUIView.ShopButton != null)
            {
                _ctx.MainUIView.ShopButton.onClick.AddListener(OpenShop);
            }

            // Настраиваем магазин
            if (_ctx.ShopUIView.CloseButton != null)
            {
                _ctx.ShopUIView.CloseButton.onClick.AddListener(CloseShop);
            }
            
            // Инициализируем панель placeable элементов
            if (_ctx.PlaceableItemsPanel != null)
            {
                _ctx.PlaceableItemsPanel.Initialize(_ctx.MainCamera, _ctx.GardenBounds, _ctx.FindPlotAtPosition);
            }
            
            // Подписываемся на смену режима для обновления UI
            _ctx.ModeManager.ActiveMode
                .Subscribe(OnModeChanged)
                .AddTo(_disposable);

            // Показываем основной UI
            _ctx.MainUIView.Show();
            _ctx.ShopUIView.Hide();
            
            if (_ctx.PlaceableItemsPanel != null)
            {
                _ctx.PlaceableItemsPanel.Hide();
            }
        }
        
        private void SwitchMode(string modeName)
        {
            _ctx.ModeManager.SwitchMode(modeName);
        }
        
        private void OnModeChanged(IGardenerMode mode)
        {
            // Отписываемся от предыдущего режима
            _currentModeSubscription?.Dispose();
            _currentModeSubscription = null;
            
            if (mode == null)
            {
                // Если режим не активен, скрываем панель и очищаем UI
                if (_ctx.PlaceableItemsPanel != null)
                {
                    _ctx.PlaceableItemsPanel.Hide();
                }
                
                // Сбрасываем отображение активного режима
                if (_ctx.MainUIView != null)
                {
                    _ctx.MainUIView.ClearActiveMode();
                }
                
                Debug.Log("Mode deactivated");
                return;
            }
            
            Debug.Log($"Mode changed to: {mode.ModeName}");
            
            // Обновляем UI в зависимости от режима
            UpdateModeUI(mode);
            
            // Подписываемся на изменения PlaceableItems для InventoryMode
            if (mode is InventoryMode inventoryMode)
            {
                _currentModeSubscription = inventoryMode.PlaceableItemsChanged
                    .Subscribe(items => UpdatePlaceableItemsPanel(mode, items));
            }
            
            // Обновляем панель placeable элементов
            var placeableItems = mode.GetPlaceableItems();
            UpdatePlaceableItemsPanel(mode, placeableItems);
        }
        
        private void UpdatePlaceableItemsPanel(IGardenerMode mode, PlaceableItem[] placeableItems)
        {
            if (placeableItems != null && placeableItems.Length > 0)
            {
                if (_ctx.PlaceableItemsPanel != null)
                {
                    // Передаем callback, который вызовет метод режима
                    _ctx.PlaceableItemsPanel.PopulateItems(placeableItems, (item, pos) => 
                    {
                        Debug.Log($"Item placed: {item.ItemName} at {pos}");
                        mode.OnItemPlaced(item, pos);
                    });
                    _ctx.PlaceableItemsPanel.Show();
                }
            }
            else
            {
                if (_ctx.PlaceableItemsPanel != null)
                {
                    _ctx.PlaceableItemsPanel.Hide();
                }
            }
        }
        
        private void UpdateModeUI(IGardenerMode mode)
        {
            // Обновляем отображение активного режима на кнопках
            _ctx.MainUIView.SetActiveMode(mode.ModeName);
        }

        private void OpenShop()
        {
            _ctx.ShopUIView.PopulateShop(_ctx.AvailablePlants, BuyPlant);
            _ctx.ShopUIView.Show();
            _ctx.MainUIView.Hide();
        }

        private void CloseShop()
        {
            _ctx.ShopUIView.Hide();
            _ctx.MainUIView.Show();
        }

        private void BuyPlant(PlantSettings plantSettings)
        {
            _ctx.OnBuyPlant?.Invoke(plantSettings);
            // Не закрываем магазин, чтобы игрок мог купить еще что-то
            // CloseShop();
        }

        protected override void OnDispose()
        {
            _currentModeSubscription?.Dispose();
            _disposable.Dispose();

            // Отписываемся от событий кнопок
            if (_ctx.MainUIView.HarveyModeButton != null)
            {
                _ctx.MainUIView.HarveyModeButton.onClick.RemoveAllListeners();
            }

            if (_ctx.MainUIView.WateringModeButton != null)
            {
                _ctx.MainUIView.WateringModeButton.onClick.RemoveAllListeners();
            }

            if (_ctx.MainUIView.InventoryModeButton != null)
            {
                _ctx.MainUIView.InventoryModeButton.onClick.RemoveAllListeners();
            }

            if (_ctx.MainUIView.ShopButton != null)
            {
                _ctx.MainUIView.ShopButton.onClick.RemoveAllListeners();
            }

            if (_ctx.ShopUIView.CloseButton != null)
            {
                _ctx.ShopUIView.CloseButton.onClick.RemoveAllListeners();
            }
        }
    }
}