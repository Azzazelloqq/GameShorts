using UnityEngine;
using System.Collections.Generic;

namespace Code.Games.AngryHumans
{
internal class LaunchController : MonoBehaviour
{
	[Header("Swipe Throw Settings")]
	[SerializeField]
	[Tooltip("Launch force multiplier from swipe velocity")]
	private float _swipeForceMultiplier = 0.05f;

	[SerializeField]
	[Tooltip("Maximum launch force")]
	private float _maxLaunchForce = 30f;

	[SerializeField]
	[Tooltip("Minimum screen swipe speed for launch (pixels/second)")]
	private float _minSwipeSpeed = 100f;

	[SerializeField]
	[Range(0f, 1f)]
	[Tooltip("Balance between height and depth: 0 = pure forward, 1 = pure up")]
	private float _verticalToForwardRatio = 0.5f;

	[SerializeField]
	[Tooltip("Base forward launch force (independent of swipe)")]
	private float _baseForwardForce = 15f;

	[Header("Swipe Tracking")]
	[SerializeField]
	[Tooltip("Number of recent positions for swipe velocity calculation")]
	private int _velocitySamples = 5;

	[SerializeField]
	[Tooltip("Time in seconds for average swipe velocity calculation")]
	private float _velocityTimeWindow = 0.1f;

	[SerializeField]
	private Camera _gameCamera;

	[Header("References")]
	[SerializeField]
	private LaunchPlatform _launchPlatform;

	private Human _currentHuman;
	private Transform _grabbedPoint;
	private Rigidbody _grabbedRigidbody;
	private Vector3 _targetDragPosition;
	private bool _isDragging = false;

	private List<Vector3> _swipePositions = new();
	private List<float> _swipeTimes = new();
	private Vector3 _lastSwipeVelocity;

	private List<Vector2> _screenSwipePositions = new();
	private List<float> _screenSwipeTimes = new();

	[Header("Drag Physics")]
	[SerializeField]
	[Tooltip("Drag attraction force")]
	private float _dragForce = 500f;

	[SerializeField]
	[Tooltip("Drag damping")]
	private float _dragDamping = 10f;

	private void Awake()
	{
		if (_gameCamera == null)
		{
			_gameCamera = Camera.main;
		}
	}

	public void OnPointerDown(Vector2 screenPosition)
	{
		if (_launchPlatform == null)
		{
			return;
		}

		_currentHuman = _launchPlatform.GetCurrentHuman();

		if (_currentHuman == null || _currentHuman.IsLaunched)
		{
			return;
		}

		var worldPosition = GetWorldPosition(screenPosition);
		_grabbedPoint = _currentHuman.GetNearestGrabPoint(worldPosition);

		_grabbedRigidbody = _grabbedPoint.GetComponent<Rigidbody>();
		if (_grabbedRigidbody == null)
		{
			_grabbedRigidbody = _grabbedPoint.GetComponentInParent<Rigidbody>();
		}

		_currentHuman.OnGrabbed(_grabbedPoint);

		_isDragging = true;
		_targetDragPosition = worldPosition;

		_swipePositions.Clear();
		_swipeTimes.Clear();
		_swipePositions.Add(worldPosition);
		_swipeTimes.Add(Time.time);

		_screenSwipePositions.Clear();
		_screenSwipeTimes.Clear();
		_screenSwipePositions.Add(screenPosition);
		_screenSwipeTimes.Add(Time.time);

		_lastSwipeVelocity = Vector3.zero;
	}

	public void OnDrag(Vector2 screenPosition)
	{
		if (!_isDragging || _currentHuman == null || _grabbedPoint == null)
		{
			return;
		}

		var worldPosition = GetWorldPosition(screenPosition);
		var currentTime = Time.time;

		_targetDragPosition = worldPosition;

		_swipePositions.Add(worldPosition);
		_swipeTimes.Add(currentTime);

		while (_swipePositions.Count > _velocitySamples)
		{
			_swipePositions.RemoveAt(0);
			_swipeTimes.RemoveAt(0);
		}

		_screenSwipePositions.Add(screenPosition);
		_screenSwipeTimes.Add(currentTime);

		while (_screenSwipePositions.Count > _velocitySamples)
		{
			_screenSwipePositions.RemoveAt(0);
			_screenSwipeTimes.RemoveAt(0);
		}
	}

