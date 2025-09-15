using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.ShortGamesCore.Source.GameCore;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Game1
{
    public class Game1 : BaseMonoBehaviour, IShortGame2D
    {
        [SerializeField] private MainSceneContextView sceneContextView;
        private IDisposable _core;
        private CancellationTokenSource _cancellationTokenSource;
        
        public int Id => 1;
        public bool IsPreloaded { get; }
        
        private bool _isDisposed;
        
        public ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            return default;
        }

        public RenderTexture GetRenderTexture()
        {
            return null;
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
        }

        public void RestartGame()
        {
            RecreateRoot();
        }

        public void StopGame()
        {
            Dispose();
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
            CorePm.Ctx rootCtx = new CorePm.Ctx
            {
                sceneContextView = sceneContextView,
                cancellationToken = _cancellationTokenSource.Token,
                restartGame = RestartGame
            };
            _core = CorePmFactory.CreateCorePm(rootCtx);
        }
    }
}