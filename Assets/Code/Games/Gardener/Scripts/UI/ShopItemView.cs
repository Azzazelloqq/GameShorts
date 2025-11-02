using System;
using Code.Core.BaseDMDisposable.Scripts;
using GameShorts.Gardener.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.Gardener.UI
{
    internal class ShopItemView : BaseMonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _priceText;
        [SerializeField] private Image _plantImage;
        [SerializeField] private Button _buyButton;

        private Action _onBuyClicked;

        private void Start()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.AddListener(OnBuyClicked);
            }
        }

        protected override void OnDestroy()
        {
            if (_buyButton != null)
            {
                _buyButton.onClick.RemoveListener(OnBuyClicked);
            }
        }

        public void SetData(PlantSettings plantSettings, Action onBuyClicked)
        {
            if (_nameText != null)
            {
                _nameText.text = plantSettings.PlantName;
            }

            if (_priceText != null)
            {
                _priceText.text = plantSettings.SeedPrice.ToString();
            }

            if (_plantImage != null)
            {
                _plantImage.sprite = plantSettings.SeedIcon;
            }

            _onBuyClicked = onBuyClicked;
        }

        private void OnBuyClicked()
        {
            _onBuyClicked?.Invoke();
        }
    }
}