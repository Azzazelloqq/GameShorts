using UnityEngine;
using UnityEngine.EventSystems;

namespace Code.Core.GameSwiper.InputHandlers
{
/// <summary>
/// Handles swipe input for GameSwiper.
/// Detects vertical swipe gestures and converts them to navigation events.
/// </summary>
public class SwipeInputHandler : GameSwiperInputHandler,
	IBeginDragHandler, IDragHandler, IEndDragHandler
{
	[Header("Swipe Settings")]
	[SerializeField]
	private float _swipeThreshold = 100f;

	[SerializeField]
	private float _dragSensitivity = 1f;

	[SerializeField]
	private bool _invertSwipe = false;

	[Header("Rubber Band Effect")]
	[SerializeField]
	private float _maxRubberBandOffset = 100f;

	[SerializeField]
	private float _rubberBandResistance = 0.5f;

	private Vector2 _dragStartPosition;
	private float _currentDragDelta;
	private bool _isDragging;
	private bool _isEnabled = true;
	private bool _canGoNext = true;
	private bool _canGoPrevious = true;

	public override bool IsEnabled
	{
		get => _isEnabled;
		set
		{
			_isEnabled = value;
			if (!_isEnabled && _isDragging)
			{
				ResetInputState();
			}
		}
	}

	public override void SetNavigationAvailability(bool canGoNext, bool canGoPrevious)
	{
		_canGoNext = canGoNext;
		_canGoPrevious = canGoPrevious;
	}

	public override void ResetInputState()
	{
		_isDragging = false;
		_currentDragDelta = 0f;
		ReportDragProgress(0f);
	}

	public void OnBeginDrag(PointerEventData eventData)
	{
		if (!_isEnabled)
		{
			return;
		}

		_isDragging = true;
		_dragStartPosition = eventData.position;
		_currentDragDelta = 0f;
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (!_isEnabled || !_isDragging)
		{
			return;
		}

		// Calculate vertical drag delta
		var deltaY = eventData.position.y - _dragStartPosition.y;
		if (_invertSwipe)
		{
			deltaY = -deltaY;
		}

		_currentDragDelta = deltaY * _dragSensitivity;

		// Apply rubber band effect if at limits
		if ((_currentDragDelta > 0 && !_canGoNext) ||
			(_currentDragDelta < 0 && !_canGoPrevious))
		{
			_currentDragDelta *= _rubberBandResistance;
			_currentDragDelta = Mathf.Clamp(_currentDragDelta,
				-_maxRubberBandOffset, _maxRubberBandOffset);
		}

		// Calculate progress for visual feedback
		var progress = Mathf.Clamp(_currentDragDelta / _swipeThreshold, -1f, 1f);
		ReportDragProgress(progress);
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!_isEnabled || !_isDragging)
		{
			return;
		}

		_isDragging = false;

		// Check if swipe threshold was met
		if (Mathf.Abs(_currentDragDelta) >= _swipeThreshold)
		{
			if (_currentDragDelta > 0 && _canGoNext)
			{
				// Swipe up - go to next (like TikTok/YouTube Shorts)
				RequestNextGame();
			}
			else if (_currentDragDelta < 0 && _canGoPrevious)
			{
				// Swipe down - go to previous
				RequestPreviousGame();
			}
		}

		// Reset state
		ResetInputState();
	}

	private void OnDisable()
	{
		ResetInputState();
	}
}
}