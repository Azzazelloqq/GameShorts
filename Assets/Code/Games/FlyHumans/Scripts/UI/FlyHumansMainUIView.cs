using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.FlyHumans.UI
{
    internal class FlyHumansMainUIView : BaseMonoBehaviour
    {
        internal struct Ctx
        {
        }

        [SerializeField] 
        private Button _jumpButton;
        
        [SerializeField]
        private GameObject _rootPanel;

        private Ctx _ctx;

        public Button JumpButton => _jumpButton;

        public void SetCtx(Ctx ctx)
        {
            _ctx = ctx;
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

