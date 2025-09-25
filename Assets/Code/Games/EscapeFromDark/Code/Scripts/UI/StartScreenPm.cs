using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.UI
{
    internal class StartScreenPm : BaseDisposable
    {
        internal struct Ctx
        {
            public EscapeFromDarkSceneContextView sceneContextView;
            public Action startGameClicked;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private StartScreenView _view;

        public StartScreenPm(Ctx ctx)
        {
            _ctx = ctx;
            CreateView();
        }

        private void CreateView()
        {
             _view.StartButton.onClick.AddListener(OnStartButtonClicked);
        }

        private void OnStartButtonClicked()
        {
            Debug.Log("StartScreenPm: Start button clicked");
            _ctx.startGameClicked?.Invoke();
        }

        protected override void OnDispose()
        {
            if (_view != null)
            {
                UnityEngine.Object.Destroy(_view.gameObject);
            }
        }
    }
}
