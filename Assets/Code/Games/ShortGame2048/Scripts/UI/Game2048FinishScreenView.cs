using Disposable;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Code.Games
{
    internal class Game2048FinishScreenView : MonoBehaviourDisposable
    {
        internal struct Ctx
        {
            public System.Action onRestartClicked;
            public int currentScore;
            public int bestScore;
        }

        [SerializeField] 
        private Button _restartButton;
        
        [SerializeField]
        private GameObject _rootPanel;
        
        [SerializeField]
        private TextMeshProUGUI _currentScoreText;
        
        [SerializeField]
        private TextMeshProUGUI _bestScoreText;

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
            
            // Обновляем текст счета
            UpdateScoreDisplay();
        }
        
        private void UpdateScoreDisplay()
        {
            if (_currentScoreText != null)
            {
                _currentScoreText.text = _ctx.currentScore.ToString();
            }
            
            if (_bestScoreText != null)
            {
                _bestScoreText.text = _ctx.bestScore.ToString();
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

