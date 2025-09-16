using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.Core.GameSwiper
{
/// <summary>
/// Handles interactive swipe gestures with support for direction changes and cancellation
/// Similar to TikTok/YouTube Shorts swipe behavior
/// </summary>
public class InteractiveSwipeHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[Header("Swipe Settings")]
	[SerializeField]
	private float _swipeThreshold = 50f; // Minimum distance to trigger swipe

	[SerializeField]
	private float _swipeDeadzone = 10f; // Dead zone before swipe starts

	[SerializeField]
	private float _maxSwipeTime = 1f; // Maximum time for a swipe gesture

	[SerializeField]
	private bool _invertVerticalSwipe = false; // Invert up/down direction

	[Header("Visual Feedback")]
	[SerializeField]
	private RectTransform _currentGameContainer;

	[SerializeField]
	private RectTransform _nextGamePreview;

	[SerializeField]
	private RectTransform _previousGamePreview;

	[SerializeField]
	private float _previewScale = 0.9f; // Scale of preview during swipe

	[SerializeField]
	private float _rubberBandStrength = 0.5f; // Elasticity when reaching limits

	// Events
	public event Action<SwipeDirection, float> OnSwipeProgress; // Direction and progress (0-1)
	public event Action<SwipeDirection> OnSwipeComplete;
	public event Action OnSwipeCancelled;

	public enum SwipeDirection
	{
		None,
		Up, // Previous game
		Down // Next game
	}

	// State
	private bool _isDragging;
	private Vector2 _startPosition;
	private Vector2 _currentPosition;
	private float _swipeStartTime;
	private SwipeDirection _currentDirection;
	private SwipeDirection _committedDirection;
	private float _currentProgress;
	private bool _canSwipeNext;
	private bool _canSwipePrevious;
	private Canvas _canvas;

	private void Awake()
	{
		_canvas = GetComponentInParent<Canvas>();
	}

	/// <summary>
	/// Updates swipe availability based on game queue state
	/// </summary>
	public void UpdateSwipeAvailability(bool canSwipeNext, bool canSwipePrevious)
	{
		_canSwipeNext = canSwipeNext;
		_canSwipePrevious = canSwipePrevious;
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!enabled)
		{
			return;
		}

		_isDragging = true;
		_startPosition = eventData.position;
		_currentPosition = _startPosition;
		_swipeStartTime = Time.time;
		_currentDirection = SwipeDirection.None;
		_committedDirection = SwipeDirection.None;
		_currentProgress = 0f;

		// Show previews
		ShowPreviews();
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!_isDragging)
		{
			return;
		}

		_currentPosition = eventData.position;

		// Calculate swipe delta
		var delta = _currentPosition - _startPosition;

		// Apply canvas scale factor if needed
		if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
		{
			delta /= _canvas.scaleFactor;
		}

		// Check if we've moved past the deadzone
		if (Mathf.Abs(delta.y) < _swipeDeadzone)
		{
			ResetVisuals();
			return;
		}

		// Determine direction
		var newDirection = delta.y > 0 ? _invertVerticalSwipe ? SwipeDirection.Down : SwipeDirection.Up :
			_invertVerticalSwipe ? SwipeDirection.Up : SwipeDirection.Down;

		// Check if direction is allowed
		if ((newDirection == SwipeDirection.Down && !_canSwipeNext) ||
			(newDirection == SwipeDirection.Up && !_canSwipePrevious))
		{
			// Apply rubber band effect for disabled directions
			delta.y *= _rubberBandStrength;
		}

		// Update direction if changed
		if (_currentDirection != newDirection)
		{
			_currentDirection = newDirection;

			// If we haven't committed to a direction yet, update it
			if (_committedDirection == SwipeDirection.None && Mathf.Abs(delta.y) > _swipeThreshold * 0.3f)
			{
				_committedDirection = newDirection;
			}
		}

		// Calculate progress (0 to 1)
		_currentProgress = Mathf.Clamp01(Mathf.Abs(delta.y) / _swipeThreshold);

		// Update visuals
		UpdateSwipeVisuals(delta.y, _currentDirection, _currentProgress);

		// Notify progress
		OnSwipeProgress?.Invoke(_currentDirection, _currentProgress);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!_isDragging)
		{
			return;
		}

		_isDragging = false;

		// Calculate final swipe metrics
		var delta = _currentPosition - _startPosition;
		var swipeTime = Time.time - _swipeStartTime;
		var swipeDistance = Mathf.Abs(delta.y);

		// Apply canvas scale factor
		if (_canvas != null && _canvas.renderMode != RenderMode.ScreenSpaceOverlay)
		{
			swipeDistance /= _canvas.scaleFactor;
		}

		// Determine if swipe is valid
		var isValidSwipe = swipeDistance >= _swipeThreshold &&
							swipeTime <= _maxSwipeTime &&
							_committedDirection != SwipeDirection.None;

		// Check if the committed direction is allowed
		if (isValidSwipe)
		{
			if ((_committedDirection == SwipeDirection.Down && !_canSwipeNext) ||
				(_committedDirection == SwipeDirection.Up && !_canSwipePrevious))
			{
				isValidSwipe = false;
			}
		}

		if (isValidSwipe)
		{
			// Complete the swipe
			CompleteSwipe(_committedDirection);
		}
		else
		{
			// Cancel the swipe - animate back
			CancelSwipe();
		}
	}

	private void ShowPreviews()
	{
		// Show appropriate preview based on availability
		if (_nextGamePreview != null && _canSwipeNext)
		{
			_nextGamePreview.gameObject.SetActive(true);
			_nextGamePreview.localScale = Vector3.one * _previewScale;
		}

		if (_previousGamePreview != null && _canSwipePrevious)
		{
			_previousGamePreview.gameObject.SetActive(true);
			_previousGamePreview.localScale = Vector3.one * _previewScale;
		}
	}

	private void UpdateSwipeVisuals(float deltaY, SwipeDirection direction, float progress)
	{
		if (_currentGameContainer == null)
		{
			return;
		}

		// Move current game container based on swipe
		Vector3 newPosition = _currentGameContainer.anchoredPosition;
		newPosition.y = deltaY * 0.5f; // Half speed for smoother feel
		_currentGameContainer.anchoredPosition = newPosition;

		// Update preview positions and scales
		if (direction == SwipeDirection.Down && _nextGamePreview != null)
		{
			// Bringing next game from bottom
			Vector3 previewPos = _nextGamePreview.anchoredPosition;
			previewPos.y = -Screen.height + (deltaY + Screen.height * progress);
			_nextGamePreview.anchoredPosition = previewPos;

			// Scale up as we progress
			var scale = Mathf.Lerp(_previewScale, 1f, progress);
			_nextGamePreview.localScale = Vector3.one * scale;
		}
		else if (direction == SwipeDirection.Up && _previousGamePreview != null)
		{
			// Bringing previous game from top
			Vector3 previewPos = _previousGamePreview.anchoredPosition;
			previewPos.y = Screen.height + (deltaY - Screen.height * progress);
			_previousGamePreview.anchoredPosition = previewPos;

			// Scale up as we progress
			var scale = Mathf.Lerp(_previewScale, 1f, progress);
			_previousGamePreview.localScale = Vector3.one * scale;
		}

		// Adjust opacity based on progress
		UpdateOpacity(progress);
	}

	private void UpdateOpacity(float progress)
	{
		// Optional: Update canvas group alpha based on progress
		var currentGroup = _currentGameContainer?.GetComponent<CanvasGroup>();
		if (currentGroup != null)
		{
			currentGroup.alpha = 1f - progress * 0.3f; // Fade out slightly
		}
	}

	private void CompleteSwipe(SwipeDirection direction)
	{
		// Trigger swipe complete event
		OnSwipeComplete?.Invoke(direction);

		// Reset visuals after animation
		ResetVisuals();
	}

	private void CancelSwipe()
	{
		// Trigger cancellation event
		OnSwipeCancelled?.Invoke();

		// Animate back to original position
		AnimateBackToOriginal();
	}

	private void AnimateBackToOriginal()
	{
		// Simple reset - in production, use DOTween or animation system
		if (_currentGameContainer != null)
		{
			_currentGameContainer.anchoredPosition = Vector2.zero;
		}

		ResetVisuals();
	}

	private void ResetVisuals()
	{
		// Reset all visual states
		if (_currentGameContainer != null)
		{
			_currentGameContainer.anchoredPosition = Vector2.zero;

			var group = _currentGameContainer.GetComponent<CanvasGroup>();
			if (group != null)
			{
				group.alpha = 1f;
			}
		}

		if (_nextGamePreview != null)
		{
			_nextGamePreview.gameObject.SetActive(false);
			_nextGamePreview.anchoredPosition = new Vector2(0, -Screen.height);
		}

		if (_previousGamePreview != null)
		{
			_previousGamePreview.gameObject.SetActive(false);
			_previousGamePreview.anchoredPosition = new Vector2(0, Screen.height);
		}

		_currentProgress = 0f;
		_currentDirection = SwipeDirection.None;
		_committedDirection = SwipeDirection.None;
	}

	/// <summary>
	/// Force cancel any ongoing swipe
	/// </summary>
	public void ForceCancel()
	{
		if (_isDragging)
		{
			_isDragging = false;
			CancelSwipe();
		}
	}

	private void OnDisable()
	{
		ForceCancel();
	}
}
}