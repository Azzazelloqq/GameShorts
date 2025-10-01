using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Code.Games
{
    internal class Game2048FinishScreenView : BaseMonoBehaviour
    {
        internal struct Ctx
        {
            public System.Action onRestartClicked;
        }

        [SerializeField] 
        private Button _restartButton;
        
        [SerializeField]
        private GameObject _rootPanel;

        private Ctx _ctx;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            
            // Подписываемся на кнопку рестарта
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(OnRestartClick);
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

        private void OnRestartClick()
        {
            _ctx.onRestartClicked?.Invoke();
        }

        private void OnDestroy()
        {
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClick);
            }
        }
    }
}

