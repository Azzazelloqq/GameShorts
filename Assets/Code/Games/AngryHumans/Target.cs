using System;
using UnityEngine;

namespace Code.Games.AngryHumans
{
/// <summary>
/// Базовый класс цели, которую можно уничтожить попаданием человечка
/// </summary>
public class Target : MonoBehaviour
{
	[Header("Target Settings")]
	[SerializeField]
	[Tooltip("Очки за уничтожение этой цели")]
	private int _scoreValue = 100;

	[SerializeField]
	[Tooltip("Минимальная скорость столкновения для уничтожения")]
	private float _minHitVelocity = 2f;

	[Header("Visual Feedback")]
	[SerializeField]
	[Tooltip("Эффект при получении урона (опционально)")]
	private GameObject _damageEffectPrefab;

	[SerializeField]
	[Tooltip("Эффект при уничтожении (опционально)")]
	private GameObject _destroyEffectPrefab;

	[SerializeField]
	[Tooltip("Должна ли цель исчезнуть при уничтожении")]
	private bool _destroyOnDeath = true;

	[SerializeField]
	[Tooltip("Задержка перед уничтожением объекта (секунды)")]
	private float _destroyDelay = 0.5f;

	private bool _isDestroyed = false;
	private Rigidbody _rigidbody;

	/// <summary>
	/// Вызывается при уничтожении цели. Передает количество очков за уничтожение
	/// </summary>
	public event Action<Target, int> OnTargetDestroyed;

	public int ScoreValue => _scoreValue;
	public bool IsDestroyed => _isDestroyed;

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (_isDestroyed)
		{
			return;
		}

		// Проверяем скорость столкновения
		var impactVelocity = collision.relativeVelocity.magnitude;

		if (impactVelocity < _minHitVelocity)
		{
			return;
		}

		// Любое попадание с достаточной скоростью уничтожает цель
		DestroyTarget(collision.GetContact(0).point);
	}

	/// <summary>
	/// Уничтожает цель
	/// </summary>
	private void DestroyTarget(Vector3 hitPoint)
	{
		if (_isDestroyed)
		{
			return;
		}

		_isDestroyed = true;

		// Вызываем событие уничтожения
		OnTargetDestroyed?.Invoke(this, _scoreValue);

		// Создаем эффект попадания
		if (_damageEffectPrefab != null)
		{
			Instantiate(_damageEffectPrefab, hitPoint, Quaternion.identity);
		}

		// Создаем эффект уничтожения
		if (_destroyEffectPrefab != null)
		{
			Instantiate(_destroyEffectPrefab, transform.position, Quaternion.identity);
		}

		// Уничтожаем объект или деактивируем его
		if (_destroyOnDeath)
		{
			Destroy(gameObject, _destroyDelay);
		}
		else
		{
			// Можно добавить визуальную индикацию уничтожения (изменение цвета, отключение коллайдера и т.д.)
			var colliders = GetComponentsInChildren<Collider>();
			foreach (var col in colliders)
			{
				col.enabled = false;
			}

			// Отключаем физику
			if (_rigidbody != null)
			{
				_rigidbody.isKinematic = true;
			}
		}
	}

	/// <summary>
	/// Сбрасывает цель в исходное состояние
	/// </summary>
	public void Reset()
	{
		_isDestroyed = false;

		// Включаем коллайдеры обратно
		var colliders = GetComponentsInChildren<Collider>();
		foreach (var col in colliders)
		{
			col.enabled = true;
		}

		// Включаем физику
		if (_rigidbody != null)
		{
			_rigidbody.isKinematic = false;
			_rigidbody.linearVelocity = Vector3.zero;
			_rigidbody.angularVelocity = Vector3.zero;
		}
	}
}
}

