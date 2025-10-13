using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Code.Games.AngryHumans
{
    /// <summary>
    /// Менеджер уровней для управления загрузкой и прогрессом
    /// </summary>
    public class LevelManager : MonoBehaviour
    {
        [Header("Level Configurations")]
        [SerializeField]
        [Tooltip("Список всех доступных уровней")]
        private LevelConfig[] _levelConfigs;
        
        [SerializeField]
        [Tooltip("Индекс текущего уровня")]
        private int _currentLevelIndex = 0;
        
        [Header("References")]
        [SerializeField]
        private TargetManager _targetManager;
        
        [SerializeField]
        private ScoreController _scoreController;
        
        [SerializeField]
        private Transform _environmentRoot;
        
        private LevelConfig _currentLevelConfig;
        private GameObject _currentLevelInstance;
        private readonly List<AsyncOperationHandle<GameObject>> _loadedAssets = new();
        
        /// <summary>
        /// Событие при начале загрузки уровня
        /// </summary>
        public event Action<LevelConfig> OnLevelLoadStarted;
        
        /// <summary>
        /// Событие при завершении загрузки уровня
        /// </summary>
        public event Action<LevelConfig> OnLevelLoaded;
        
        /// <summary>
        /// Событие при завершении уровня
        /// </summary>
        public event Action<LevelConfig, int, int> OnLevelCompleted; // config, score, stars
        
        /// <summary>
        /// Событие при провале уровня
        /// </summary>
        public event Action<LevelConfig> OnLevelFailed;
        
        public LevelConfig CurrentLevelConfig => _currentLevelConfig;
        public int CurrentLevelIndex => _currentLevelIndex;
        public int TotalLevels => _levelConfigs?.Length ?? 0;
        
        private void Awake()
        {
            ValidateLevelConfigs();
        }
        
        /// <summary>
        /// Проверяет наличие конфигураций уровней
        /// </summary>
        private void ValidateLevelConfigs()
        {
            if (_levelConfigs == null || _levelConfigs.Length == 0)
            {
                Debug.LogError("LevelManager: No level configurations found!");
            }
        }
        
        /// <summary>
        /// Загружает уровень по индексу
        /// </summary>
        public async Task LoadLevel(int levelIndex)
        {
            if (levelIndex < 0 || levelIndex >= _levelConfigs.Length)
            {
                Debug.LogError($"LevelManager: Invalid level index {levelIndex}");
                return;
            }
            
            _currentLevelIndex = levelIndex;
            _currentLevelConfig = _levelConfigs[levelIndex];
            
            OnLevelLoadStarted?.Invoke(_currentLevelConfig);
            
            // Очищаем предыдущий уровень
            ClearCurrentLevel();
            
            // Применяем настройки уровня
            ApplyLevelSettings(_currentLevelConfig);
            
            // Загружаем и спавним префаб уровня
            await LoadLevelPrefab();
            
            // Регистрируем все структуры в уровне
            RegisterLevelStructures();
            
            OnLevelLoaded?.Invoke(_currentLevelConfig);
        }
        
        /// <summary>
        /// Загружает текущий уровень
        /// </summary>
        public async Task LoadCurrentLevel()
        {
            await LoadLevel(_currentLevelIndex);
        }
        
        /// <summary>
        /// Загружает следующий уровень
        /// </summary>
        public async Task LoadNextLevel()
        {
            if (_currentLevelIndex < _levelConfigs.Length - 1)
            {
                await LoadLevel(_currentLevelIndex + 1);
            }
            else
            {
                Debug.Log("LevelManager: No more levels! You've completed the game!");
            }
        }
        
        /// <summary>
        /// Перезагружает текущий уровень
        /// </summary>
        public async Task RestartLevel()
        {
            await LoadLevel(_currentLevelIndex);
        }
        
        /// <summary>
        /// Применяет настройки уровня
        /// </summary>
        private void ApplyLevelSettings(LevelConfig config)
        {
            // Устанавливаем количество попыток
            if (_scoreController != null)
            {
                _scoreController.SetMaxAttempts(config.MaxAttempts);
                _scoreController.ResetAttempts();
                _scoreController.ResetScore();
            }
            
            // Устанавливаем цвет фона
            if (Camera.main != null)
            {
                Camera.main.backgroundColor = config.BackgroundColor;
            }
            
            Debug.Log($"LevelManager: Loading level '{config.LevelName}' with {config.MaxAttempts} attempts");
        }
        
        /// <summary>
        /// Загружает префаб уровня
        /// </summary>
        private async Task LoadLevelPrefab()
        {
            if (_currentLevelConfig == null || 
                _currentLevelConfig.LevelPrefabReference == null || 
                !_currentLevelConfig.LevelPrefabReference.RuntimeKeyIsValid())
            {
                Debug.LogWarning("LevelManager: No level prefab configured!");
                return;
            }
            
            var handle = Addressables.LoadAssetAsync<GameObject>(_currentLevelConfig.LevelPrefabReference);
            _loadedAssets.Add(handle);
            
            var prefab = await handle.Task;
            if (prefab != null)
            {
                var parent = _environmentRoot != null ? _environmentRoot : transform;
                _currentLevelInstance = Instantiate(prefab, parent);
                Debug.Log($"LevelManager: Loaded level '{_currentLevelConfig.LevelName}'");
            }
        }
        
        /// <summary>
        /// Регистрирует все структуры в загруженном уровне
        /// </summary>
        private void RegisterLevelStructures()
        {
            if (_currentLevelInstance == null || _targetManager == null)
                return;
            
            // Находим все TargetStructure в уровне
            var structures = _currentLevelInstance.GetComponentsInChildren<TargetStructure>();
            
            foreach (var structure in structures)
            {
                structure.Initialize();
                _targetManager.RegisterStructure(structure);
            }
            
            Debug.Log($"LevelManager: Registered {structures.Length} structures in level");
        }
        
        /// <summary>
        /// Очищает текущий уровень
        /// </summary>
        public void ClearCurrentLevel()
        {
            // Удаляем текущий уровень
            if (_currentLevelInstance != null)
            {
                Destroy(_currentLevelInstance);
                _currentLevelInstance = null;
            }
            
            // Очищаем менеджер целей
            if (_targetManager != null)
            {
                _targetManager.ClearAllStructures();
            }
            
            // Освобождаем загруженные ассеты
            foreach (var handle in _loadedAssets)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _loadedAssets.Clear();
        }
        
        /// <summary>
        /// Обрабатывает завершение уровня
        /// </summary>
        public void CompleteLevel(int score)
        {
            if (_currentLevelConfig == null)
                return;
            
            int stars = _currentLevelConfig.CalculateStars(score);
            bool completed = _currentLevelConfig.IsLevelCompleted(score);
            
            if (completed)
            {
                // Проверяем идеальное прохождение
                bool perfectCompletion = false;
                if (_targetManager != null && _currentLevelInstance != null)
                {
                    var totalStructures = _currentLevelInstance.GetComponentsInChildren<TargetStructure>().Length;
                    perfectCompletion = _targetManager.GetCompletedStructuresCount() == totalStructures;
                }
                
                int reward = _currentLevelConfig.CalculateReward(score, perfectCompletion);
                
                Debug.Log($"Level '{_currentLevelConfig.LevelName}' completed! " +
                         $"Score: {score}, Stars: {stars}, Reward: {reward}");
                
                OnLevelCompleted?.Invoke(_currentLevelConfig, score, stars);
            }
            else
            {
                Debug.Log($"Level '{_currentLevelConfig.LevelName}' failed! Score: {score}");
                OnLevelFailed?.Invoke(_currentLevelConfig);
            }
        }
        
        /// <summary>
        /// Получает конфигурацию уровня по индексу
        /// </summary>
        public LevelConfig GetLevelConfig(int index)
        {
            if (index >= 0 && index < _levelConfigs.Length)
            {
                return _levelConfigs[index];
            }
            return null;
        }
        
        /// <summary>
        /// Проверяет, есть ли следующий уровень
        /// </summary>
        public bool HasNextLevel()
        {
            return _currentLevelIndex < _levelConfigs.Length - 1;
        }
        
        private void OnDestroy()
        {
            ClearCurrentLevel();
        }
        
#if UNITY_EDITOR
        /// <summary>
        /// Тестовая загрузка уровня в редакторе
        /// </summary>
        [ContextMenu("Load First Level")]
        private async void TestLoadFirstLevel()
        {
            await LoadLevel(0);
        }
        
        /// <summary>
        /// Тестовая загрузка следующего уровня
        /// </summary>
        [ContextMenu("Load Next Level")]
        private async void TestLoadNextLevel()
        {
            await LoadNextLevel();
        }
#endif
    }
}
