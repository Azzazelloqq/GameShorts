using UnityEngine;

namespace Code.Games.AngryHumans
{
/// <summary>
/// Разрушаемый блок для построения структур (стены, балки, платформы и т.д.)
/// </summary>
public class DestructibleBlock : MonoBehaviour
{
	[Header("Block Settings")]
	[SerializeField]
	[Tooltip("Может ли блок быть уничтожен? Если false - блок несносимый, участвует только в физике")]
	private bool _isDestructible = true;

	[SerializeField]
	[Tooltip("Минимальная скорость столкновения для уничтожения")]
	private float _minHitVelocity = 2f;

	[Header("Destruction Effects")]
	[SerializeField]
	[Tooltip("Эффект разрушения блока")]
	private GameObject _destructionEffectPrefab;

	[SerializeField]
	[Tooltip("Осколки, которые появятся при разрушении")]
	private GameObject[] _debrisPrefabs;

	[SerializeField]
	[Tooltip("Количество осколков при разрушении")]
	private int _debrisCount = 3;

	[SerializeField]
	[Tooltip("Сила разброса осколков")]
	private float _debrisExplosionForce = 5f;

	[SerializeField]
	private MeshRenderer _meshRenderer;
	
	private Rigidbody _rigidbody;
	private bool _isDestroyed = false;

	// Цвета для блоков
	private static readonly Color DestructibleColor = new Color(1f, 0.65f, 0.2f);   // Яркий оранжевый/янтарный
	private static readonly Color IndestructibleColor = new Color(0.15f, 0.25f, 0.35f); // Тёмно-синий/стальной

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_meshRenderer ??= GetComponentInChildren<MeshRenderer>();
		
		ApplyBlockColor();
	}

	/// <summary>
	/// Применяет цвет блока в зависимости от типа
	/// </summary>
	private void ApplyBlockColor()
	{
		if (_meshRenderer == null)
		{
			return;
		}

		_meshRenderer.material.color = _isDestructible ? DestructibleColor : IndestructibleColor;
	}

	private void OnCollisionEnter(Collision collision)
	{
		// Несносимые блоки не уничтожаются
		if (!_isDestructible)
		{
			return;
		}

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

		// Любое попадание с достаточной скоростью уничтожает блок
		DestroyBlock();
	}

	private void DestroyBlock()
	{
		if (_isDestroyed)
		{
			return;
		}

		_isDestroyed = true;

		// Создаем эффект разрушения
		if (_destructionEffectPrefab != null)
		{
			Instantiate(_destructionEffectPrefab, transform.position, Quaternion.identity);
		}

		// Создаем осколки
		SpawnDebris();

		// Уничтожаем блок
		Destroy(gameObject);
	}

	private void SpawnDebris()
	{
		if (_debrisPrefabs == null || _debrisPrefabs.Length == 0)
		{
			return;
		}

		for (var i = 0; i < _debrisCount; i++)
		{
			var debrisPrefab = _debrisPrefabs[Random.Range(0, _debrisPrefabs.Length)];
			if (debrisPrefab == null)
			{
				continue;
			}

			var position = transform.position + Random.insideUnitSphere * 0.5f;
			var rotation = Random.rotation;
			var debris = Instantiate(debrisPrefab, position, rotation);

			// Применяем силу взрыва к осколкам
			var debrisRigidbody = debris.GetComponent<Rigidbody>();
			if (debrisRigidbody != null)
			{
				var explosionDir = Random.insideUnitSphere;
				debrisRigidbody.AddForce(explosionDir * _debrisExplosionForce, ForceMode.Impulse);
				debrisRigidbody.AddTorque(Random.insideUnitSphere * _debrisExplosionForce, ForceMode.Impulse);
			}

			// Удаляем осколки через несколько секунд
			Destroy(debris, 5f);
		}
	}

	/// <summary>
	/// Сбрасывает блок в исходное состояние
	/// </summary>
	public void Reset()
	{
		_isDestroyed = false;

		ApplyBlockColor();

		if (_rigidbody != null)
		{
			_rigidbody.linearVelocity = Vector3.zero;
			_rigidbody.angularVelocity = Vector3.zero;
		}
	}

	public bool IsDestructible => _isDestructible;

#if UNITY_EDITOR
	private void OnValidate()
	{
		// Применяем цвет при изменении параметров в редакторе
		if (Application.isPlaying)
		{
			ApplyBlockColor();
		}
	}
#endif
}
}

