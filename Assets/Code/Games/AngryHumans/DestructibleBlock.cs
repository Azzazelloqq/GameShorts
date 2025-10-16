using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif 
using UnityEngine;
using Random = UnityEngine.Random;

namespace Code.Games.AngryHumans
{
internal class DestructibleBlock : MonoBehaviour, IPhysicsActivatable
{
	private enum CacheZoneShape
	{
		Sphere,
		Box
	}

	[Header("Block Settings")]
	[SerializeField]
	[Tooltip("Can block be destroyed? If false - block is indestructible, participates only in physics")]
	private bool _isDestructible = true;

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

	[Header("Destruction Effects")]
	[SerializeField]
	[Tooltip("Block destruction effect")]
	private GameObject _destructionEffectPrefab;

	[SerializeField]
	[Tooltip("Debris spawned on destruction")]
	private GameObject[] _debrisPrefabs;

	[SerializeField]
	[Tooltip("Number of debris pieces on destruction")]
	private int _debrisCount = 3;

	[SerializeField]
	[Tooltip("Debris explosion force")]
	private float _debrisExplosionForce = 5f;

	[SerializeField]
	private MeshRenderer _meshRenderer;

	private Rigidbody _rigidbody;
	private bool _isDestroyed = false;
	private bool _physicsActivated = false;

	private static readonly Color DestructibleColor = new(1f, 0.65f, 0.2f);
	private static readonly Color IndestructibleColor = new(0.15f, 0.25f, 0.35f);
	private Collider[] _nearbyCollidersBuffer;

	private void Awake()
	{
		_rigidbody = GetComponent<Rigidbody>();
		_meshRenderer ??= GetComponentInChildren<MeshRenderer>();
		_nearbyCollidersBuffer = new Collider[100];

		ApplyBlockColor();

		if (_rigidbody != null)
		{
			_rigidbody.isKinematic = true;
		}
	}

	[ContextMenu("Update Connected Objects Cache")]
	public void UpdateConnectedObjectsCache()
	{
		_cachedConnectedObjects.Clear();

		Vector3 center;
		Vector3 scaledOffset;
		Collider[] allColliders;
		
		#if UNITY_EDITOR
		var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
		var isPrefabMode = prefabStage != null && prefabStage.IsPartOfPrefabContents(gameObject);

		if (!isPrefabMode)
		{
			isPrefabMode = PrefabUtility.IsPartOfPrefabAsset(gameObject);
		}

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

			EditorUtility.SetDirty(this);
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
		EditorUtility.SetDirty(this);
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

		if (!_isDestructible)
		{
			return;
		}

		DestroyBlock();
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

	private void DestroyBlock()
	{
		if (_isDestroyed)
		{
			return;
		}

		_isDestroyed = true;
		ActivateConnectedStructure();

		if (_destructionEffectPrefab != null)
		{
			Instantiate(_destructionEffectPrefab, transform.position, Quaternion.identity);
		}

		SpawnDebris();
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

			var debrisRigidbody = debris.GetComponent<Rigidbody>();
			if (debrisRigidbody != null)
			{
				var explosionDir = Random.insideUnitSphere;
				debrisRigidbody.AddForce(explosionDir * _debrisExplosionForce, ForceMode.Impulse);
				debrisRigidbody.AddTorque(Random.insideUnitSphere * _debrisExplosionForce, ForceMode.Impulse);
			}

			Destroy(debris, 5f);
		}
	}

	public void Reset()
	{
		_isDestroyed = false;
		_physicsActivated = false;

		ApplyBlockColor();

		if (_rigidbody != null)
		{
			_rigidbody.isKinematic = true;
			_rigidbody.linearVelocity = Vector3.zero;
			_rigidbody.angularVelocity = Vector3.zero;
		}
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

	public bool IsDestructible => _isDestructible;
	public bool IsPhysicsActivated => _physicsActivated;
	public bool IsDestroyed => _isDestroyed;
}
}