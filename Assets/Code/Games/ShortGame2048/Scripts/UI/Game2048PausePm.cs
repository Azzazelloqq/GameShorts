using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using UnityEngine;
using UnityEngine.UI;
using R3;

namespace Code.Games
{
    internal class Game2048PausePm : BaseDisposable
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

            AddDispose(_ctx.isPaused.Subscribe(SetPause));
            AddDispose(_compositeDisposable);
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

