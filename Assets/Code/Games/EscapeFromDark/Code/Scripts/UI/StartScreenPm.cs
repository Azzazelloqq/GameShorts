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
            _view = _ctx.sceneContextView.StartScreenView;
            _view.SetCtx(new StartScreenView.Ctx());
            CreateView();
        }

        private void CreateView()
        {
             _view.StartButton.onClick.AddListener(OnStartButtonClicked);
        }

        private void OnStartButtonClicked()
        {
            _ctx.startGameClicked?.Invoke();
        }

        protected override void OnDispose()
        {
            _view.StartButton.onClick.RemoveAllListeners();
            if (_view != null)
            {
                UnityEngine.Object.Destroy(_view.gameObject);
            }
        }
    }
}
