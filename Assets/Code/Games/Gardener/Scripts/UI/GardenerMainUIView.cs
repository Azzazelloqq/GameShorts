using Code.Core.BaseDMDisposable.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.Gardener.UI
{
    internal class GardenerMainUIView : BaseMonoBehaviour
    {
        [Header("Mode Buttons")]
        [SerializeField] private Button _harveyModeButton;
        [SerializeField] private Button _wateringModeButton;
        [SerializeField] private Button _inventoryModeButton;
        
        [Header("Other Buttons")]
        [SerializeField] private Button _shopButton;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI _moneyText;
        [SerializeField] private TextMeshProUGUI _currentModeText;
        [SerializeField] private GameObject _rootPanel;
        
        [Header("Mode Button Colors")]
        [SerializeField] private Color _activeModeColor = Color.green;
        [SerializeField] private Color _inactiveModeColor = Color.white;

        public Button HarveyModeButton => _harveyModeButton;
        public Button WateringModeButton => _wateringModeButton;
        public Button InventoryModeButton => _inventoryModeButton;
        public Button ShopButton => _shopButton;

        public void SetMoney(int amount)
        {
            if (_moneyText != null)
            {
                _moneyText.text = $"${amount.ToString()}";
            }
        }
        
        /// <summary>
        /// Устанавливает активный режим и обновляет отображение
        /// </summary>
        public void SetActiveMode(string modeName)
        {
            if (_currentModeText != null)
            {
                _currentModeText.gameObject.SetActive(!string.IsNullOrEmpty(modeName));
                _currentModeText.text = string.IsNullOrEmpty(modeName) ? "Режим: Нет" : $"Режим: {modeName}";
            }
            
            // Обновляем цвета кнопок
            UpdateModeButtonColors(modeName);
        }
        
        /// <summary>
        /// Сбрасывает активный режим
        /// </summary>
        public void ClearActiveMode()
        {
            if (_currentModeText != null)
            {
                _currentModeText.gameObject.SetActive(false);
            }
            
            // Сбрасываем цвета всех кнопок
            UpdateModeButtonColors(null);
        }
        
        private void UpdateModeButtonColors(string activeMode)
        {
            // Harvey Mode Button
            if (_harveyModeButton != null)
            {
                var colors = _harveyModeButton.colors;
                colors.normalColor = (activeMode == "Harvey") ? _activeModeColor : _inactiveModeColor;
                _harveyModeButton.colors = colors;
            }
            
            // Watering Mode Button
            if (_wateringModeButton != null)
            {
                var colors = _wateringModeButton.colors;
                colors.normalColor = (activeMode == "Watering") ? _activeModeColor : _inactiveModeColor;
                _wateringModeButton.colors = colors;
            }
            
            // Inventory Mode Button
            if (_inventoryModeButton != null)
            {
                var colors = _inventoryModeButton.colors;
                colors.normalColor = (activeMode == "Inventory") ? _activeModeColor : _inactiveModeColor;
                _inventoryModeButton.colors = colors;
            }
        }

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
    }
}