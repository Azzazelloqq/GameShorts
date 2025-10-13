using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Games.AngryHumans
{
/// <summary>
/// Структура с целями - представляет домик/конструкцию с врагами
/// </summary>
public class TargetStructure : MonoBehaviour
{
	[Header("Structure Info")]
	[SerializeField]
	[Tooltip("Название структуры (для отладки)")]
	private string _structureName = "Structure";

	[SerializeField]
	[Tooltip("Бонусные очки за полное уничтожение всех целей в структуре")]
	private int _completionBonusScore = 500;

	[Header("References")]
	[SerializeField]
	[Tooltip("Все цели в этой структуре (заполняется автоматически при инициализации)")]
	private Target[] _targets;

	[SerializeField]
	[Tooltip("Все разрушаемые блоки в структуре (опционально)")]
	private DestructibleBlock[] _destructibleBlocks;

	private readonly HashSet<Target> _aliveTargets = new();
	private bool _isCompleted = false;
	private int _totalTargets = 0;

	/// <summary>
	/// Вызывается когда цель в структуре уничтожена
	/// </summary>
	public event Action<TargetStructure, Target, int> OnTargetDestroyed;

	/// <summary>
	/// Вызывается когда все цели в структуре уничтожены
	/// </summary>
	public event Action<TargetStructure, int> OnStructureCompleted;

	public string StructureName => _structureName;
	public int TotalTargets => _totalTargets;
	public int AliveTargetsCount => _aliveTargets.Count;
	public bool IsCompleted => _isCompleted;
	
	/// <summary>
	/// Устанавливает бонус за завершение структуры
	/// </summary>
	public void SetCompletionBonus(int bonus)
	{
		_completionBonusScore = bonus;
	}

	private void Awake()
	{
		Initialize();
	}

	/// <summary>
	/// Инициализирует структуру - находит все цели и блоки
	/// </summary>
	public void Initialize()
	{
		// Защита от повторной инициализации
		if (_totalTargets > 0 && _aliveTargets.Count > 0)
		{
			Debug.LogWarning($"[{_structureName}] Already initialized, skipping...");
			return;
		}
		
		// Находим все цели в дочерних объектах, если не заданы вручную
		if (_targets == null || _targets.Length == 0)
		{
			_targets = GetComponentsInChildren<Target>();
		}

		// Находим все разрушаемые блоки
		if (_destructibleBlocks == null || _destructibleBlocks.Length == 0)
		{
			_destructibleBlocks = GetComponentsInChildren<DestructibleBlock>();
		}

		// Инициализируем список живых целей
		_aliveTargets.Clear();
		foreach (var target in _targets)
		{
			if (target != null)
			{
				// Отписываемся перед подпиской, чтобы избежать дублирования
				target.OnTargetDestroyed -= HandleTargetDestroyed;
				target.OnTargetDestroyed += HandleTargetDestroyed;
				_aliveTargets.Add(target);
			}
		}

		_totalTargets = _aliveTargets.Count;
		_isCompleted = false;

		Debug.Log($"[{_structureName}] Initialized with {_totalTargets} targets and {_destructibleBlocks.Length} blocks");
	}

	private void OnDestroy()
	{
		// Отписываемся от событий
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

		// Уведомляем о уничтожении цели
		OnTargetDestroyed?.Invoke(this, target, score);

		Debug.Log($"[{_structureName}] Target destroyed! Remaining: {_aliveTargets.Count}/{_totalTargets}");

		// Проверяем, все ли цели уничтожены
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

		Debug.Log($"[{_structureName}] All targets destroyed! Bonus: {_completionBonusScore}");

		// Уведомляем о завершении структуры
		OnStructureCompleted?.Invoke(this, _completionBonusScore);
	}

	/// <summary>
	/// Сбрасывает структуру в исходное состояние
	/// </summary>
	public void Reset()
	{
		_isCompleted = false;
		_aliveTargets.Clear();

		// Сбрасываем все цели
		foreach (var target in _targets)
		{
			if (target != null)
			{
				target.Reset();
				_aliveTargets.Add(target);
			}
		}

		// Сбрасываем все блоки
		foreach (var block in _destructibleBlocks)
		{
			if (block != null)
			{
				block.Reset();
			}
		}
	}

	/// <summary>
	/// Получает процент уничтоженных целей (0-1)
	/// </summary>
	public float GetCompletionPercentage()
	{
		if (_totalTargets == 0)
		{
			return 0f;
		}

		return 1f - ((float)_aliveTargets.Count / _totalTargets);
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

