using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Data;
using GameShorts.Gardener.UI;
using GameShorts.Gardener.Core;
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
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

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
                _ctx.PlaceableItemsPanel.Initialize(_ctx.MainCamera, _ctx.GardenBounds);
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
            if (mode == null)
            {
                // Если режим не активен, скрываем панель
                if (_ctx.PlaceableItemsPanel != null)
                {
                    _ctx.PlaceableItemsPanel.Hide();
                }
                return;
            }
            
            Debug.Log($"Mode changed to: {mode.ModeName}");
            
            // Обновляем UI в зависимости от режима
            UpdateModeUI(mode);
            
            // Обновляем панель placeable элементов
            var placeableItems = mode.GetPlaceableItems();
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
            CloseShop();
        }

        protected override void OnDispose()
        {
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