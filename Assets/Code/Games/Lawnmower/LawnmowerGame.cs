using System;
using System.Threading;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Core;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.Tools.Pool;
using Code.Core.InputManager;
using LightDI;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Lawnmower
{
    public class LawnmowerGame : BaseMonoBehaviour, IShortGame
    {
        [SerializeField] private LawnmowerSceneContextView sceneContextView;
        private IDisposable _core;
        private CancellationTokenSource _cancellationTokenSource;
        
        private bool _isDisposed;
        public int Id => 3; // Уникальный ID для игры Lawnmower

        public void StartGame()
        {
            CreateRoot();
        }

        public void PauseGame()
        {
            // TODO: Реализовать паузу если нужно
        }

        public void ResumeGame()
        {
            // TODO: Реализовать возобновление если нужно
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
            
            LawnmowerCorePm.Ctx rootCtx = new LawnmowerCorePm.Ctx
            {
                sceneContextView = sceneContextView,
                cancellationToken = _cancellationTokenSource.Token,
                restartGame = RestartGame
            };
            
            _core = new LawnmowerCorePm(rootCtx);
        }
    }
}
