using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.Tools.Pool;
using Code.Utils;
using GameShorts.CubeRunner.Core;
using GameShorts.CubeRunner.View;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace GameShorts.CubeRunner
{
    public class CubeRunnerGame : BaseMonoBehaviour, IShortGame3D
    {
        [SerializeField]
        private CubeRunnerSceneContextView _sceneContextView;

        [SerializeField]
        private Camera _camera;

        [SerializeField]
        private GraphicRaycaster _graphicRaycaster;

        public bool IsPreloaded { get; private set; }

        private IDisposable _core;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isDisposed;
        private RenderTexture _renderTexture;
        private readonly ReactiveProperty<bool> _isPaused = new ReactiveProperty<bool>();

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
            Dispose();
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
            _isDisposed = true;
        }

        private void RecreateRoot()
        {
            DisposeCore();
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
            _isDisposed = false;
            _isPaused.Value = false;
            _cancellationTokenSource = new CancellationTokenSource();
            var rootCtx = new CubeRunnerCorePm.Ctx
            {
                sceneContextView = _sceneContextView,
                cancellationToken = _cancellationTokenSource.Token,
                restartGame = RestartGame,
                isPaused = _isPaused
            };
            _core = new CubeRunnerCorePm(rootCtx);
        }
    }
}

