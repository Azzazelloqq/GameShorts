using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Utils;
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

        public ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            IsPreloaded = true;

            _renderTexture = RenderTextureUtils.GetRenderTextureForShortGame(_camera, _cameraUI);
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

