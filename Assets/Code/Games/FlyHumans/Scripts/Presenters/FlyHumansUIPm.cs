using System;
using System.Threading;
using Disposable;
using GameShorts.FlyHumans.View;
using R3;

namespace GameShorts.FlyHumans.Gameplay
{
    internal class FlyHumansUIPm : DisposableBase
    {
        internal struct Ctx
        {
            public CancellationToken cancellationToken;
            public FlyHumansSceneContextView sceneContextView;
            public Action restartGame;
            public Action startGame;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private IDisposable _startButtonSubscription;

        public FlyHumansUIPm(Ctx ctx)
        {
            _ctx = ctx;
            Initialize();
        }

        private void Initialize()
        {
            // Показываем стартовое окно, скрываем основное
            if (_ctx.sceneContextView.StartUIView != null)
            {
                _ctx.sceneContextView.StartUIView.Show();
            }
            
            if (_ctx.sceneContextView.MainUIView != null)
            {
                _ctx.sceneContextView.MainUIView.Hide();
            }
            
            // Подписываемся на кнопку старта
            if (_ctx.sceneContextView.StartUIView != null && _ctx.sceneContextView.StartUIView.StartButton != null)
            {
                _startButtonSubscription = _ctx.sceneContextView.StartUIView.StartButton.OnClickAsObservable()
                    .Subscribe(_ => OnStartButtonClicked());
                    
                AddDisposable(_startButtonSubscription);
            }
        }

        private void OnStartButtonClicked()
        {
            // Скрываем стартовое окно, показываем основное
            if (_ctx.sceneContextView.StartUIView != null)
            {
                _ctx.sceneContextView.StartUIView.Hide();
            }
            
            if (_ctx.sceneContextView.MainUIView != null)
            {
                _ctx.sceneContextView.MainUIView.Show();
            }
            
            // Запускаем игру
            _ctx.startGame?.Invoke();
        }
    }
}

