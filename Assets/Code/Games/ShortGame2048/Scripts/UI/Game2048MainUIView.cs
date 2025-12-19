using Disposable;
using TMPro;
using UnityEngine;

namespace Code.Games
{
    internal class Game2048MainUIView : MonoBehaviourDisposable
    {
        internal struct Ctx
        {
        }

        [SerializeField] 
        private TextMeshProUGUI _currentScoreText;
        
        [SerializeField]
        private TextMeshProUGUI _bestScoreText;

        private Ctx _ctx;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
        }

        public void UpdateCurrentScore(int score)
        {
            if (_currentScoreText != null)
            {
                _currentScoreText.text = score.ToString();
            }
        }
        
        public void UpdateBestScore(int score)
        {
            if (_bestScoreText != null)
            {
                _bestScoreText.text = score.ToString();
            }
        }
    }
}

