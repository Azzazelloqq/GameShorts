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

            if (_core == null)
            {
                CreateRoot(startPaused: true);
            }
            IsPreloaded = true;
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
    }
}

