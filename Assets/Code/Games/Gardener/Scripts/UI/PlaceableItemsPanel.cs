using System;
using System.Collections.Generic;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Gameplay.Modes;
using GameShorts.Gardener.View;
using UnityEngine;

namespace GameShorts.Gardener.UI
{
    /// <summary>
    /// Панель с элементами, которые можно разместить на сцене
    /// Отображается в определенных режимах (например, Harvey Mode)
    /// </summary>
    public class PlaceableItemsPanel : BaseMonoBehaviour
    {
        [SerializeField] private GameObject _rootPanel;
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private PlaceableItemView _itemPrefab;
        [SerializeField] private Canvas _canvas;
        [SerializeField] private Camera _worldCamera;
        
        private GardenBounds _gardenBounds;
        private Action<PlaceableItem, Vector3> _onItemPlaced;
        private readonly List<PlaceableItemPm> _itemPresenters = new List<PlaceableItemPm>();
        
        /// <summary>
        /// Инициализирует панель
        /// </summary>
        public void Initialize(Camera worldCamera, GardenBounds gardenBounds)
        {
            _worldCamera = worldCamera;
            _gardenBounds = gardenBounds;
            _gardenBounds.Init();
            
            if (_canvas == null)
                _canvas = GetComponentInParent<Canvas>();
        }
        
        /// <summary>
        /// Заполняет панель элементами
        /// </summary>
        public void PopulateItems(PlaceableItem[] items, Action<PlaceableItem, Vector3> onItemPlaced)
        {
            _onItemPlaced = onItemPlaced;
            
            // Очищаем старые элементы
            ClearItems();
            
            if (items == null || items.Length == 0)
                return;
            
            // Создаем новые элементы с Presenter'ами (MVP)
            foreach (var item in items)
            {
                var itemView = Instantiate(_itemPrefab, _itemsContainer);
                
                // Создаем Presenter для View
                var itemPm = new PlaceableItemPm(new PlaceableItemPm.Ctx
                {
                    item = item,
                    view = itemView,
                    canvas = _canvas,
                    worldCamera = _worldCamera,
                    gardenBounds = _gardenBounds,
                    onItemPlaced = OnItemPlaced
                });
                
                _itemPresenters.Add(itemPm);
            }
        }
        
        private void OnItemPlaced(PlaceableItem item, Vector3 worldPosition)
        {
            _onItemPlaced?.Invoke(item, worldPosition);
        }
        
        /// <summary>
        /// Очищает все элементы
        /// </summary>
        private void ClearItems()
        {
            // Удаляем Presenter'ы
            foreach (var presenter in _itemPresenters)
            {
                presenter?.Dispose();
            }
            _itemPresenters.Clear();
            
            // Удаляем View объекты
            if (_itemsContainer == null)
                return;
            
            for (int i = _itemsContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(_itemsContainer.GetChild(i).gameObject);
            }
        }
        
        /// <summary>
        /// Показывает панель
        /// </summary>
        public void Show()
        {
            if (_rootPanel != null)
                _rootPanel.SetActive(true);
        }
        
        /// <summary>
        /// Скрывает панель
        /// </summary>
        public void Hide()
        {
            if (_rootPanel != null)
                _rootPanel.SetActive(false);
        }
        
        protected override void OnDestroy()
        {
            ClearItems();
            base.OnDestroy();
        }
    }
}

