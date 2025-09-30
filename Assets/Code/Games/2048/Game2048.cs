using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Games._2048.Scripts.Core;
using Code.Games._2048.Scripts.View;
using Code.Utils;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Games._2048
{
    public class Game2048 : BaseMonoBehaviour, IShortGame3D
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
        }

        public void UnpauseGame()
        {
            throw new NotImplementedException();
        }

        public void ResumeGame()
        {
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
            _cancellationTokenSource = new CancellationTokenSource();
            var rootCtx = new Game2048CorePm.Ctx
            {
                sceneContextView = _sceneContextView,
                cancellationToken = _cancellationTokenSource.Token,
                restartGame = RestartGame
            };
            _core = Game2048CorePmFactory.CreateGame2048CorePm(rootCtx);
        }
    }
}
