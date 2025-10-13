using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Code.Games.AngryHumans
{
internal class LevelManager : MonoBehaviour
{
	[Header("Level Configurations")]
	[SerializeField]
	[Tooltip("List of all available levels")]
	private LevelConfig[] _levelConfigs;

	[SerializeField]
	[Tooltip("Current level index")]
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
	private bool _isLoading = false;

	public event Action<LevelConfig> OnLevelLoadStarted;
	public event Action<LevelConfig> OnLevelLoaded;
	public event Action<LevelConfig, int, int> OnLevelCompleted;
	public event Action<LevelConfig> OnLevelFailed;

	public LevelConfig CurrentLevelConfig => _currentLevelConfig;
	public int CurrentLevelIndex => _currentLevelIndex;
	public int TotalLevels => _levelConfigs?.Length ?? 0;

	private void Awake()
	{
		ValidateLevelConfigs();
	}

	private void ValidateLevelConfigs()
	{
		if (_levelConfigs == null || _levelConfigs.Length == 0)
		{
			Debug.LogError("[LevelManager] No level configurations found!");
			return;
		}

		for (var i = 0; i < _levelConfigs.Length; i++)
		{
			if (_levelConfigs[i] == null)
			{
				Debug.LogError($"[LevelManager] Level config at index {i} is null!");
			}
		}
	}

	public async Task LoadLevel(int levelIndex)
	{
		if (levelIndex < 0 || levelIndex >= _levelConfigs.Length)
		{
			Debug.LogError($"[LevelManager] Invalid level index {levelIndex}");
			return;
		}

		if (_isLoading)
		{
			Debug.LogWarning($"[LevelManager] Already loading a level, skipping request for level {levelIndex}");
			return;
		}

		_isLoading = true;

		try
		{
			_currentLevelIndex = levelIndex;
			_currentLevelConfig = _levelConfigs[levelIndex];

			OnLevelLoadStarted?.Invoke(_currentLevelConfig);
			ClearCurrentLevel();
			ApplyLevelSettings(_currentLevelConfig);
			await LoadLevelPrefab();
			RegisterLevelStructures();
			OnLevelLoaded?.Invoke(_currentLevelConfig);
		}
		finally
		{
			_isLoading = false;
		}
	}

	public async Task LoadCurrentLevel()
	{
		await LoadLevel(_currentLevelIndex);
	}

	public async Task LoadNextLevel()
	{
		if (_currentLevelIndex < _levelConfigs.Length - 1)
		{
			await LoadLevel(_currentLevelIndex + 1);
		}
	}

	public async Task RestartLevel()
	{
		await LoadLevel(_currentLevelIndex);
	}

	private void ApplyLevelSettings(LevelConfig config)
	{
		if (_scoreController != null)
		{
			_scoreController.SetMaxAttempts(config.MaxAttempts);
			_scoreController.ResetAttempts();
			_scoreController.ResetScore();
		}

		if (Camera.main != null)
		{
			Camera.main.backgroundColor = config.BackgroundColor;
		}
	}

	private async Task LoadLevelPrefab()
	{
		if (_currentLevelConfig == null ||
			_currentLevelConfig.LevelPrefabReference == null ||
			!_currentLevelConfig.LevelPrefabReference.RuntimeKeyIsValid())
		{
			Debug.LogWarning("[LevelManager] No level prefab configured!");
			return;
		}

		var handle = Addressables.LoadAssetAsync<GameObject>(_currentLevelConfig.LevelPrefabReference);
		_loadedAssets.Add(handle);

		var prefab = await handle.Task;
		if (prefab != null)
		{
			var parent = _environmentRoot != null ? _environmentRoot : transform;
			_currentLevelInstance = Instantiate(prefab, parent);
		}
	}

	private void RegisterLevelStructures()
	{
		if (_currentLevelInstance == null || _targetManager == null)
		{
			return;
		}

		var structures = _currentLevelInstance.GetComponentsInChildren<TargetStructure>();

		foreach (var structure in structures)
		{
			_targetManager.RegisterStructure(structure);
		}
	}

	public void ClearCurrentLevel()
	{
		if (_currentLevelInstance != null)
		{
			Destroy(_currentLevelInstance);
			_currentLevelInstance = null;
		}

		if (_targetManager != null)
		{
			_targetManager.ClearAllStructures();
		}

		foreach (var handle in _loadedAssets)
		{
			if (handle.IsValid())
			{
				Addressables.Release(handle);
			}
		}

		_loadedAssets.Clear();
	}

	public void CompleteLevel(int score)
	{
		if (_currentLevelConfig == null)
		{
			return;
		}

		var stars = _currentLevelConfig.CalculateStars(score);
		var completed = _currentLevelConfig.IsLevelCompleted(score);

		if (completed)
		{
			var perfectCompletion = false;
			if (_targetManager != null && _currentLevelInstance != null)
			{
				var totalStructures = _currentLevelInstance.GetComponentsInChildren<TargetStructure>().Length;
				perfectCompletion = _targetManager.GetCompletedStructuresCount() == totalStructures;
			}

			var reward = _currentLevelConfig.CalculateReward(score, perfectCompletion);
			OnLevelCompleted?.Invoke(_currentLevelConfig, score, stars);
		}
		else
		{
			OnLevelFailed?.Invoke(_currentLevelConfig);
		}
	}

	public LevelConfig GetLevelConfig(int index)
	{
		if (index >= 0 && index < _levelConfigs.Length)
		{
			return _levelConfigs[index];
		}

		return null;
	}

	public bool HasNextLevel()
	{
		return _currentLevelIndex < _levelConfigs.Length - 1;
	}

	private void OnDestroy()
	{
		ClearCurrentLevel();
	}

	#if UNITY_EDITOR
	[ContextMenu("Load First Level")]
	private async void TestLoadFirstLevel()
	{
		await LoadLevel(0);
	}

	[ContextMenu("Load Next Level")]
	private async void TestLoadNextLevel()
	{
		await LoadNextLevel();
	}
	#endif
}
}