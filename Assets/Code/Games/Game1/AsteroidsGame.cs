using System;
using System.Threading;
using Asteroids.Code.Games.Game1.Scripts.View;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Game1.Scripts.Core;
using Code.Core.ShortGamesCore.Game1.Scripts.View;
using Code.Core.ShortGamesCore.Source.GameCore;
using UnityEngine;

namespace Asteroids.Code.Games.Game1
{
    public class AsteroidsGame : BaseMonoBehaviour, IShortGame
    {
        [SerializeField] private MainSceneContextView sceneContextView;
        private IDisposable _core;
        private CancellationTokenSource _cancellationTokenSource;
        
        private bool _isDisposed;
        public int Id => 1;

        public void StartGame()
        {
            CreateRoot();
        }

        public void PauseGame()
        {
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