using System;
using Code.Core.BaseDMDisposable.Scripts;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Logic.UI
{
    internal class StartScreenView : BaseMonoBehaviour
    {
        internal struct Ctx
        {
            public Action startGameClicked;
        }
        
        [SerializeField]
        private TextMeshProUGUI _titleLabel;

        [SerializeField]
        private TextMeshProUGUI _instructionLabel;

        [SerializeField]
        private Button _tapToStartArea; // Invisible button covering the whole screen
        public TextMeshProUGUI TitleLabel => _titleLabel;
        public TextMeshProUGUI InstructionLabel => _instructionLabel;

        private Ctx _ctx;
        
        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
            _tapToStartArea.onClick.AddListener(StartGameClicked);
        }
        
        protected override void OnDestroy()
        {
            _tapToStartArea.onClick.RemoveListener(StartGameClicked);
            base.OnDestroy();
        }
        
        private void StartGameClicked()
        {
            _ctx.startGameClicked?.Invoke();
        }
    }
}
