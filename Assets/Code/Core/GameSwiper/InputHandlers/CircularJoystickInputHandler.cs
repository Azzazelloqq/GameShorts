using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Code.Core.GameSwiper.InputHandlers
{
/// <summary>
/// Handles circular joystick input for GameSwiper.
/// User drags an inner circle within an outer circle to swipe up/down.
/// </summary>
public class CircularJoystickInputHandler : GameSwiperInputHandler,
	IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler, IPointerUpHandler
{
	[Header("Joystick UI Elements")]
	[SerializeField]
	private RectTransform _outerCircle;

	[SerializeField]
	private RectTransform _innerCircle;

	[Header("Joystick Settings")]
	[SerializeField]
	[Tooltip("Distance from center required to trigger swipe (in outer circle radii). 2.0 = twice the outer circle radius")]
	private float _swipeThresholdInRadii = 2.0f;

	[SerializeField]
	[Tooltip("How sensitive the drag is")]
	private float _dragSensitivity = 1f;

	[SerializeField]
	[Tooltip("Maximum distance the inner circle can move from center (in outer circle radii)")]
	private float _maxDragDistanceInRadii = 2.5f;

	[SerializeField]
	[Tooltip("Speed at which joystick returns to center when released")]
	private float _returnSpeed = 10f;

	[SerializeField]
	[Tooltip("Show debug info")]
	private bool _showDebug = false;

	[Header("Visual Feedback")]
	[SerializeField]
	[Tooltip("Color of inner circle when idle")]
	private Color _idleColor = Color.white;

	[SerializeField]
	[Tooltip("Color of inner circle when ready to swipe")]
	private Color _readyColor = Color.green;

	[SerializeField]
	[Tooltip("Enable color feedback")]
	private bool _useColorFeedback = true;

	[Header("Gooey Effect (Optional)")]
	[SerializeField]
	[Tooltip("Optional: GooeyJoystickEffect component for viscous liquid effect")]
	private GooeyJoystickEffect _gooeyEffect;

	[Header("Behaviour")]
	[SerializeField]
	[Tooltip("Hide joystick visuals when input is disabled")] 
	private bool _hideWhenDisabled = false;

	private Image _innerCircleImage;
	private bool _isDragging;
	private bool _isEnabled = true;
	private bool _canGoNext = true;
	private bool _canGoPrevious = true;
	private Vector2 _outerCircleCenter;
	private float _outerCircleRadius;
	private Vector2 _currentDragOffset;
	private bool _isReturning;

	public override bool IsEnabled
	{
		get => _isEnabled;
		set
		{
			if (_isEnabled == value)
			{
				return;
			}

			_isEnabled = value;
			if (!_isEnabled && _isDragging)
			{
				ResetInputState();
			}

			UpdateJoystickVisibility();

			if (_isEnabled)
			{
				CalculateCircleParameters();
				UpdateInnerCirclePosition();
				UpdateVisualFeedback();
			}

			_gooeyEffect?.UpdateEffect();
		}
	}

	private void Awake()
	{
		if (_innerCircle != null && _useColorFeedback)
		{
			_innerCircleImage = _innerCircle.GetComponent<Image>();
			if (_innerCircleImage != null)
			{
				_innerCircleImage.color = _idleColor;
			}
		}

		CalculateCircleParameters();
	}

	private void Start()
	{
		UpdateJoystickVisibility();
	}

	private void Update()
	{
		// Smoothly return joystick to center when not dragging
		if (_isReturning && !_isDragging)
		{
			_currentDragOffset = Vector2.Lerp(_currentDragOffset, Vector2.zero, Time.deltaTime * _returnSpeed);

			if (_currentDragOffset.magnitude < 0.1f)
			{
				_currentDragOffset = Vector2.zero;
				_isReturning = false;
			}

			UpdateInnerCirclePosition();
			UpdateVisualFeedback();
		}
	}

	public override void SetNavigationAvailability(bool canGoNext, bool canGoPrevious)
	{
		_canGoNext = canGoNext;
		_canGoPrevious = canGoPrevious;
	}

	public override void ResetInputState()
	{
		ResetInputStateInternal(forceInstant: false);
	}

	private void ResetInputStateInternal(bool forceInstant)
	{
		_isDragging = false;

		if (forceInstant)
		{
			_isReturning = false;
			_currentDragOffset = Vector2.zero;
		}
		else
		{
			_isReturning = _currentDragOffset.sqrMagnitude > Mathf.Epsilon;
		}

		UpdateInnerCirclePosition();
		ReportDragProgress(0f);
		UpdateVisualFeedback();
		_gooeyEffect?.UpdateEffect();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (!_isEnabled)
		{
			return;
		}

		_isReturning = false;
	}

	public void OnPointerUp(PointerEventData eventData)
	{
		if (!_isEnabled)
		{
			return;
		}

		_isReturning = true;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!_isEnabled)
		{
			return;
		}

		_isDragging = true;
		_isReturning = false;
		CalculateCircleParameters();
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!_isEnabled || !_isDragging)
		{
			return;
		}

		// Convert screen position to local position relative to outer circle
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			_outerCircle,
			eventData.position,
			eventData.pressEventCamera,
			out var localPoint);

		// Calculate offset from center
		_currentDragOffset = localPoint * _dragSensitivity;

		// Clamp to max distance
		var maxDistance = _outerCircleRadius * _maxDragDistanceInRadii;
		if (_currentDragOffset.magnitude > maxDistance)
		{
			_currentDragOffset = _currentDragOffset.normalized * maxDistance;
		}

		// Update visual position
		UpdateInnerCirclePosition();

		// Calculate progress for vertical movement only
		var verticalDistance = _currentDragOffset.y;
		var swipeThreshold = _outerCircleRadius * _swipeThresholdInRadii;
		var progress = Mathf.Clamp(verticalDistance / swipeThreshold, -1f, 1f);

		// Report progress (inverted: positive = up = next game)
		ReportDragProgress(progress);

		// Update visual feedback
		UpdateVisualFeedback();

		if (_showDebug)
		{
			Debug.Log($"Joystick - Offset: {_currentDragOffset}, Progress: {progress:F2}, " +
					$"Distance: {_currentDragOffset.magnitude:F0}, Threshold: {swipeThreshold:F0}");
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!_isEnabled || !_isDragging)
		{
			return;
		}

		_isDragging = false;

		// Calculate if swipe threshold was met (vertical only)
		var verticalDistance = _currentDragOffset.y;
		var swipeThreshold = _outerCircleRadius * _swipeThresholdInRadii;

		if (Mathf.Abs(verticalDistance) >= swipeThreshold)
		{
			if (verticalDistance > 0 && _canGoNext)
			{
				// Dragged up (positive Y) - go to next game
				if (_showDebug)
				{
					Debug.Log("Joystick: Swipe UP (Next Game)");
				}

				RequestNextGame();
			}
			else if (verticalDistance < 0 && _canGoPrevious)
			{
				// Dragged down (negative Y) - go to previous game
				if (_showDebug)
				{
					Debug.Log("Joystick: Swipe DOWN (Previous Game)");
				}

				RequestPreviousGame();
			}
		}

		// Start returning to center
		_isReturning = true;
	}

	private void CalculateCircleParameters()
	{
		if (_outerCircle == null)
		{
			return;
		}

		// Get center position in screen space
		_outerCircleCenter = _outerCircle.position;

		// Calculate radius (half of width, assuming circle is square)
		_outerCircleRadius = _outerCircle.rect.width * 0.5f;

		if (_showDebug)
		{
			Debug.Log($"Joystick - Center: {_outerCircleCenter}, Radius: {_outerCircleRadius}");
		}
	}

	private void UpdateInnerCirclePosition()
	{
		if (_innerCircle != null)
		{
			_innerCircle.anchoredPosition = _currentDragOffset;
		}

		_gooeyEffect?.UpdateEffect();
	}

	private void UpdateVisualFeedback()
	{
		if (!_useColorFeedback || _innerCircleImage == null)
		{
			return;
		}

		// Check if we've reached the swipe threshold
		var verticalDistance = Mathf.Abs(_currentDragOffset.y);
		var swipeThreshold = _outerCircleRadius * _swipeThresholdInRadii;

		var isReady = verticalDistance >= swipeThreshold;

		// Interpolate color based on progress
		var progress = Mathf.Clamp01(verticalDistance / swipeThreshold);
		_innerCircleImage.color = Color.Lerp(_idleColor, _readyColor, progress);
		_gooeyEffect?.UpdateEffect();
	}

	private void UpdateJoystickVisibility()
	{
		if (_outerCircle != null)
		{
			if (_hideWhenDisabled)
			{
				_outerCircle.gameObject.SetActive(_isEnabled);
			}
			else if (!_outerCircle.gameObject.activeSelf)
			{
				_outerCircle.gameObject.SetActive(true);
			}
		}

		// Update gooey effect visibility
		if (_gooeyEffect != null)
		{
			if (_hideWhenDisabled)
			{
				_gooeyEffect.gameObject.SetActive(_isEnabled);
			}
			else if (!_gooeyEffect.gameObject.activeSelf)
			{
				_gooeyEffect.gameObject.SetActive(true);
			}

			if (_isEnabled || !_hideWhenDisabled)
			{
				_gooeyEffect.UpdateEffect();
			}
		}
	}

	private void OnDisable()
	{
		ResetInputStateInternal(forceInstant: true);
	}

	private void OnValidate()
	{
		// Ensure values are reasonable
		_swipeThresholdInRadii = Mathf.Max(0.1f, _swipeThresholdInRadii);
		_maxDragDistanceInRadii = Mathf.Max(_swipeThresholdInRadii, _maxDragDistanceInRadii);
		_dragSensitivity = Mathf.Max(0.1f, _dragSensitivity);
		_returnSpeed = Mathf.Max(0.1f, _returnSpeed);
	}

	// Debug visualization
	private void OnDrawGizmosSelected()
	{
		if (_outerCircle == null || !Application.isPlaying)
		{
			return;
		}

		// Draw swipe threshold circle
		Gizmos.color = Color.yellow;
		var swipeDistance = _outerCircleRadius * _swipeThresholdInRadii;
		DrawCircle(_outerCircle.position, swipeDistance);

		// Draw max drag distance circle
		Gizmos.color = Color.red;
		var maxDistance = _outerCircleRadius * _maxDragDistanceInRadii;
		DrawCircle(_outerCircle.position, maxDistance);

		// Draw current offset
		if (_currentDragOffset.magnitude > 0.1f)
		{
			Gizmos.color = Color.green;
			var worldOffset = _outerCircle.TransformVector(_currentDragOffset);
			Gizmos.DrawLine(_outerCircle.position, _outerCircle.position + worldOffset);
		}
	}

	private void DrawCircle(Vector3 center, float radius, int segments = 32)
	{
		var angleStep = 360f / segments;
		var prevPoint = center + new Vector3(radius, 0, 0);

		for (var i = 1; i <= segments; i++)
		{
			var angle = i * angleStep * Mathf.Deg2Rad;
			var newPoint = center + new Vector3(
				Mathf.Cos(angle) * radius,
				Mathf.Sin(angle) * radius,
				0);
			Gizmos.DrawLine(prevPoint, newPoint);
			prevPoint = newPoint;
		}
	}
}
}