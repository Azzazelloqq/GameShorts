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
	[Header("Debug Settings")]
	[SerializeField]
	[Tooltip("Отображать отладочную информацию")]
	private bool _showDebugInfo = false;

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
	
	/// <summary>
	/// Получает количество завершенных структур в текущем уровне
	/// </summary>
	public int GetCompletedStructuresCount()
	{
		int completed = 0;
		foreach (var structure in _activeStructures)
		{
			if (structure != null && structure.IsCompleted)
			{
				completed++;
			}
		}
		return completed;
	}



	/// <summary>
	/// Регистрирует структуру (используется LevelManager)
	/// </summary>
	public void RegisterStructure(TargetStructure structure)
	{
		if (structure != null)
		{
			structure.OnTargetDestroyed += HandleTargetDestroyed;
			structure.OnStructureCompleted += HandleStructureCompleted;
			_activeStructures.Add(structure);
			
			if (_showDebugInfo)
			{
				Debug.Log($"TargetManager: Registered structure {structure.name}");
			}
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
				// Не удаляем GameObject, так как это теперь делает LevelManager
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
}
}

