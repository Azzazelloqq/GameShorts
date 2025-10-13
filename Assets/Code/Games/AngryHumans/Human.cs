using System;
using UnityEngine;

namespace Code.Games.AngryHumans
{
internal class Human : MonoBehaviour, IPhysicsActivatable
{
	[SerializeField]
	private Transform[] _grabPoints;

	[SerializeField]
	private Rigidbody _mainRigidbody;

	[SerializeField]
	private Rigidbody[] _ragdollRigidbodies;

	[SerializeField]
	private Animator _animator;

	private bool _isLaunched = false;
	private bool _isOnPlatform = true;
	private bool _ragdollEnabled = false;
	private float _platformY;
	private const float FallThreshold = 50;

	public event Action OnFellBelowPlatform;

	private void Awake()
	{
		if (_mainRigidbody == null)
		{
			_mainRigidbody = GetComponent<Rigidbody>();
		}

		if (_ragdollRigidbodies == null || _ragdollRigidbodies.Length == 0)
		{
			_ragdollRigidbodies = GetComponentsInChildren<Rigidbody>();
		}

		if (_animator == null)
		{
			_animator = GetComponentInChildren<Animator>();
		}

		DisableRagdoll();
	}

	private void Update()
	{
		if (_isLaunched && !_isOnPlatform)
		{
			if (transform.position.y < _platformY - FallThreshold)
			{
				OnFellBelowPlatform?.Invoke();
				_isLaunched = false;
			}
		}
	}

	public void SetOnPlatform(bool onPlatform)
	{
		_isOnPlatform = onPlatform;
		_isLaunched = false;

		if (onPlatform)
		{
			DisableRagdoll();
			EnableAnimation();
			_platformY = transform.position.y;

			if (_mainRigidbody != null)
			{
				_mainRigidbody.isKinematic = true;
			}
		}
	}

	public void EnableRagdoll()
	{
		if (_ragdollEnabled)
		{
			return;
		}

		_ragdollEnabled = true;
		DisableAnimation();

		foreach (var rb in _ragdollRigidbodies)
		{
			rb.isKinematic = false;
		}
	}

	public void DisableRagdoll()
	{
		_ragdollEnabled = false;
		foreach (var rb in _ragdollRigidbodies)
		{
			rb.isKinematic = true;
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
		}

		if (_mainRigidbody != null)
		{
			_mainRigidbody.isKinematic = true;
			_mainRigidbody.linearVelocity = Vector3.zero;
			_mainRigidbody.angularVelocity = Vector3.zero;
		}
	}

	/// <summary>
	/// Returns the nearest grab point to the given world position
	/// </summary>
	public Transform GetNearestGrabPoint(Vector3 worldPosition)
	{
		if (_grabPoints == null || _grabPoints.Length == 0)
		{
			return transform;
		}

		var nearest = _grabPoints[0];
		var minDistance = Vector3.Distance(worldPosition, nearest.position);

		for (var i = 1; i < _grabPoints.Length; i++)
		{
			var distance = Vector3.Distance(worldPosition, _grabPoints[i].position);
			if (distance < minDistance)
			{
				minDistance = distance;
				nearest = _grabPoints[i];
			}
		}

		return nearest;
	}

	public void OnGrabbed(Transform grabPoint)
	{
		_isOnPlatform = false;
		_platformY = transform.position.y;
		EnableRagdoll();
	}

	private void DisableAnimation()
	{
		if (_animator != null)
		{
			_animator.enabled = false;
		}
	}

	private void EnableAnimation()
	{
		if (_animator != null)
		{
			_animator.enabled = true;
		}
	}

	public void Launch(Transform grabPoint, Vector3 launchVelocity)
	{
		if (_isLaunched)
		{
			return;
		}

		_isLaunched = true;
		_isOnPlatform = false;

		if (_mainRigidbody != null)
		{
			_mainRigidbody.isKinematic = false;
		}

		EnableRagdoll();

		var grabRigidbody = grabPoint.GetComponent<Rigidbody>();
		if (grabRigidbody == null)
		{
			grabRigidbody = grabPoint.GetComponentInParent<Rigidbody>();
		}

		if (grabRigidbody != null)
		{
			foreach (var rb in _ragdollRigidbodies)
			{
				if (rb != null && !rb.isKinematic)
				{
					rb.linearVelocity = Vector3.zero;
					rb.angularVelocity = Vector3.zero;
				}
			}

			foreach (var rb in _ragdollRigidbodies)
			{
				if (rb != null && !rb.isKinematic)
				{
					rb.linearVelocity = launchVelocity;
				}
			}
		}
		else if (_mainRigidbody != null)
		{
			_mainRigidbody.linearVelocity = launchVelocity;
		}
		else
		{
			Debug.LogError("[Human] No rigidbody found to launch!");
		}
	}

	public bool IsLaunched => _isLaunched;
	public bool IsOnPlatform => _isOnPlatform;
	public bool IsPhysicsActivated => _isLaunched;

	public void ActivatePhysics()
	{
	}
}
}