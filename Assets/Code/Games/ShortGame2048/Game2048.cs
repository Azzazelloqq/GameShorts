using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using Disposable;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Games
{
    public class Game2048 : MonoBehaviourDisposable, IShortGame3D
    {
        [SerializeField]
        private Game2048SceneContextView _sceneContextView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GraphicRaycaster _graphicRaycaster;
        public bool IsPreloaded { get; private set; }

        private IDisposable _core;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;
        private RenderTexture _renderTexture;
        private ReactiveProperty<bool> _isPaused = new ReactiveProperty<bool>();

        public ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            IsPreloaded = true;

            _renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera);

            return default;
        }

        public RenderTexture GetRenderTexture()
        {
            return _renderTexture;
        }

        public void StartGame()
        {
            CreateRoot();
        }

        public void PauseGame()
        {
            _isPaused.Value = true;
        }

        public void UnpauseGame()
        {
            _isPaused.Value = false;
        }

        public void RestartGame()
        {
            RecreateRoot();
        }

        public void StopGame()
        {
            PauseGame();
            DisableInput();
        }

        public void EnableInput()
        {
            _graphicRaycaster.enabled = true;
        }

        public void DisableInput()
        {
            _graphicRaycaster.enabled = false;
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            DisposeCore();
            RenderTextureUtils.ReleaseAndDestroy(ref _renderTexture, _camera);
            IsPreloaded = false;

            _isDisposed = true;
            Destroy(gameObject);
        }

        private void RecreateRoot()
        {
            DisposeCore();
            
            // Сбрасываем флаг disposed, чтобы можно было снова dispose при необходимости
            _isDisposed = false;

            CreateRoot();
        }

        private void DisposeCore()
        {
            if (_cancellationTokenSource != null)
            {
                if (!_cancellationTokenSource.IsCancellationRequested)
                {
                    _cancellationTokenSource.Cancel();
                }

                _cancellationTokenSource.Dispose();

                _cancellationTokenSource = null;
            }

            _core?.Dispose();
            _core = null;
        }

        private void CreateRoot()
        {
            _isPaused.Value = false;
            _cancellationTokenSource = new CancellationTokenSource();
            var rootCtx = new Game2048CorePm.Ctx
            {
                sceneContextView = _sceneContextView,
                cancellationToken = _cancellationTokenSource.Token,
                restartGame = RestartGame,
                isPaused = _isPaused
            };
            _core = Game2048CorePmFactory.CreateGame2048CorePm(rootCtx);
        }
    }
}
