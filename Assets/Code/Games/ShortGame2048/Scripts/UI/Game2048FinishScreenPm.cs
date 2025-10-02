using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;

namespace Code.Games
{
    internal class Game2048FinishScreenPm : BaseDisposable
    {
        internal struct Ctx
        {
            public Game2048SceneContextView sceneContextView;
            public Action restartGame;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private Game2048FinishScreenView _view;
        private int _currentScore;
        private int _bestScore;

        public Game2048FinishScreenPm(Ctx ctx)
        {
            _ctx = ctx;
            
            InitializeView();
        }

        private void InitializeView()
        {
            _view = _ctx.sceneContextView.FinishScreenView;
            
            if (_view == null)
            {
                Debug.LogError("Game2048FinishScreenPm: FinishScreenView is null!");
                return;
            }
            
            var viewCtx = new Game2048FinishScreenView.Ctx
            {
                onRestartClicked = OnRestartClicked
            };
            
            _view.SetCtx(viewCtx);
            
            // Скрываем экран по умолчанию
            _view.Hide();
            
            Debug.Log("Game2048FinishScreenPm: Initialized");
        }

        public void ShowFinishScreen(int currentScore, int bestScore)
        {
            _currentScore = currentScore;
            _bestScore = bestScore;
            
            if (_view != null)
            {
                var viewCtx = new Game2048FinishScreenView.Ctx
                {
                    onRestartClicked = OnRestartClicked,
                    currentScore = _currentScore,
                    bestScore = _bestScore
                };
                
                _view.SetCtx(viewCtx);
                _view.Show();
            }
            
            Debug.Log($"Game2048FinishScreenPm: Showing finish screen with score {_currentScore}, best {_bestScore}");
        }

        public void HideFinishScreen()
        {
            if (_view != null)
            {
                _view.Hide();
            }
            
            Debug.Log("Game2048FinishScreenPm: Hiding finish screen");
        }

        private void OnRestartClicked()
        {
            Debug.Log("Game2048FinishScreenPm: Restart clicked, invoking restart action");
            
            // Скрываем экран перед рестартом
            HideFinishScreen();
            
            // Вызываем рестарт игры
            _ctx.restartGame?.Invoke();
        }

        protected override void OnDispose()
        {
            HideFinishScreen();
            base.OnDispose();
        }
    }
}

