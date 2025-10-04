using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.FlyHumans.UI
{
    internal class FlyHumansStartUIView : BaseMonoBehaviour
    {
        [SerializeField] 
        private Button _startButton;
        
        [SerializeField]
        private GameObject _rootPanel;

        public Button StartButton => _startButton;

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

