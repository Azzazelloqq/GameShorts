using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Code.Core.Tester
{
    internal class GameItemButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TextMeshProUGUI buttonText;
        
        public event Action<string> OnButtonClicked;
        
        private string _gameName;
        
        private void Start()
        {
            if (button == null)
                button = GetComponent<Button>();
                
            if (button != null)
            {
                button.onClick.AddListener(HandleButtonClick);
            }
        }
        
        public void Setup(string gameName)
        {
            _gameName = gameName;
            
            if (buttonText != null)
            {
                buttonText.text = gameName;
            }
        }
        
        private void HandleButtonClick()
        {
            OnButtonClicked?.Invoke(_gameName);
        }
        
        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleButtonClick);
            }
        }
    }
}
