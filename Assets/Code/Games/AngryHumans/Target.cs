using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Games.AngryHumans
{
internal class Target : MonoBehaviour, IPhysicsActivatable
{
	private enum CacheZoneShape
	{
		Sphere,
		Box
	}

	[Header("Target Settings")]
	[SerializeField]
	[Tooltip("Score value for destroying this target")]
	private int _scoreValue = 100;

	[SerializeField]
	[Tooltip("Minimum collision velocity for destruction")]
	private float _minHitVelocity = 2f;

	[Header("Physics Activation")]
	[SerializeField]
	[Tooltip("Activate all connected structure objects")]
	private bool _activateConnectedStructure = true;

	[Header("Connection Cache Settings")]
	[SerializeField]
	[Tooltip("Cache zone shape")]
	private CacheZoneShape _cacheZoneShape = CacheZoneShape.Box;

	[SerializeField]
	[Tooltip("Sphere radius or box size for caching")]
	private Vector3 _cacheZoneSize = new(2f, 5f, 2f);

	[SerializeField]
	[Tooltip("Cache zone offset relative to object center")]
	private Vector3 _cacheZoneOffset = Vector3.zero;

	[SerializeField]
	[Tooltip("List of precached connected objects")]
	private List<GameObject> _cachedConnectedObjects = new();

	[Header("Visual Feedback")]
	[SerializeField]
	[Tooltip("Damage effect prefab (optional)")]
	private GameObject _damageEffectPrefab;

	[SerializeField]
	[Tooltip("Destruction effect prefab (optional)")]
	private GameObject _destroyEffectPrefab;

	[SerializeField]
	[Tooltip("Should target disappear on death")]
	private bool _destroyOnDeath = true;

	[SerializeField]
	[Tooltip("Delay before object destruction (seconds)")]
	private float _destroyDelay = 0.5f;

	private bool _isDestroyed = false;
	private Rigidbody _rigidbody;
	private bool _physicsActivated = false;
	private Collider[] _nearbyCollidersBuffer;

	public event Action<Target, int> OnTargetDestroyed;

	public int ScoreValue => _scoreValue;
	public bool IsDestroyed => _isDestroyed;

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_nearbyCollidersBuffer = new Collider[100];

		if (_rigidbody != null)
		{
			_rigidbody.isKinematic = true;
		}
	}


	[ContextMenu("Update Connected Objects Cache")]
	public void UpdateConnectedObjectsCache()
	{
		_cachedConnectedObjects.Clear();

		#if UNITY_EDITOR
		var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
		var isPrefabMode = prefabStage != null && prefabStage.IsPartOfPrefabContents(gameObject);

		if (!isPrefabMode)
		{
			isPrefabMode = UnityEditor.PrefabUtility.IsPartOfPrefabAsset(gameObject);
		}

		Vector3 center;
		Vector3 scaledOffset;
		Collider[] allColliders;

		if (isPrefabMode)
		{
			var root = transform.root;
			allColliders = root.GetComponentsInChildren<Collider>();

			scaledOffset = Vector3.Scale(_cacheZoneOffset, transform.lossyScale);
			center = transform.position + transform.rotation * scaledOffset;

			const float touchTolerance = 0.5f;
			var processedRoots = new HashSet<GameObject>();

			foreach (var collider in allColliders)
			{
				if (collider.transform == transform || collider.transform.IsChildOf(transform))
				{
					continue;
				}

				var isInZone = false;

				if (_cacheZoneShape == CacheZoneShape.Sphere)
				{
					var scaledRadius = _cacheZoneSize.x *
										Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
					var colliderBounds = collider.bounds;
					var closestPointOnBounds = colliderBounds.ClosestPoint(center);
					var distance = Vector3.Distance(closestPointOnBounds, center);
					isInZone = distance <= scaledRadius + touchTolerance;
				}
				else
				{
					var scaledSize = Vector3.Scale(_cacheZoneSize, transform.lossyScale);
					var searchBounds = new Bounds(center, scaledSize + Vector3.one * touchTolerance * 2f);
					var colliderBounds = collider.bounds;
					isInZone = searchBounds.Intersects(colliderBounds);
				}

				if (isInZone)
				{
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

			UnityEditor.EditorUtility.SetDirty(this);
			return;
		}
		#endif

		Physics.SyncTransforms();

		scaledOffset = Vector3.Scale(_cacheZoneOffset, transform.lossyScale);
		center = transform.position + transform.rotation * scaledOffset;
		Collider[] foundColliders = null;

		#if UNITY_EDITOR
		allColliders = FindObjectsOfType<Collider>();

		if (_cacheZoneShape == CacheZoneShape.Sphere)
		{
			var scaledRadius = _cacheZoneSize.x *
								Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
			foundColliders = Physics.OverlapSphere(center, scaledRadius, ~0, QueryTriggerInteraction.Ignore);

			if (foundColliders.Length == 0)
			{
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
					}
				}

				foundColliders = manualList.ToArray();
			}
		}
		else
		{
			var scaledSize = Vector3.Scale(_cacheZoneSize, transform.lossyScale);
			foundColliders = Physics.OverlapBox(
				center,
				scaledSize / 2f,
				transform.rotation,
				~0,
				QueryTriggerInteraction.Ignore
			);
		}
		#else
		if (_nearbyCollidersBuffer == null)
		{
			_nearbyCollidersBuffer = new Collider[100];
		}
		
		int hitCount = 0;
		if (_cacheZoneShape == CacheZoneShape.Sphere)
		{
			var scaledRadius =
 _cacheZoneSize.x * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
			hitCount =
 Physics.OverlapSphereNonAlloc(center, scaledRadius, _nearbyCollidersBuffer, ~0, QueryTriggerInteraction.Ignore);
		}
		else
		{
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
		
		foundColliders = new Collider[hitCount];
		for (int i = 0; i < hitCount; i++)
		{
			foundColliders[i] = _nearbyCollidersBuffer[i];
		}
		#endif

		var addedRoots = new HashSet<GameObject>();

		foreach (var collider in foundColliders)
		{
			if (collider == null)
			{
				continue;
			}

			if (collider.transform.IsChildOf(transform) || collider.transform == transform)
			{
				continue;
			}

			GameObject targetObject = null;
			var current = collider.transform;

			while (current != null)
			{
				var activatable = current.GetComponent<IPhysicsActivatable>();
				var block = current.GetComponent<DestructibleBlock>();
				var target = current.GetComponent<Target>();

				if (activatable != null || block != null || target != null)
				{
					targetObject = current.gameObject;
					break;
				}

				current = current.parent;
			}

			if (targetObject != null && !addedRoots.Contains(targetObject))
			{
				_cachedConnectedObjects.Add(targetObject);
				addedRoots.Add(targetObject);
			}
		}

		#if UNITY_EDITOR
		UnityEditor.EditorUtility.SetDirty(this);
		#endif
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0, 0.5f, 1f, 0.4f);

		var scaledOffset = Vector3.Scale(_cacheZoneOffset, transform.lossyScale);
		var center = transform.position + transform.rotation * scaledOffset;

		if (_cacheZoneShape == CacheZoneShape.Sphere)
		{
			var scaledRadius = _cacheZoneSize.x *
								Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
			Gizmos.DrawWireSphere(center, scaledRadius);
			Gizmos.color = new Color(0, 0.5f, 1f, 0.1f);
			Gizmos.DrawSphere(center, scaledRadius);
		}
		else
		{
			var scaledSize = Vector3.Scale(_cacheZoneSize, transform.lossyScale);
			var oldMatrix = Gizmos.matrix;
			Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
			Gizmos.DrawWireCube(Vector3.zero, scaledSize);
			Gizmos.color = new Color(0, 0.5f, 1f, 0.1f);
			Gizmos.DrawCube(Vector3.zero, scaledSize);
			Gizmos.matrix = oldMatrix;
		}

		if (_cachedConnectedObjects != null && _cachedConnectedObjects.Count > 0)
		{
			Gizmos.color = new Color(1, 1, 0, 0.8f);
			foreach (var connected in _cachedConnectedObjects)
			{
				if (connected != null)
				{
					Gizmos.DrawLine(transform.position, connected.transform.position);
					Gizmos.color = new Color(1, 0.5f, 0, 1f);
					Gizmos.DrawSphere(connected.transform.position, 0.1f);
					Gizmos.color = new Color(1, 1, 0, 0.8f);
				}
			}
		}
	}

	private void OnCollisionEnter(Collision collision)
	{
		if (_isDestroyed)
		{
			return;
		}

		if (!_physicsActivated && _rigidbody != null)
		{
			_physicsActivated = true;
			_rigidbody.isKinematic = false;

			var contactPoint = collision.GetContact(0);
			var impulse = collision.impulse;
			_rigidbody.AddForceAtPosition(impulse, contactPoint.point, ForceMode.Impulse);

			ActivateConnectedStructure();
		}

		var impactVelocity = collision.relativeVelocity.magnitude;

		if (impactVelocity < _minHitVelocity)
		{
			return;
		}

		DestroyTarget(collision.GetContact(0).point);
	}

	public void ActivatePhysics()
	{
		if (_physicsActivated || _rigidbody == null)
		{
			return;
		}

		_physicsActivated = true;
		_rigidbody.isKinematic = false;
	}

	private void ActivateConnectedStructure()
	{
		if (!_activateConnectedStructure)
		{
			return;
		}

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

			var connectedTarget = connectedObject.GetComponent<Target>();
			if (connectedTarget != null)
			{
				connectedTarget.ActivateConnectedObjectsFromCache();
			}

			var connectedBlock = connectedObject.GetComponent<DestructibleBlock>();
			if (connectedBlock != null)
			{
				connectedBlock.ActivateConnectedObjectsFromCache();
			}
		}
	}

	public void ActivateConnectedObjectsFromCache()
	{
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

	private void DestroyTarget(Vector3 hitPoint)
	{
		if (_isDestroyed)
		{
			return;
		}

		_isDestroyed = true;
		ActivateConnectedStructure();
		OnTargetDestroyed?.Invoke(this, _scoreValue);

		if (_damageEffectPrefab != null)
		{
			Instantiate(_damageEffectPrefab, hitPoint, Quaternion.identity);
		}

		if (_destroyEffectPrefab != null)
		{
			Instantiate(_destroyEffectPrefab, transform.position, Quaternion.identity);
		}

		if (_destroyOnDeath)
		{
			Destroy(gameObject, _destroyDelay);
		}
		else
		{
			var colliders = GetComponentsInChildren<Collider>();
			foreach (var col in colliders)
			{
				col.enabled = false;
			}

			if (_rigidbody != null)
			{
				_rigidbody.isKinematic = true;
			}
		}
	}

	public void Reset()
	{
		_isDestroyed = false;
		_physicsActivated = false;

		var colliders = GetComponentsInChildren<Collider>();
		foreach (var col in colliders)
		{
			col.enabled = true;
		}

		if (_rigidbody != null)
		{
			_rigidbody.isKinematic = true;
			_rigidbody.linearVelocity = Vector3.zero;
			_rigidbody.angularVelocity = Vector3.zero;
		}
	}

	public bool IsPhysicsActivated => _physicsActivated;
}
}