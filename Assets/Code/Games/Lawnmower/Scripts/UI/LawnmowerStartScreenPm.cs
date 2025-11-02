using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.UI
{
    internal class LawnmowerStartScreenPm : BaseDisposable
    {
        internal struct Ctx
        {
            public LawnmowerSceneContextView sceneContextView;
            public Action startGameClicked;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private LawnmowerStartScreenView _view;

        public LawnmowerStartScreenPm(Ctx ctx)
        {
            _ctx = ctx;
            _view = _ctx.sceneContextView.StartScreenView;

            if (_view == null)
            {
                Debug.LogError("LawnmowerStartScreenPm: StartScreenView is null!");
                return;
            }

            _view.SetCtx(new LawnmowerStartScreenView.Ctx());
            _view.gameObject.SetActive(true);
            CreateView();
        }

        private void CreateView()
        {
            if (_view.StartButton == null)
            {
                Debug.LogError("LawnmowerStartScreenPm: StartButton is null!");
                return;
            }

            _view.StartButton.onClick.RemoveListener(OnStartButtonClicked);
            _view.StartButton.onClick.AddListener(OnStartButtonClicked);
        }

        private void OnStartButtonClicked()
        {
            _ctx.startGameClicked?.Invoke();
        }

        protected override void OnDispose()
        {
            if (_view == null)
            {
                return;
            }

            if (_view.StartButton != null)
            {
                _view.StartButton.onClick.RemoveListener(OnStartButtonClicked);
            }

            _view.gameObject.SetActive(false);
            _view = null;
        }
    }
}

