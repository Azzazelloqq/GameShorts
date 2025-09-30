using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;
using System.Threading.Tasks;

namespace Code.Games.AngryHumans
{
/// <summary>
/// Управляет целями и структурами в игре
/// </summary>
public class TargetManager : MonoBehaviour
{
	[Serializable]
	public struct StructurePreset
	{
		[SerializeField]
		private string _name;

		[SerializeField]
		[Tooltip("Ссылка на префаб структуры")]
		private AssetReference _structurePrefabReference;

		[SerializeField]
		[Tooltip("Сложность структуры (1-10)")]
		private int _difficulty;

		public string Name => _name;
		public AssetReference StructurePrefabReference => _structurePrefabReference;
		public int Difficulty => _difficulty;

		public StructurePreset(string name, AssetReference structurePrefabReference, int difficulty)
		{
			_name = name;
			_structurePrefabReference = structurePrefabReference;
			_difficulty = difficulty;
		}
	}

	[Header("Structure Presets")]
	[SerializeField]
	[Tooltip("Доступные пресеты структур")]
	private StructurePreset[] _structurePresets;

	[Header("Spawn Settings")]
	[SerializeField]
	[Tooltip("Позиция спавна структур")]
	private Transform _spawnPoint;

	[SerializeField]
	[Tooltip("Расстояние между структурами")]
	private float _structureSpacing = 10f;

	[Header("Game Settings")]
	[SerializeField]
	[Tooltip("Количество структур на уровень")]
	private int _structuresPerLevel = 3;

	private readonly Dictionary<string, GameObject> _loadedPrefabs = new();
	private readonly List<AsyncOperationHandle<GameObject>> _handles = new();
	private readonly List<TargetStructure> _activeStructures = new();
	
	private int _currentScore = 0;
	private int _totalTargetsDestroyed = 0;
	private int _totalStructuresCompleted = 0;

	/// <summary>
	/// Вызывается при уничтожении цели (структура, цель, очки)
	/// </summary>
	public event Action<TargetStructure, Target, int> OnTargetDestroyed;

	/// <summary>
	/// Вызывается при завершении структуры (структура, бонусные очки)
	/// </summary>
	public event Action<TargetStructure, int> OnStructureCompleted;

	/// <summary>
	/// Вызывается при завершении всех структур
	/// </summary>
	public event Action OnAllStructuresCompleted;

	/// <summary>
	/// Вызывается при изменении счета (новый счет)
	/// </summary>
	public event Action<int> OnScoreChanged;

	public int CurrentScore => _currentScore;
	public int TotalTargetsDestroyed => _totalTargetsDestroyed;
	public int TotalStructuresCompleted => _totalStructuresCompleted;
	public int ActiveStructuresCount => _activeStructures.Count;
	public int CompletedStructuresCount => _totalStructuresCompleted;

#if UNITY_EDITOR
	private void OnValidate()
	{
		for (var i = 0; i < _structurePresets.Length; i++)
		{
			var preset = _structurePresets[i];

			if (!string.IsNullOrEmpty(preset.Name))
			{
				continue;
			}

			if (preset.StructurePrefabReference == null)
			{
				continue;
			}

			_structurePresets[i] = new StructurePreset(
				preset.StructurePrefabReference.editorAsset.name,
				preset.StructurePrefabReference,
				preset.Difficulty);
		}
	}
#endif

	/// <summary>
	/// Предзагрузка префабов структур
	/// </summary>
	public async Task PreloadStructuresAsync(CancellationToken cancellationToken = default)
	{
		foreach (var preset in _structurePresets)
		{
			if (cancellationToken.IsCancellationRequested)
			{
				break;
			}

			var handle = Addressables.LoadAssetAsync<GameObject>(preset.StructurePrefabReference);
			_handles.Add(handle);

			var prefab = await handle.Task;

			if (!cancellationToken.IsCancellationRequested && prefab != null)
			{
				_loadedPrefabs[preset.Name] = prefab;
			}
		}

		Debug.Log($"TargetManager: Preloaded {_loadedPrefabs.Count} structure prefabs");
	}

	/// <summary>
	/// Создает случайную структуру
	/// </summary>
	public TargetStructure SpawnRandomStructure(Vector3? position = null)
	{
		if (_loadedPrefabs.Count == 0)
		{
			Debug.LogWarning("TargetManager: No structure prefabs loaded!");
			return null;
		}

		var keys = new List<string>(_loadedPrefabs.Keys);
		var randomKey = keys[UnityEngine.Random.Range(0, keys.Count)];

		return SpawnStructure(randomKey, position);
	}

