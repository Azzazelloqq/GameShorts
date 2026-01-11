using System;
using System.Threading;
using System.Threading.Tasks;
using Disposable;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using GameShorts.FlyHumans.Core;
using GameShorts.FlyHumans.View;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.FlyHumans
{
    public class FlyHumansGame : MonoBehaviourDisposable, IShortGame3D
    {
        [SerializeField]
        private FlyHumansSceneContextView _sceneContextView;

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

        public void Disable()
        {
            gameObject.SetActive(false);
        }

        public void Enable()
        {
            gameObject.SetActive(true);
        }

        public void RestartGame()
        {
            RecreateRoot();
        }

        public void StopGame()
        {
            Disable();
            DisableInput();
        }

        public void EnableInput()
        {
            if (_graphicRaycaster != null)
            {
                _graphicRaycaster.enabled = true;
            }
        }

        public void DisableInput()
        {
            if (_graphicRaycaster != null)
            {
                _graphicRaycaster.enabled = false;
            }
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

        private void OnDestroy()
        {
            Dispose();
        }

        private void RecreateRoot()
        {
            DisposeCore();

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
            var rootCtx = new FlyHumansCorePm.Ctx
            {
                sceneContextView = _sceneContextView,
                cancellationToken = _cancellationTokenSource.Token,
                restartGame = RestartGame,
                isPaused = _isPaused
            };
            _core = new FlyHumansCorePm(rootCtx);
        }
    }
}

