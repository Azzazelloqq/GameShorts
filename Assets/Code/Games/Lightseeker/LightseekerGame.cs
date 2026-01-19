using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
using Cysharp.Threading.Tasks;
using Disposable;
using R3;
using UnityEngine;

namespace Lightseeker
{
public class LightseekerGame : MonoBehaviourDisposable, IShortGame3D
    {
        [SerializeField]
        private LightseekerSceneContextView _sceneContextView;

        [SerializeField]
        private Camera _camera;
        [SerializeField]
        private Camera _cameraUI;

        public bool IsPreloaded { get; private set; }

        private IDisposable _core;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;
        private RenderTexture _renderTexture;
        private ReactiveProperty<bool> _isPaused = new ReactiveProperty<bool>();
        private UniTask _preloadTask;
        private bool _isPreloading;
        private bool _startQueued;

        public async UniTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            if (_isDisposed)
            {
                return;
            }

            if (_renderTexture == null)
            {
                _renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera, _cameraUI);
            }

            if (IsPreloaded)
            {
                return;
            }

            if (_isPreloading)
            {
                await _preloadTask;
                return;
            }

            if (_core == null)
            {
                CreateRoot(startPaused: true);
            }

            _isPreloading = true;
            _preloadTask = PreloadInternalAsync(cancellationToken).Preserve();
            try
            {
                await _preloadTask;
                if (_isDisposed)
                {
                    return;
                }

                IsPreloaded = true;
            }
            finally
            {
                _isPreloading = false;
            }
        }

        public RenderTexture GetRenderTexture()
        {
            return _renderTexture;
        }

        public void StartGame()
        {
            if (_core == null)
            {
                CreateRoot(startPaused: false);
                return;
            }

            if (_isPreloading)
            {
                QueueStartAfterPreload();
                return;
            }

            _isPaused.Value = false;
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
        }

        public void DisableInput()
        {
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            DisposeCore();
            RenderTextureUtils.ReleaseAndDestroy(ref _renderTexture, _camera, _cameraUI);
            IsPreloaded = false;

            _isDisposed = true;
            Destroy(gameObject);
        }

        protected override void OnDestroy()
        {
            Dispose();
            base.OnDestroy();
        }

        private void RecreateRoot()
        {
            DisposeCore();
            
            _isDisposed = false;

            CreateRoot(false);
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

        private void CreateRoot(bool startPaused)
        {
            _isPaused.Value = startPaused;
            _cancellationTokenSource = new CancellationTokenSource();
            var rootCtx = new LightseekerCorePm.Ctx
            {
                sceneContextView = _sceneContextView,
                cancellationToken = _cancellationTokenSource.Token,
                restartGame = RestartGame,
                isPaused = _isPaused
            };
            _core = LightseekerCorePmFactory.CreateLightseekerCorePm(rootCtx);
        }

        private async UniTask PreloadInternalAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Allow Unity to process initialization between preload steps.
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
        }

        private void QueueStartAfterPreload()
        {
            if (_startQueued)
            {
                return;
            }

            _startQueued = true;
            StartAfterPreloadAsync().Forget();
        }

        private async UniTaskVoid StartAfterPreloadAsync()
        {
            try
            {
                await _preloadTask;
            }
            catch (Exception)
            {
            }
            finally
            {
                _startQueued = false;
            }

            if (_isDisposed || _core == null)
            {
                return;
            }

            _isPaused.Value = false;
        }
    }
}

