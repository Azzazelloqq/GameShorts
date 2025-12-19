using System;
using Disposable;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.UI
{
    internal class FinishScreenView : MonoBehaviourDisposable
    {
        internal struct Ctx
        {
            public Action reloadClicked;
        }
        
        [SerializeField]
        private TextMeshProUGUI _scoreLabel;

        public TextMeshProUGUI ScoreLabel => _scoreLabel;

        [SerializeField]
        private Button _restartButton;
        
        
        private Ctx _ctx;
        
        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            _restartButton.onClick.AddListener(RelodeClicked);
        }
        
        protected override void OnDestroy()
        {
            _restartButton.onClick.RemoveListener(RelodeClicked);
            base.OnDestroy();
        }
        
        private void RelodeClicked()
        {
            _ctx.reloadClicked.Invoke();
        }
    }
}