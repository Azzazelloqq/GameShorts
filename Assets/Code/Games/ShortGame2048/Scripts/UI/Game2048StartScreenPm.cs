using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;

namespace Code.Games
{
    internal class Game2048StartScreenPm : BaseDisposable
    {
        internal struct Ctx
        {
            public Game2048SceneContextView sceneContextView;
            public Action startGameClicked;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private Game2048StartScreenView _view;

        public Game2048StartScreenPm(Ctx ctx)
        {
            _ctx = ctx;
            _view = _ctx.sceneContextView.StartScreenView;
            _view.gameObject.SetActive(true);
            CreateView();
        }
        
        private void OnStartButtonClicked()
        {
            _ctx.startGameClicked?.Invoke();
        }

        private void CreateView()
        {
            _view.StartButton.onClick.AddListener(OnStartButtonClicked);
        }

        protected override void OnDispose()
        { 
            _view.gameObject.SetActive(false);
            _view.StartButton.onClick.RemoveAllListeners();
            base.OnDispose();
        }
    }
}
