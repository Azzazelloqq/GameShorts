using System;
using UnityEngine;
using Disposable;
using Code.Core.ShortGamesCore.Lawnmower.Scripts.Level;
using R3;

namespace Code.Core.ShortGamesCore.Lawnmower.Scripts.UI
{
    /// <summary>
    /// Presenter для управления основным игровым UI
    /// </summary>
    internal class MainGameUIPm : DisposableBase
    {
        internal struct Ctx
        {
            public MainGameUIView view;
            public LawnmowerLevelManager levelManager;
            public Action onLevelCompleted;
        }

        private readonly Ctx _ctx;
        private float _lastProgressCheck = 0f;
        private const float PROGRESS_CHECK_INTERVAL = 0.1f; // Проверяем прогресс каждые 0.1 секунды

        public MainGameUIPm(Ctx ctx)
        {
            _ctx = ctx;
            
            // Инициализируем View
            _ctx.view.SetCtx(new MainGameUIView.Ctx());
            
            // Обновляем UI с текущим уровнем
            UpdateLevelUI();
            
            Debug.Log("MainGameUIPm initialized");
        }

        /// <summary>
        /// Обновление прогресса уровня (вызывается каждый кадр)
        /// </summary>
        public void UpdateLevelProgress(float deltaTime)
        {
            // Ограничиваем частоту проверки для производительности
            if (Time.time - _lastProgressCheck < PROGRESS_CHECK_INTERVAL) return;
            _lastProgressCheck = Time.time;
            
            LevelView currentLevel = _ctx.levelManager.GetCurrentLevel();
            if (currentLevel == null) return;
            
            // Вычисляем общий прогресс уровня
            float totalProgress = CalculateLevelProgress(currentLevel);
            
            // Обновляем UI
            _ctx.view.UpdateLevelProgress(totalProgress, currentLevel.LevelName);
            
            // Проверяем завершение уровня
            if (totalProgress >= 1f)
            {
                HandleLevelCompleted();
            }
        }

        private float CalculateLevelProgress(LevelView level)
        {
            if (level.GrassFields == null || level.GrassFields.Length == 0) return 0f;
            
            float totalProgress = 0f;
            int validFieldsCount = 0;
            
            foreach (var grassField in level.GrassFields)
            {
                if (grassField != null)
                {
                    totalProgress += grassField.GetCompletionPercentage();
                    validFieldsCount++;
                }
            }
            
            return validFieldsCount > 0 ? totalProgress / validFieldsCount : 0f;
        }

        private void HandleLevelCompleted()
        {
            Debug.Log("Level completed! Transitioning to next level...");
            _ctx.onLevelCompleted?.Invoke();
        }

        private void UpdateLevelUI()
        {
            LevelView currentLevel = _ctx.levelManager.GetCurrentLevel();
            if (currentLevel != null)
            {
                float progress = CalculateLevelProgress(currentLevel);
                _ctx.view.UpdateLevelProgress(progress, currentLevel.LevelName);
            }
        }

        /// <summary>
        /// Установить видимость основного UI
        /// </summary>
        public void SetVisible(bool visible)
        {
            _ctx.view.SetVisible(visible);
        }
    }
}
