using System.Threading;
using Disposable;
using R3;
using UnityEngine;
using CompositeDisposable = Disposable.CompositeDisposable;

namespace Code.Games
{
    internal class Game2048PausePm : DisposableBase
    {
        internal struct Ctx
        {
            public Game2048SceneContextView sceneContextView;
            public CancellationToken cancellationToken;
            public ReactiveProperty<bool> isPaused;
        }

        private readonly Ctx _ctx;
        private readonly CompositeDisposable _compositeDisposable = new();

        public Game2048PausePm(Ctx ctx)
        {
            _ctx = ctx;
            
            InitializePauseUI();

            AddDisposable(_ctx.isPaused.Subscribe(SetPause));
            AddDisposable(_compositeDisposable);
        }

        private void InitializePauseUI()
        {
            // Скрываем панель паузы изначально
            if (_ctx.sceneContextView.PausePanel != null)
            {
                _ctx.sceneContextView.PausePanel.SetActive(false);
            }
            
            // Подписываемся на кнопку паузы
            if (_ctx.sceneContextView.PauseButton != null)
            {
                _ctx.sceneContextView.PauseButton.onClick.AddListener(TogglePause);
            }
        }

        private void TogglePause()
        {
            _ctx.isPaused.Value = !_ctx.isPaused.Value;
        }

        public void SetPause(bool isPaused)
        {
            // Обновляем UI
            if (_ctx.sceneContextView.PausePanel != null)
            {
                _ctx.sceneContextView.PausePanel.SetActive(_ctx.isPaused.Value);
            }
            
            Debug.Log($"Game2048PausePm: Pause state changed to {_ctx.isPaused.Value}");
        }

        protected override void OnDispose()
        {
            // Отписываемся от кнопки
            if (_ctx.sceneContextView.PauseButton != null)
            {
                _ctx.sceneContextView.PauseButton.onClick.RemoveListener(TogglePause);
            }
            
            base.OnDispose();
        }
    }
}

