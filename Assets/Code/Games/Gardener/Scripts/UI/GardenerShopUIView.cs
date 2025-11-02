using System;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Data;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.Gardener.UI
{
    internal class GardenerShopUIView : BaseMonoBehaviour
    {
        [SerializeField] private Button _closeButton;
        [SerializeField] private GameObject _rootPanel;
        [SerializeField] private Transform _itemsContainer;
        [SerializeField] private GameObject _shopItemPrefab;

        public Button CloseButton => _closeButton;

        public void Show()
        {
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(true);
            }
        }

        public void Hide()
        {
            if (_rootPanel != null)
            {
                _rootPanel.SetActive(false);
            }
        }

        public void PopulateShop(PlantSettings[] plantSettings, Action<PlantSettings> onBuyItem)
        {
            // Очищаем контейнер
            foreach (Transform child in _itemsContainer)
            {
                Destroy(child.gameObject);
            }

            // Создаем элементы магазина
            foreach (var plant in plantSettings)
            {
                var itemObject = Instantiate(_shopItemPrefab, _itemsContainer);
                var shopItem = itemObject.GetComponent<ShopItemView>();
                
                if (shopItem != null)
                {
                    shopItem.SetData(plant, () => onBuyItem?.Invoke(plant));
                }
            }
        }
    }
}