	/// <summary>
	/// Создает структуру по имени
	/// </summary>
	public TargetStructure SpawnStructure(string presetName, Vector3? position = null)
	{
		if (!_loadedPrefabs.TryGetValue(presetName, out var prefab))
		{
			Debug.LogWarning($"TargetManager: Structure prefab '{presetName}' not found!");
			return null;
		}

		var spawnPosition = position ?? GetNextSpawnPosition();
		var instance = Instantiate(prefab, spawnPosition, Quaternion.identity, transform);
		var structure = instance.GetComponent<TargetStructure>();

		if (structure != null)
		{
			structure.Initialize();
			structure.OnTargetDestroyed += HandleTargetDestroyed;
			structure.OnStructureCompleted += HandleStructureCompleted;
			_activeStructures.Add(structure);

			Debug.Log($"TargetManager: Spawned structure '{presetName}' at {spawnPosition}");
		}
		else
		{
			Debug.LogWarning($"TargetManager: Structure prefab '{presetName}' doesn't have TargetStructure component!");
		}

		return structure;
	}

	/// <summary>
	/// Создает набор структур для уровня
	/// </summary>
	public void SpawnLevel()
	{
		ClearAllStructures();

		for (var i = 0; i < _structuresPerLevel; i++)
		{
			SpawnRandomStructure();
		}
	}

	/// <summary>
	/// Очищает все активные структуры
	/// </summary>
	public void ClearAllStructures()
	{
		foreach (var structure in _activeStructures)
		{
			if (structure != null)
			{
				structure.OnTargetDestroyed -= HandleTargetDestroyed;
				structure.OnStructureCompleted -= HandleStructureCompleted;
				Destroy(structure.gameObject);
			}
		}

		_activeStructures.Clear();
	}

	/// <summary>
	/// Сбрасывает счет
	/// </summary>
	public void ResetScore()
	{
		_currentScore = 0;
		_totalTargetsDestroyed = 0;
		_totalStructuresCompleted = 0;
		OnScoreChanged?.Invoke(_currentScore);
	}

	private void HandleTargetDestroyed(TargetStructure structure, Target target, int score)
	{
		_currentScore += score;
		_totalTargetsDestroyed++;

		OnTargetDestroyed?.Invoke(structure, target, score);
		OnScoreChanged?.Invoke(_currentScore);

		Debug.Log($"TargetManager: Target destroyed! Score: +{score} (Total: {_currentScore})");
	}

	private void HandleStructureCompleted(TargetStructure structure, int bonusScore)
	{
		_currentScore += bonusScore;
		_totalStructuresCompleted++;

		OnStructureCompleted?.Invoke(structure, bonusScore);
		OnScoreChanged?.Invoke(_currentScore);

		Debug.Log($"TargetManager: Structure completed! Bonus: +{bonusScore} (Total: {_currentScore})");

		// Проверяем, все ли структуры завершены
		var allCompleted = true;
		foreach (var activeStructure in _activeStructures)
		{
			if (activeStructure != null && !activeStructure.IsCompleted)
			{
				allCompleted = false;
				break;
			}
		}

		if (allCompleted && _activeStructures.Count > 0)
		{
			OnAllStructuresCompleted?.Invoke();
			Debug.Log("TargetManager: All structures completed!");
		}
	}

	private Vector3 GetNextSpawnPosition()
	{
		if (_spawnPoint != null)
		{
			var offset = _activeStructures.Count * _structureSpacing;
			return _spawnPoint.position + Vector3.right * offset;
		}

		return Vector3.zero + Vector3.right * _activeStructures.Count * _structureSpacing;
	}

	private void OnDestroy()
	{
		// Освобождаем ресурсы Addressables
		foreach (var handle in _handles)
		{
			if (handle.IsValid())
			{
				Addressables.Release(handle);
			}
		}

		_handles.Clear();
		_loadedPrefabs.Clear();
	}

#if UNITY_EDITOR
	[ContextMenu("Spawn Test Level")]
	private void EditorSpawnTestLevel()
	{
		// Для тестирования в редакторе без Addressables
		var structures = _structurePresets;
		if (structures.Length == 0)
		{
			Debug.LogWarning("No structure presets configured!");
			return;
		}

		ClearAllStructures();

		for (var i = 0; i < Mathf.Min(_structuresPerLevel, structures.Length); i++)
		{
			// В редакторе можем использовать прямую ссылку
			Debug.Log($"Would spawn structure: {structures[i].Name}");
		}
	}
#endif
}
}

