using System;
using System.Threading;
using Disposable;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark.Scripts.UI
{
    internal class StartScreenPm : DisposableBase
    {
        internal struct Ctx
        {
            public EscapeFromDarkSceneContextView sceneContextView;
            public Action startGameClicked;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private readonly StartScreenView _templateView;
        private StartScreenView _view;

        public StartScreenPm(Ctx ctx)
        {
            _ctx = ctx;
            _templateView = _ctx.sceneContextView.StartScreenView;

            if (_templateView == null)
            {
                Debug.LogError("StartScreenPm: StartScreenView is null!");
                return;
            }

            if (_templateView.gameObject.activeSelf)
            {
                _templateView.gameObject.SetActive(false);
            }

            Transform templateTransform = _templateView.transform;
            GameObject viewInstance = UnityEngine.Object.Instantiate(
                _templateView.gameObject,
                templateTransform.parent,
                false);

            if (viewInstance == null)
            {
                Debug.LogError("StartScreenPm: Failed to instantiate StartScreenView instance!");
                return;
            }

            viewInstance.transform.SetSiblingIndex(templateTransform.GetSiblingIndex());
            _view = viewInstance.GetComponent<StartScreenView>();

            if (_view == null)
            {
                Debug.LogError("StartScreenPm: Instantiated start screen view has no StartScreenView component!");
                UnityEngine.Object.Destroy(viewInstance);
                return;
            }

            _view.SetCtx(new StartScreenView.Ctx());
            _view.gameObject.SetActive(true);
            CreateView();
        }

        private void CreateView()
        {
            if (_view == null)
            {
                return;
            }

            if (_view.StartButton == null)
            {
                Debug.LogError("StartScreenPm: StartButton is null!");
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

            UnityEngine.Object.Destroy(_view.gameObject);
            _view = null;
        }
    }
}
