using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using UnityEngine;

namespace Code.Core.ShotGamesCore.Tests.Mocks
{
    /// <summary>
    /// Мок для тестирования базовой мини-игры
    /// </summary>
    public class MockShortGame : MonoBehaviour, IShortGame
    {
        public int Id { get; set; } = 1;
        public bool IsPreloaded { get; private set; }
        
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        public int StartCallCount { get; private set; }
        public int PauseCallCount { get; private set; }
        public int ResumeCallCount { get; private set; }
        public int RestartCallCount { get; private set; }
        public int StopCallCount { get; private set; }
        
        private RenderTexture _renderTexture;
        
        public async ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            IsPreloaded = true;
        }
        
        public RenderTexture GetRenderTexture()
        {
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(256, 256, 16);
            }
            return _renderTexture;
        }
        
        public void Dispose()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                if (Application.isEditor && !Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(_renderTexture);
                else
                    UnityEngine.Object.Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
        
        void IShortGame.StartGame()
        {
            IsStarted = true;
            IsPaused = false;
            StartCallCount++;
        }
        
        public void Disable()
        {
            IsPaused = true;
            PauseCallCount++;
            gameObject.SetActive(false);
        }
        
        public void Enable()
        {
            IsPaused = false;
            ResumeCallCount++;
            gameObject.SetActive(true);
        }
        
        public void RestartGame()
        {
            RestartCallCount++;
            ((IShortGame)this).StartGame();
        }
        
        public void StopGame()
        {
            IsStarted = false;
            IsPaused = false;
            StopCallCount++;
        }

        public void EnableInput()
        {
            
        }

        public void DisableInput()
        {
        }

        public void Reset()
        {
            IsStarted = false;
            IsPaused = false;
            IsPreloaded = false;
            StartCallCount = 0;
            PauseCallCount = 0;
            ResumeCallCount = 0;
            RestartCallCount = 0;
            StopCallCount = 0;
        }
    }
    
    /// <summary>
    /// Мок для тестирования мини-игры с поддержкой пулинга
    /// </summary>
    public class MockPoolableShortGame : MonoBehaviour, IShortGamePoolable
    {
        public int Id { get; set; } = 2;
        public bool IsPreloaded { get; private set; }
        
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsPooled { get; private set; }
        
        public int OnPooledCallCount { get; private set; }
        public int OnUnpooledCallCount { get; private set; }
        
        private RenderTexture _renderTexture;
        
        public async ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            IsPreloaded = true;
        }
        
        public RenderTexture GetRenderTexture()
        {
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(256, 256, 16);
            }
            return _renderTexture;
        }
        
        void IShortGame.StartGame()
        {
            IsStarted = true;
            IsPaused = false;
        }
        
        public void Disable()
        {
            IsPaused = true;
            gameObject.SetActive(false);
        }
        
        public void Enable()
        {
            IsPaused = false;
            gameObject.SetActive(true);
        }
        
        public void RestartGame()
        {
            ((IShortGame)this).StartGame();
        }
        
        public void StopGame()
        {
            IsStarted = false;
            IsPaused = false;
        }

        public void EnableInput()
        {
            
        }

        public void DisableInput()
        {
        }

        public void OnPooled()
        {
            IsPooled = true;
            OnPooledCallCount++;
        }
        
        public void OnUnpooled()
        {
            IsPooled = false;
            OnUnpooledCallCount++;
        }
        
        public void Reset()
        {
            IsStarted = false;
            IsPaused = false;
            IsPooled = false;
            IsPreloaded = false;
            OnPooledCallCount = 0;
            OnUnpooledCallCount = 0;
        }

        public void Dispose()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                if (Application.isEditor && !Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(_renderTexture);
                else
                    UnityEngine.Object.Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
    }
    
    /// <summary>
    /// Мок для тестирования 3D игры
    /// </summary>
    public class MockShortGame3D : MonoBehaviour, IShortGame3D
    {
        public int Id { get; set; } = 3;
        public bool IsPreloaded { get; private set; }
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        
        private RenderTexture _renderTexture;
        
        public async ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            IsPreloaded = true;
        }
        
        public RenderTexture GetRenderTexture()
        {
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(512, 512, 24);
            }
            return _renderTexture;
        }
        
        public void StartGame()
        {
            IsStarted = true;
            IsPaused = false;
        }
        
        public void StopGame()
        {
            IsStarted = false;
            IsPaused = false;
        }

        public void EnableInput()
        {
        }

        public void DisableInput()
        {
        }

        public void Disable()
        {
            IsPaused = true;
            gameObject.SetActive(false);
        }
        
        public void Enable()
        {
            IsPaused = false;
            gameObject.SetActive(true);
        }
        
        public void RestartGame()
        {
            StopGame();
            StartGame();
        }
        
        public void Dispose()
        {
            StopGame();
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                if (Application.isEditor && !Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(_renderTexture);
                else
                    UnityEngine.Object.Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
    }
    
    /// <summary>
    /// Мок для тестирования 2D игры
    /// </summary>
    public class MockShortGame2D : MonoBehaviour, IShortGame2D
    {
        public int Id { get; set; } = 4;
        public bool IsPreloaded { get; private set; }
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        
        private RenderTexture _renderTexture;
        
        public async ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            IsPreloaded = true;
        }
        
        public RenderTexture GetRenderTexture()
        {
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(256, 256, 0);
            }
            return _renderTexture;
        }
        
        public void StartGame()
        {
            IsStarted = true;
            IsPaused = false;
        }
        
        public void StopGame()
        {
            IsStarted = false;
            IsPaused = false;
        }

        public void EnableInput()
        {
            
        }

        public void DisableInput()
        {
        }

        public void Disable()
        {
            IsPaused = true;
            gameObject.SetActive(false);
        }
        
        public void Enable()
        {
            IsPaused = false;
            gameObject.SetActive(true);
        }
        
        public void RestartGame()
        {
            StopGame();
            StartGame();
        }
        
        public void Dispose()
        {
            StopGame();
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                if (Application.isEditor && !Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(_renderTexture);
                else
                    UnityEngine.Object.Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
    }
    
    /// <summary>
    /// Мок для тестирования UI игры
    /// </summary>
    public class MockShortGameUI : MonoBehaviour, IShortGameUI
    {
        public int Id { get; set; } = 5;
        public bool IsPreloaded { get; private set; }
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        
        private RenderTexture _renderTexture;
        
        public async ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            IsPreloaded = true;
        }
        
        public RenderTexture GetRenderTexture()
        {
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(1920, 1080, 0);
            }
            return _renderTexture;
        }
        
        public void StartGame()
        {
            IsStarted = true;
            IsPaused = false;
        }
        
        public void StopGame()
        {
            IsStarted = false;
            IsPaused = false;
        }

        public void EnableInput()
        {
            
        }

        public void DisableInput()
        {
        }

        public void Disable()
        {
            IsPaused = true;
            gameObject.SetActive(false);
        }
        
        public void Enable()
        {
            IsPaused = false;
            gameObject.SetActive(true);
        }
        
        public void RestartGame()
        {
            StopGame();
            StartGame();
        }
        
        public void Dispose()
        {
            StopGame();
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                if (Application.isEditor && !Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(_renderTexture);
                else
                    UnityEngine.Object.Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
    }
    
    /// <summary>
    /// Мок для тестирования poolable 3D игры
    /// </summary>
    public class MockPoolableShortGame3D : MonoBehaviour, IShortGame3D, IShortGamePoolable
    {
        public int Id { get; set; } = 6;
        public bool IsPreloaded { get; private set; }
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsPooled { get; private set; }
        public int OnPooledCallCount { get; private set; }
        public int OnUnpooledCallCount { get; private set; }
        
        private RenderTexture _renderTexture;
        
        public async ValueTask PreloadGameAsync(CancellationToken cancellationToken = default)
        {
            await Task.Delay(10, cancellationToken);
            IsPreloaded = true;
        }
        
        public RenderTexture GetRenderTexture()
        {
            if (_renderTexture == null)
            {
                _renderTexture = new RenderTexture(512, 512, 24);
            }
            return _renderTexture;
        }
        
        public void StartGame()
        {
            IsStarted = true;
            IsPaused = false;
        }
        
        public void StopGame()
        {
            IsStarted = false;
            IsPaused = false;
        }

        public void EnableInput()
        {
            
        }

        public void DisableInput()
        {
        }

        public void Disable()
        {
            IsPaused = true;
            gameObject.SetActive(false);
        }
        
        public void Enable()
        {
            IsPaused = false;
            gameObject.SetActive(true);
        }
        
        public void RestartGame()
        {
            StopGame();
            StartGame();
        }
        
        public void Dispose()
        {
            StopGame();
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                if (Application.isEditor && !Application.isPlaying)
                    UnityEngine.Object.DestroyImmediate(_renderTexture);
                else
                    UnityEngine.Object.Destroy(_renderTexture);
                _renderTexture = null;
            }
        }
        
        public void OnPooled()
        {
            IsPooled = true;
            OnPooledCallCount++;
        }
        
        public void OnUnpooled()
        {
            IsPooled = false;
            OnUnpooledCallCount++;
        }
    }
}
