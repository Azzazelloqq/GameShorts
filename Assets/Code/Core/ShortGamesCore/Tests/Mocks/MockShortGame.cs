using System;
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
        
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        public int StartCallCount { get; private set; }
        public int PauseCallCount { get; private set; }
        public int ResumeCallCount { get; private set; }
        public int RestartCallCount { get; private set; }
        public int StopCallCount { get; private set; }
        
        public void Dispose()
        {
        }
        
        void IShortGame.StartGame()
        {
            IsStarted = true;
            IsPaused = false;
            StartCallCount++;
        }
        
        public void PauseGame()
        {
            IsPaused = true;
            PauseCallCount++;
        }
        
        public void ResumeGame()
        {
            IsPaused = false;
            ResumeCallCount++;
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
        
        public void Reset()
        {
            IsStarted = false;
            IsPaused = false;
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
        
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsPooled { get; private set; }
        
        public int OnPooledCallCount { get; private set; }
        public int OnUnpooledCallCount { get; private set; }
        
        void IShortGame.StartGame()
        {
            IsStarted = true;
            IsPaused = false;
        }
        
        public void PauseGame()
        {
            IsPaused = true;
        }
        
        public void ResumeGame()
        {
            IsPaused = false;
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
            OnPooledCallCount = 0;
            OnUnpooledCallCount = 0;
        }

        public void Dispose()
        {
            
        }
    }
    
    /// <summary>
    /// Мок для тестирования 3D игры
    /// </summary>
    public class MockShortGame3D : MonoBehaviour, IShortGame3D
    {
        public int Id { get; set; } = 3;
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        
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
        
        public void PauseGame()
        {
            IsPaused = true;
        }
        
        public void ResumeGame()
        {
            IsPaused = false;
        }
        
        public void RestartGame()
        {
            StopGame();
            StartGame();
        }
        
        public void Dispose()
        {
            StopGame();
        }
    }
    
    /// <summary>
    /// Мок для тестирования 2D игры
    /// </summary>
    public class MockShortGame2D : MonoBehaviour, IShortGame2D
    {
        public int Id { get; set; } = 4;
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        
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
        
        public void PauseGame()
        {
            IsPaused = true;
        }
        
        public void ResumeGame()
        {
            IsPaused = false;
        }
        
        public void RestartGame()
        {
            StopGame();
            StartGame();
        }
        
        public void Dispose()
        {
            StopGame();
        }
    }
    
    /// <summary>
    /// Мок для тестирования UI игры
    /// </summary>
    public class MockShortGameUI : MonoBehaviour, IShortGameUI
    {
        public int Id { get; set; } = 5;
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        
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
        
        public void PauseGame()
        {
            IsPaused = true;
        }
        
        public void ResumeGame()
        {
            IsPaused = false;
        }
        
        public void RestartGame()
        {
            StopGame();
            StartGame();
        }
        
        public void Dispose()
        {
            StopGame();
        }
    }
    
    /// <summary>
    /// Мок для тестирования poolable 3D игры
    /// </summary>
    public class MockPoolableShortGame3D : MonoBehaviour, IShortGame3D, IShortGamePoolable
    {
        public int Id { get; set; } = 6;
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsPooled { get; private set; }
        public int OnPooledCallCount { get; private set; }
        public int OnUnpooledCallCount { get; private set; }
        
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
        
        public void PauseGame()
        {
            IsPaused = true;
        }
        
        public void ResumeGame()
        {
            IsPaused = false;
        }
        
        public void RestartGame()
        {
            StopGame();
            StartGame();
        }
        
        public void Dispose()
        {
            StopGame();
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
