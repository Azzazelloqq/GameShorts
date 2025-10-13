using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Games.AngryHumans
{
internal class TargetStructure : MonoBehaviour
{
	[Header("Structure Info")]
	[SerializeField]
	[Tooltip("Structure name (for debugging)")]
	private string _structureName = "Structure";

	[SerializeField]
	[Tooltip("Bonus score for destroying all targets in structure")]
	private int _completionBonusScore = 500;

	[Header("References")]
	[SerializeField]
	[Tooltip("All targets in this structure (filled automatically on initialization)")]
	private Target[] _targets;

	[SerializeField]
	[Tooltip("All destructible blocks in structure (optional)")]
	private DestructibleBlock[] _destructibleBlocks;

	private readonly HashSet<Target> _aliveTargets = new();
	private bool _isCompleted = false;
	private int _totalTargets = 0;

	public event Action<TargetStructure, Target, int> OnTargetDestroyed;
	public event Action<TargetStructure, int> OnStructureCompleted;

	public string StructureName => _structureName;
	public int TotalTargets => _totalTargets;
	public int AliveTargetsCount => _aliveTargets.Count;
	public bool IsCompleted => _isCompleted;

	public void SetCompletionBonus(int bonus)
	{
		_completionBonusScore = bonus;
	}

	private void Awake()
	{
		Initialize();
	}

	public void Initialize()
	{
		if (_totalTargets > 0 && _aliveTargets.Count > 0)
		{
			return;
		}

		if (_targets == null || _targets.Length == 0)
		{
			_targets = GetComponentsInChildren<Target>();
		}

		if (_destructibleBlocks == null || _destructibleBlocks.Length == 0)
		{
			_destructibleBlocks = GetComponentsInChildren<DestructibleBlock>();
		}

		_aliveTargets.Clear();
		foreach (var target in _targets)
		{
			if (target != null)
			{
				target.OnTargetDestroyed -= HandleTargetDestroyed;
				target.OnTargetDestroyed += HandleTargetDestroyed;
				_aliveTargets.Add(target);
			}
		}

		_totalTargets = _aliveTargets.Count;
		_isCompleted = false;
	}

	private void OnDestroy()
	{
		foreach (var target in _targets)
		{
			if (target != null)
			{
				target.OnTargetDestroyed -= HandleTargetDestroyed;
			}
		}
	}

	private void HandleTargetDestroyed(Target target, int score)
	{
		if (_isCompleted)
		{
			return;
		}

		_aliveTargets.Remove(target);
		OnTargetDestroyed?.Invoke(this, target, score);

		if (_aliveTargets.Count == 0)
		{
			CompleteStructure();
		}
	}

	private void CompleteStructure()
	{
		if (_isCompleted)
		{
			return;
		}

		_isCompleted = true;
		OnStructureCompleted?.Invoke(this, _completionBonusScore);
	}

	public void Reset()
	{
		_isCompleted = false;
		_aliveTargets.Clear();

		foreach (var target in _targets)
		{
			if (target != null)
			{
				target.Reset();
				_aliveTargets.Add(target);
			}
		}

		foreach (var block in _destructibleBlocks)
		{
			if (block != null)
			{
				block.Reset();
			}
		}
	}

	public float GetCompletionPercentage()
	{
		if (_totalTargets == 0)
		{
			return 0f;
		}

		return 1f - (float)_aliveTargets.Count / _totalTargets;
	}

	#if UNITY_EDITOR
	[ContextMenu("Find All Targets")]
	private void EditorFindTargets()
	{
		_targets = GetComponentsInChildren<Target>();
		_destructibleBlocks = GetComponentsInChildren<DestructibleBlock>();
		Debug.Log($"Found {_targets.Length} targets and {_destructibleBlocks.Length} blocks");
	}

	private void OnDrawGizmos()
	{
		// Рисуем границы структуры
		Gizmos.color = _isCompleted ? Color.green : Color.yellow;
		Gizmos.DrawWireCube(transform.position, Vector3.one * 2f);

		// Рисуем связи с целями
		if (_targets != null)
		{
			Gizmos.color = Color.red;
			foreach (var target in _targets)
			{
				if (target != null)
				{
					Gizmos.DrawLine(transform.position, target.transform.position);
				}
			}
		}
	}
	#endif
}
}