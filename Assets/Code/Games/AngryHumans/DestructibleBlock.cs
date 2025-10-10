using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Games.AngryHumans
{
/// <summary>
/// Разрушаемый блок для построения структур (стены, балки, платформы и т.д.)
/// </summary>
public class DestructibleBlock : MonoBehaviour, IPhysicsActivatable
{
	/// <summary>
	/// Форма зоны кеширования связанных объектов
	/// </summary>
	private enum CacheZoneShape
	{
		Sphere,
		Box
	}

	[Header("Block Settings")]
	[SerializeField]
	[Tooltip("Может ли блок быть уничтожен? Если false - блок несносимый, участвует только в физике")]
	private bool _isDestructible = true;

	[SerializeField]
	[Tooltip("Минимальная скорость столкновения для уничтожения")]
	private float _minHitVelocity = 2f;

	[Header("Physics Activation")]
	[SerializeField]
	[Tooltip("Активировать все связанные объекты структуры")]
	private bool _activateConnectedStructure = true; // Активировать всю связанную структуру

	[Header("Connection Cache Settings")]
	[SerializeField]
	[Tooltip("Форма зоны кеширования")]
	private CacheZoneShape _cacheZoneShape = CacheZoneShape.Box;

	[SerializeField]
	[Tooltip("Радиус сферы или размер бокса для кеширования")]
	private Vector3 _cacheZoneSize = new(2f, 5f, 2f);

	[SerializeField]
	[Tooltip("Смещение зоны кеширования относительно центра объекта")]
	private Vector3 _cacheZoneOffset = Vector3.zero;

	[SerializeField]
	[Tooltip("Список прекешированных связанных объектов")]
	private List<GameObject> _cachedConnectedObjects = new();

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
	private bool _physicsActivated = false; // Флаг активации физики после первого удара

	// Цвета для блоков
	private static readonly Color DestructibleColor = new(1f, 0.65f, 0.2f); // Яркий оранжевый/янтарный
	private static readonly Color IndestructibleColor = new(0.15f, 0.25f, 0.35f); // Тёмно-синий/стальной

	// Буфер для NonAlloc версии Physics.OverlapSphere (экземплярный для thread-safety)
	private Collider[] _nearbyCollidersBuffer;

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_meshRenderer ??= GetComponentInChildren<MeshRenderer>();

		// Инициализируем буфер для поиска коллайдеров
		_nearbyCollidersBuffer = new Collider[100];

		ApplyBlockColor();

		// Замораживаем блок до первого попадания
		if (_rigidbody != null)
		{
			_rigidbody.isKinematic = true;
		}
	}

	#if UNITY_EDITOR
	private void OnValidate()
	{
		// Автоматически кешируем при изменении параметров в редакторе
		if (!Application.isPlaying)
		{
			UnityEditor.EditorApplication.delayCall += () =>
			{
				if (this != null)
				{
					UpdateConnectedObjectsCache();
				}
			};
		}
	}
	#endif

	/// <summary>
	/// Вычисляет общий Bounds всех коллайдеров объекта и его детей
	/// </summary>
	private Bounds GetCompositeBounds()
	{
		var colliders = GetComponentsInChildren<Collider>();
		if (colliders.Length == 0)
		{
			return new Bounds(transform.position, Vector3.one);
		}

		var bounds = colliders[0].bounds;
		for (var i = 1; i < colliders.Length; i++)
		{
			bounds.Encapsulate(colliders[i].bounds);
		}

		return bounds;
	}

	/// <summary>
	/// Обновляет кеш связанных объектов (вызывается из редактора)
	/// </summary>
	[ContextMenu("Update Connected Objects Cache")]
	public void UpdateConnectedObjectsCache()
	{
		_cachedConnectedObjects.Clear();

		#if UNITY_EDITOR
		// Проверяем, находимся ли мы в режиме префаба
		var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
		var isPrefabMode = prefabStage != null && prefabStage.IsPartOfPrefabContents(gameObject);

		if (!isPrefabMode)
		{
			// Проверяем, является ли это префабом в Project view
			isPrefabMode = UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);
		}

		Vector3 center;
		Vector3 scaledOffset;
		Collider[] allColliders;
		if (isPrefabMode)
		{
			Debug.Log($"[{gameObject.name}] Prefab mode detected - using collider-based search");

			// В режиме префаба используем поиск по коллайдерам
			var root = transform.root;
			allColliders = root.GetComponentsInChildren<Collider>();

			// Используем правильный центр с учетом масштаба и смещения
			scaledOffset = Vector3.Scale(_cacheZoneOffset, transform.lossyScale);
			center = transform.position + transform.rotation * scaledOffset;

			// Небольшой допуск для касающихся объектов (0.5 единицы)
			const float touchTolerance = 0.5f;

			var processedRoots = new HashSet<GameObject>();

			foreach (var collider in allColliders)
			{
				// Пропускаем коллайдеры этого объекта и его дочерних
				if (collider.transform == transform || collider.transform.IsChildOf(transform))
				{
					continue;
				}

				// Проверяем пересечение Bounds коллайдера с нашей зоной поиска
				var isInZone = false;

				if (_cacheZoneShape == CacheZoneShape.Sphere)
				{
					// Для сферы - используем Bounds коллайдера для проверки пересечения
					var scaledRadius = _cacheZoneSize.x *
										Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
					var colliderBounds = collider.bounds;
					var closestPointOnBounds = colliderBounds.ClosestPoint(center);
					var distance = Vector3.Distance(closestPointOnBounds, center);
					// Добавляем допуск для касающихся объектов
					isInZone = distance <= scaledRadius + touchTolerance;
				}
				else // Box
				{
					// Для бокса - проверяем пересечение Bounds с учётом масштаба
					var scaledSize = Vector3.Scale(_cacheZoneSize, transform.lossyScale);
					var searchBounds = new Bounds(center, scaledSize + Vector3.one * touchTolerance * 2f);
					var colliderBounds = collider.bounds;
					isInZone = searchBounds.Intersects(colliderBounds);
				}

				if (isInZone)
				{
					// Ищем компонент на объекте коллайдера или его родителях
					var current = collider.transform;
					GameObject targetObject = null;

					while (current != null && current != root.parent)
					{
						var block = current.GetComponent<DestructibleBlock>();
						var target = current.GetComponent<Target>();
						var activatable = current.GetComponent<IPhysicsActivatable>();

						if (block != null || target != null || activatable != null)
						{
							targetObject = current.gameObject;
							break;
						}

						current = current.parent;
					}

					if (targetObject != null && !processedRoots.Contains(targetObject))
					{
						_cachedConnectedObjects.Add(targetObject);
						processedRoots.Add(targetObject);
					}
				}
			}

			Debug.Log($"[{gameObject.name}] Prefab cache updated: {_cachedConnectedObjects.Count} objects cached");
			UnityEditor.EditorUtility.SetDirty(this);

			// НЕ сохраняем автоматически - пусть пользователь сам решит когда сохранять
			// Это избавит от зависаний после обновления кеша
			return;
		}
		#endif

		// Обычный поиск для объектов на сцене
		// Синхронизируем физику перед поиском (важно для редактора)
		Physics.SyncTransforms();

		// Учитываем масштаб при расчете центра и размера зоны
		scaledOffset = Vector3.Scale(_cacheZoneOffset, transform.lossyScale);
		center = transform.position + transform.rotation * scaledOffset;
		Collider[] foundColliders = null;

		// В редакторе используем обычные методы вместо NonAlloc
		#if UNITY_EDITOR
		// Сначала проверим, что вообще есть коллайдеры в сцене
		allColliders = FindObjectsOfType<Collider>();
		Debug.Log($"[{gameObject.name}] Total colliders in scene: {allColliders.Length}");

		// Проверим ближайшие коллайдеры вручную
		var nearbyCount = 0;
		foreach (var col in allColliders)
		{
			if (col.gameObject == gameObject)
			{
				continue;
			}

			var distance = Vector3.Distance(col.transform.position, center);
			if (distance < 10f) // В пределах 10 единиц
			{
				nearbyCount++;
				Debug.Log($"  Nearby collider: {col.gameObject.name} at distance {distance:F2}");
			}
		}

		Debug.Log($"[{gameObject.name}] Found {nearbyCount} colliders within 10 units");

		if (_cacheZoneShape == CacheZoneShape.Sphere)
		{
			// Учитываем максимальный масштаб для радиуса сферы
			var scaledRadius = _cacheZoneSize.x *
								Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
			// Используем единообразный подход к триггерам
			foundColliders = Physics.OverlapSphere(center, scaledRadius, ~0, QueryTriggerInteraction.Ignore);
			Debug.Log(
				$"[{gameObject.name}] Sphere search (Editor): center={center}, radius={scaledRadius}, found={foundColliders.Length} colliders");

			// Если не нашли, пробуем альтернативный способ
			if (foundColliders.Length == 0)
			{
				Debug.LogWarning($"[{gameObject.name}] OverlapSphere found nothing, trying manual search...");
				var manualList = new List<Collider>();
				foreach (var col in allColliders)
				{
					if (col.gameObject == gameObject)
					{
						continue;
					}

					if (Vector3.Distance(col.transform.position, center) <= scaledRadius)
					{
						manualList.Add(col);
						Debug.Log($"  Manual found: {col.gameObject.name}");
					}
				}

				foundColliders = manualList.ToArray();
				Debug.Log($"[{gameObject.name}] Manual search found {foundColliders.Length} colliders");
			}
		}
		else // Box
		{
			// Учитываем масштаб для размера бокса
			var scaledSize = Vector3.Scale(_cacheZoneSize, transform.lossyScale);
			// Используем единообразный подход к триггерам
			foundColliders = Physics.OverlapBox(
				center,
				scaledSize / 2f,
				transform.rotation,
				~0,
				QueryTriggerInteraction.Ignore
			);
			Debug.Log(
				$"[{gameObject.name}] Box search (Editor): center={center}, size={scaledSize}, found={foundColliders.Length} colliders");
		}
		#else
		// В игре используем NonAlloc версии для производительности
		if (_nearbyCollidersBuffer == null)
		{
			_nearbyCollidersBuffer = new Collider[100];
		}
		
		int hitCount = 0;
		if (_cacheZoneShape == CacheZoneShape.Sphere)
		{
			// Учитываем максимальный масштаб для радиуса
			var scaledRadius =
 _cacheZoneSize.x * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
			hitCount =
 Physics.OverlapSphereNonAlloc(center, scaledRadius, _nearbyCollidersBuffer, ~0, QueryTriggerInteraction.Ignore);
		}
		else // Box
		{
			// Учитываем масштаб для размера бокса
			var scaledSize = Vector3.Scale(_cacheZoneSize, transform.lossyScale);
			hitCount = Physics.OverlapBoxNonAlloc(
				center,
				scaledSize / 2f,
				_nearbyCollidersBuffer,
				transform.rotation,
				~0,
				QueryTriggerInteraction.Ignore
			);
		}
		
		// Создаём массив из буфера для единообразной обработки
		foundColliders = new Collider[hitCount];
		for (int i = 0; i < hitCount; i++)
		{
			foundColliders[i] = _nearbyCollidersBuffer[i];
		}
		#endif

		// Добавляем найденные объекты в кеш
		var addedCount = 0;
		var addedRoots = new HashSet<GameObject>();

		foreach (var collider in foundColliders)
		{
			if (collider == null)
			{
				continue;
			}

			// Пропускаем коллайдеры, принадлежащие этому же объекту или его дочерним
			if (collider.transform.IsChildOf(transform) || collider.transform == transform)
			{
				Debug.Log($"  Skipping self or child: {collider.gameObject.name}");
				continue;
			}

			// Ищем компонент IPhysicsActivatable на самом коллайдере и всех его родителях
			GameObject targetObject = null;
			var current = collider.transform;

			while (current != null)
			{
				// Проверяем наличие нужных компонентов
				var activatable = current.GetComponent<IPhysicsActivatable>();
				var block = current.GetComponent<DestructibleBlock>();
				var target = current.GetComponent<Target>();

				if (activatable != null || block != null || target != null)
				{
					targetObject = current.gameObject;
					Debug.Log($"  Found component on: {current.name} (collider was on: {collider.name})");
					break;
				}

				current = current.parent;
			}

			// Если нашли подходящий объект и ещё не добавляли его
			if (targetObject != null && !addedRoots.Contains(targetObject))
			{
				_cachedConnectedObjects.Add(targetObject);
				addedRoots.Add(targetObject);
				addedCount++;
				Debug.Log($"  Added root object: {targetObject.name} (from collider: {collider.name})");
			}
			else if (targetObject == null)
			{
				Debug.Log(
					$"  Skipped: {collider.gameObject.name} - no IPhysicsActivatable/DestructibleBlock/Target found in hierarchy");
			}
		}

		Debug.Log(
			$"[{gameObject.name}] Cache updated: {addedCount} objects cached out of {foundColliders?.Length ?? 0} found");

		#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
		#endif
	}

	/// <summary>
	/// Визуализация зоны кеширования в редакторе
	/// </summary>
	private void OnDrawGizmosSelected()
	{
		// Рисуем зону кеширования - синий цвет чтобы отличать от зелёных коллайдеров
		Gizmos.color = new Color(0, 0.5f, 1f, 0.4f); // Синий полупрозрачный

		// Учитываем масштаб при отображении
		var scaledOffset = Vector3.Scale(_cacheZoneOffset, transform.lossyScale);
		var center = transform.position + transform.rotation * scaledOffset;

		if (_cacheZoneShape == CacheZoneShape.Sphere)
		{
			var scaledRadius = _cacheZoneSize.x *
								Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
			Gizmos.DrawWireSphere(center, scaledRadius);
			// Добавляем заливку для лучшей видимости
			Gizmos.color = new Color(0, 0.5f, 1f, 0.1f);
			Gizmos.DrawSphere(center, scaledRadius);
		}
		else // Box
		{
			var scaledSize = Vector3.Scale(_cacheZoneSize, transform.lossyScale);
			var oldMatrix = Gizmos.matrix;
			Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, scaledSize);
			// Добавляем заливку для лучшей видимости
			Gizmos.color = new Color(0, 0.5f, 1f, 0.1f);
			Gizmos.DrawCube(Vector3.zero, scaledSize);
			Gizmos.matrix = oldMatrix;
		}

		// Рисуем связи с кешированными объектами - жёлтые линии
		if (_cachedConnectedObjects != null && _cachedConnectedObjects.Count > 0)
		{
			Gizmos.color = new Color(1, 1, 0, 0.8f); // Жёлтый
			foreach (var connected in _cachedConnectedObjects)
			{
				if (connected != null)
				{
					Gizmos.DrawLine(transform.position, connected.transform.position);
					// Добавляем маленькую сферу на конце для видимости
					Gizmos.color = new Color(1, 0.5f, 0, 1f); // Оранжевый
					Gizmos.DrawSphere(connected.transform.position, 0.1f);
					Gizmos.color = new Color(1, 1, 0, 0.8f); // Возвращаем жёлтый
				}
			}
		}
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
		if (_isDestroyed)
		{
			return;
		}

		// Активируем физику при первом попадании
		if (!_physicsActivated && _rigidbody != null)
		{
			_physicsActivated = true;
			_rigidbody.isKinematic = false;

			// Применяем импульс от столкновения
			var contactPoint = collision.GetContact(0);
			var impulse = collision.impulse;
			_rigidbody.AddForceAtPosition(impulse, contactPoint.point, ForceMode.Impulse);

			// Активируем физику у связанной структуры
			ActivateConnectedStructure();
		}

		// Проверяем скорость столкновения для уничтожения
		var impactVelocity = collision.relativeVelocity.magnitude;

		if (impactVelocity < _minHitVelocity)
		{
			return;
		}

		// Несносимые блоки не уничтожаются
		if (!_isDestructible)
		{
			return;
		}

		// Любое попадание с достаточной скоростью уничтожает блок
		DestroyBlock();
	}

	/// <summary>
	/// Активирует физику у всей связанной структуры
	/// </summary>
	private void ActivateConnectedStructure()
	{
		if (!_activateConnectedStructure)
		{
			return;
		}

		// Активируем все прекешированные связанные объекты
		foreach (var connectedObject in _cachedConnectedObjects)
		{
			if (connectedObject == null)
			{
				continue;
			}

			var activatable = connectedObject.GetComponent<IPhysicsActivatable>();
			if (activatable != null && !activatable.IsPhysicsActivated)
			{
				activatable.ActivatePhysics();
			}

			// Также активируем связанные объекты у каждого связанного блока
			var connectedBlock = connectedObject.GetComponent<DestructibleBlock>();
			if (connectedBlock != null)
			{
				connectedBlock.ActivateConnectedObjectsFromCache();
			}
		}
	}

	/// <summary>
	/// Активирует прекешированные связанные объекты (вызывается из других блоков)
	/// </summary>
	public void ActivateConnectedObjectsFromCache()
	{
		// Просто активируем все объекты из кеша
		foreach (var connectedObject in _cachedConnectedObjects)
		{
			if (connectedObject == null)
			{
				continue;
			}

			var activatable = connectedObject.GetComponent<IPhysicsActivatable>();
			if (activatable != null && !activatable.IsPhysicsActivated)
			{
				activatable.ActivatePhysics();
			}
		}
	}


	/// <summary>
	/// Простая активация физики без дополнительных проверок
	/// </summary>
	private void ActivatePhysicsSimple()
	{
		if (_physicsActivated || _rigidbody == null)
		{
			return;
		}

		_physicsActivated = true;
		_rigidbody.isKinematic = false;
	}

	private void DestroyBlock()
	{
		if (_isDestroyed)
		{
			return;
		}

		_isDestroyed = true;

		// Активируем физику у связанной структуры при разрушении
		ActivateConnectedStructure();

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
		_physicsActivated = false;

		ApplyBlockColor();

		if (_rigidbody != null)
		{
			_rigidbody.isKinematic = true; // Замораживаем снова
			_rigidbody.linearVelocity = Vector3.zero;
			_rigidbody.angularVelocity = Vector3.zero;
		}
	}

	/// <summary>
	/// Принудительно активирует физику блока (вызывается извне или при активации соседей)
	/// </summary>
	public void ActivatePhysics()
	{
		if (_physicsActivated || _rigidbody == null)
		{
			return; // Уже активирован - защита от повторной активации
		}

		_physicsActivated = true;
		_rigidbody.isKinematic = false;
	}

	public bool IsDestructible => _isDestructible;
	public bool IsPhysicsActivated => _physicsActivated;
	public bool IsDestroyed => _isDestroyed;
}
}