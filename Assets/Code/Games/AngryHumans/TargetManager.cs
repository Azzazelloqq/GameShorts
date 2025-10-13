using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Threading;
using System.Threading.Tasks;

namespace Code.Games.AngryHumans
{
internal class TargetManager : MonoBehaviour
{
	[Header("Debug Settings")]
	[SerializeField]
	[Tooltip("Show debug information")]
	private bool _showDebugInfo = false;

	private readonly List<TargetStructure> _activeStructures = new();

	private int _currentScore = 0;
	private int _totalTargetsDestroyed = 0;
	private int _totalStructuresCompleted = 0;

	public event Action<TargetStructure, Target, int> OnTargetDestroyed;
	public event Action<TargetStructure, int> OnStructureCompleted;
	public event Action OnAllStructuresCompleted;
	public event Action<int> OnScoreChanged;

	public int CurrentScore => _currentScore;
	public int TotalTargetsDestroyed => _totalTargetsDestroyed;
	public int TotalStructuresCompleted => _totalStructuresCompleted;
	public int ActiveStructuresCount => _activeStructures.Count;
	public int CompletedStructuresCount => _totalStructuresCompleted;

	public int GetCompletedStructuresCount()
	{
		var completed = 0;
		foreach (var structure in _activeStructures)
		{
			if (structure != null && structure.IsCompleted)
			{
				completed++;
			}
		}

		return completed;
	}

	public void RegisterStructure(TargetStructure structure)
	{
		if (structure != null)
		{
			structure.OnTargetDestroyed += HandleTargetDestroyed;
			structure.OnStructureCompleted += HandleStructureCompleted;
			_activeStructures.Add(structure);
		}
	}

	public void ClearAllStructures()
	{
		foreach (var structure in _activeStructures)
		{
			if (structure != null)
			{
				structure.OnTargetDestroyed -= HandleTargetDestroyed;
				structure.OnStructureCompleted -= HandleStructureCompleted;
			}
		}

		_activeStructures.Clear();
	}

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
	}

	private void HandleStructureCompleted(TargetStructure structure, int bonusScore)
	{
		_currentScore += bonusScore;
		_totalStructuresCompleted++;

		OnStructureCompleted?.Invoke(structure, bonusScore);
		OnScoreChanged?.Invoke(_currentScore);

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
		}
	}
}
}