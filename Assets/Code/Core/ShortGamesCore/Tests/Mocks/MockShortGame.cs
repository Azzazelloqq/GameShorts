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
        
        void IShortGame.Start()
        {
            IsStarted = true;
            IsPaused = false;
            StartCallCount++;
        }
        
        public void Pause()
        {
            IsPaused = true;
            PauseCallCount++;
        }
        
        public void Resume()
        {
            IsPaused = false;
            ResumeCallCount++;
        }
        
        public void Restart()
        {
            RestartCallCount++;
            ((IShortGame)this).Start();
        }
        
        public void Reset()
        {
            IsStarted = false;
            IsPaused = false;
            StartCallCount = 0;
            PauseCallCount = 0;
            ResumeCallCount = 0;
            RestartCallCount = 0;
        }
    }
    
    /// <summary>
    /// Мок для тестирования мини-игры с поддержкой пулинга
    /// </summary>
    public class MockPoolableShortGame : MonoBehaviour, IPoolableShortGame
    {
        public int Id { get; set; } = 2;
        
        public bool IsStarted { get; private set; }
        public bool IsPaused { get; private set; }
        public bool IsPooled { get; private set; }
        
        public int OnPooledCallCount { get; private set; }
        public int OnUnpooledCallCount { get; private set; }
        
        void IShortGame.Start()
        {
            IsStarted = true;
            IsPaused = false;
        }
        
        public void Pause()
        {
            IsPaused = true;
        }
        
        public void Resume()
        {
            IsPaused = false;
        }
        
        public void Restart()
        {
            ((IShortGame)this).Start();
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
    }
}
