using System;
using System.Threading;
using Disposable;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.View;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.Level
{
    internal class LawnmowerLevelManager : DisposableBase
    {
        public struct Ctx
        {
            public LawnmowerSceneContextView sceneContextView;
            public CancellationToken cancellationToken;
        }

        private readonly Ctx _ctx;
        private LevelView _currentLevel;
        private int _currentLevelIndex;
        
        // Events
        public Action<LevelView> OnLevelStarted;
        public Action<LevelView> OnLevelCompleted;
        public Action OnAllLevelsCompleted;

        public LawnmowerLevelManager(Ctx ctx)
        {
            _ctx = ctx;
            _currentLevelIndex = _ctx.sceneContextView.CurrentLevelIndex;
            _currentLevel = _ctx.sceneContextView.CurrentLevel;
        }

        public void StartCurrentLevel()
        {
            if (_currentLevel == null) return;

            _currentLevel.OnLevelCompleted += OnLevelCompletedHandler;
            _currentLevel.StartLevel();
            OnLevelStarted?.Invoke(_currentLevel);
            
            UnityEngine.Debug.Log($"Started level: {_currentLevel.LevelName}");
        }

        public void StopCurrentLevel()
        {
            if (_currentLevel == null) return;

            _currentLevel.OnLevelCompleted -= OnLevelCompletedHandler;
            _currentLevel.StopLevel();
        }

        public bool HasNextLevel()
        {
            return _currentLevelIndex + 1 < _ctx.sceneContextView.Levels.Length;
        }

        public void NextLevel()
        {
            if (!HasNextLevel()) return;

            StopCurrentLevel();
            
            _currentLevelIndex++;
            _ctx.sceneContextView.SetCurrentLevel(_currentLevelIndex);
            _currentLevel = _ctx.sceneContextView.CurrentLevel;
            
            UnityEngine.Debug.Log($"Switched to level: {_currentLevel.LevelName}");
        }

        public LevelView GetCurrentLevel()
        {
            return _currentLevel;
        }

        public int GetCurrentLevelIndex()
        {
            return _currentLevelIndex;
        }

        public int GetTotalLevelsCount()
        {
            return _ctx.sceneContextView.Levels.Length;
        }

        public float GetOverallProgress()
        {
            int totalLevels = GetTotalLevelsCount();
            if (totalLevels == 0) return 1f;
            
            float completedLevels = _currentLevelIndex;
            if (_currentLevel != null)
            {
                completedLevels += _currentLevel.CompletionProgress;
            }
            
            return completedLevels / totalLevels;
        }

        private void OnLevelCompletedHandler(LevelView completedLevel)
        {
            UnityEngine.Debug.Log($"Level completed: {completedLevel.LevelName}");
            OnLevelCompleted?.Invoke(completedLevel);
            
            if (!HasNextLevel())
            {
                OnAllLevelsCompleted?.Invoke();
            }
        }

        protected override void OnDispose()
        {
            StopCurrentLevel();
            base.OnDispose();
        }
    }
}