	private void FixedUpdate()
	{
		if (!_isDragging || _grabbedRigidbody == null)
		{
			return;
		}

		var currentPosition = _grabbedRigidbody.position;
		var direction = _targetDragPosition - currentPosition;
		var distance = direction.magnitude;

		if (distance > 0.01f)
		{
			var force = direction.normalized * (_dragForce * distance);
			_grabbedRigidbody.AddForce(force, ForceMode.Force);
			_grabbedRigidbody.linearVelocity *= 1f - _dragDamping * Time.fixedDeltaTime;
		}
	}

	public void OnPointerUp(Vector2 screenPosition)
	{
		if (!_isDragging || _currentHuman == null || _grabbedPoint == null)
		{
			return;
		}

		_isDragging = false;

		if (_screenSwipePositions.Count < 2)
		{
			_grabbedPoint = null;
			_grabbedRigidbody = null;
			_currentHuman = null;
			return;
		}

		var startScreenPos = _screenSwipePositions[0];
		var endScreenPos = _screenSwipePositions[^1];
		var screenDelta = endScreenPos - startScreenPos;
		var deltaTime = _screenSwipeTimes[^1] - _screenSwipeTimes[0];
		var screenSpeed = screenDelta.magnitude / Mathf.Max(deltaTime, 0.001f);

		if (screenSpeed < _minSwipeSpeed)
		{
			_grabbedPoint = null;
			_grabbedRigidbody = null;
			_currentHuman = null;
			return;
		}

		var swipeVelocity = CalculateSwipeVelocityFromScreen();

		if (_grabbedRigidbody != null)
		{
			_grabbedRigidbody.linearVelocity = Vector3.zero;
			_grabbedRigidbody.angularVelocity = Vector3.zero;
		}

		var launchVelocity = CalculateLaunchVelocity(swipeVelocity);
		_currentHuman.Launch(_grabbedPoint, launchVelocity);

		_grabbedPoint = null;
		_grabbedRigidbody = null;
		_currentHuman = null;
	}

	private Vector3 CalculateSwipeVelocityFromScreen()
	{
		if (_screenSwipePositions.Count < 2)
		{
			return Vector3.zero;
		}

		var startScreenPos = _screenSwipePositions[0];
		var endScreenPos = _screenSwipePositions[^1];
		var screenDelta = endScreenPos - startScreenPos;

		var startTime = _screenSwipeTimes[0];
		var endTime = _screenSwipeTimes[^1];
		var deltaTime = endTime - startTime;

		if (deltaTime < 0.001f || screenDelta.magnitude < 1f)
		{
			return Vector3.zero;
		}

		var screenVelocity = screenDelta / deltaTime;
		var cameraRight = _gameCamera.transform.right;
		var cameraForward = _gameCamera.transform.forward;
		var horizontalForward = new Vector3(cameraForward.x, 0, cameraForward.z).normalized;

		var swipeMagnitude = screenVelocity.magnitude;
		var swipeDirection = screenVelocity.normalized;

		var rightComponent = cameraRight * swipeDirection.x;
		var verticalInput = swipeDirection.y;
		var upComponent = Vector3.up * verticalInput * _verticalToForwardRatio;
		var forwardComponent = horizontalForward * verticalInput * (1f - _verticalToForwardRatio);

		var worldDirection = (rightComponent + upComponent + forwardComponent).normalized;
		var worldVelocity = worldDirection * swipeMagnitude;
		worldVelocity += horizontalForward * _baseForwardForce;

		return worldVelocity;
	}

	private Vector3 CalculateLaunchVelocity(Vector3 swipeVelocity)
	{
		var velocity = swipeVelocity * _swipeForceMultiplier;

		if (velocity.magnitude > _maxLaunchForce)
		{
			velocity = velocity.normalized * _maxLaunchForce;
		}

		return velocity;
	}

	private Vector3 GetWorldPosition(Vector2 screenPosition)
	{
		var platformPosition = _launchPlatform != null ? _launchPlatform.GetSpawnPosition() : Vector3.zero;
		var distanceFromCamera = Vector3.Distance(_gameCamera.transform.position, platformPosition);
		var screenPoint = new Vector3(screenPosition.x, screenPosition.y, distanceFromCamera);
		var worldPosition = _gameCamera.ScreenToWorldPoint(screenPoint);

		return worldPosition;
	}

	public void Reset()
	{
		_isDragging = false;
		_grabbedPoint = null;
		_grabbedRigidbody = null;
		_currentHuman = null;

		_swipePositions.Clear();
		_swipeTimes.Clear();
		_screenSwipePositions.Clear();
		_screenSwipeTimes.Clear();
		_lastSwipeVelocity = Vector3.zero;
	}
}
}