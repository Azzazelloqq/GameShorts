using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.Core;
using Code.Core.ShortGamesCore.EscapeFromDark.Scripts.View;
using Code.Core.ShortGamesCore.Source.GameCore;
using LightDI;
using UnityEngine;

namespace Code.Core.ShortGamesCore.EscapeFromDark
{
    public class EscapeFromDarkGame : BaseMonoBehaviour, IShortGame
    {
        [SerializeField] private EscapeFromDarkSceneContextView sceneContextView;
        private IDisposable _core;
        private CancellationTokenSource _cancellationTokenSource;
        
        private bool _isDisposed;
        public int Id => 4; // Уникальный ID для EscapeFromDark

        public void StartGame()
        {
            CreateRoot();
        }

        public void PauseGame()
        {
            // TODO: Реализовать паузу игры
        }

        public void ResumeGame()
        {
            // TODO: Реализовать возобновление игры
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
            EscapeFromDarkCorePm.Ctx rootCtx = new EscapeFromDarkCorePm.Ctx
            {
                sceneContextView = sceneContextView,
                cancellationToken = _cancellationTokenSource.Token,
                restartGame = RestartGame
            };
            _core = new EscapeFromDarkCorePm(rootCtx);
        }
    }
}
