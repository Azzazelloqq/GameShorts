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
            
            CreateView();
        }

        private void CreateView()
        {
            // TODO: Load start screen prefab from addressables and setup UI
        }

        protected override void OnDispose()
        {
            _view?.SetCtx(new Game2048StartScreenView.Ctx());
            base.OnDispose();
        }
    }
}
