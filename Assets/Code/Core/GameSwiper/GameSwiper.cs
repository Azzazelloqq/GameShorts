using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Core.GameSwiper.InputHandlers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.GameSwiper
{
/// <summary>
/// Pure UI component for vertical carousel animation
/// Only handles visual presentation and delegates input to handlers
/// Notifies about user actions via events
/// </summary>
internal class GameSwiper : MonoBehaviour
{
	[Header("Carousel Images")]
	[SerializeField]
	private RawImage _topImage; // Previous game (Y: +1080)

	[SerializeField]
	private RawImage _centerImage; // Current game (Y: 0, visible)

	[SerializeField]
	private RawImage _bottomImage; // Next game (Y: -1080)

	[Header("Input Handlers")]
	[SerializeField]
	private List<GameSwiperInputHandler> _inputHandlers = new();

	[Header("UI Elements")]
	[SerializeField]
	private GameObject _loadingIndicator;

	[Header("Settings")]
	[SerializeField]
	private float _animationDuration = 0.3f;

	[SerializeField]
	private Ease _animationEase = Ease.OutQuad;

	[SerializeField]
	private float _imageSpacing = 1080f;

	// Events for external communication
	public event Action OnNextGameRequested;
	public event Action OnPreviousGameRequested;

	// State
	private bool _isAnimating;
	private bool _canGoNext = true;
	private bool _canGoPrevious = true;

	private void Awake()
	{
		SetupInitialPositions();
		SetupInputHandlers();
	}

	private void SetupInitialPositions()
	{
		if (_topImage)
		{
			_topImage.rectTransform.anchoredPosition = new Vector2(0, _imageSpacing);
		}

		if (_centerImage)
		{
			_centerImage.rectTransform.anchoredPosition = Vector2.zero;
		}

		if (_bottomImage)
		{
			_bottomImage.rectTransform.anchoredPosition = new Vector2(0, -_imageSpacing);
		}
	}

	private void SetupInputHandlers()
	{
		foreach (var handler in _inputHandlers)
		{
			if (handler != null)
			{
				// Subscribe to all events - common API
				handler.OnNextGameRequested += HandleNextGameRequest;
				handler.OnPreviousGameRequested += HandlePreviousGameRequest;
				handler.OnDragProgress += HandleSwipeProgress;
			}
		}
	}

	/// <summary>
	/// Update all three textures at once
	/// Called externally when game state changes
	/// </summary>
	public void UpdateTextures(RenderTexture previous, RenderTexture current, RenderTexture next)
	{
		if (_topImage)
		{
			_topImage.texture = previous;
		}

		if (_centerImage)
		{
			_centerImage.texture = current;
		}

		if (_bottomImage)
		{
			_bottomImage.texture = next;
		}
	}

	/// <summary>
	/// Update navigation availability
	/// </summary>
	public void UpdateNavigationStates(bool canGoNext, bool canGoPrevious)
	{
		_canGoNext = canGoNext;
		_canGoPrevious = canGoPrevious;

		// Update all input handlers
		foreach (var handler in _inputHandlers)
		{
			if (handler != null)
			{
				handler.SetNavigationAvailability(canGoNext && !_isAnimating, canGoPrevious && !_isAnimating);
			}
		}
	}

	/// <summary>
	/// Show/hide loading indicator
	/// </summary>
	public void SetLoadingState(bool isLoading)
	{
		_loadingIndicator.SetActive(isLoading);
	}

	/// <summary>
	/// Animate transition to next game (swipe up)
	/// </summary>
	public async Task AnimateToNext()
	{
		if (_isAnimating)
		{
			return;
		}

		_isAnimating = true;

		// Disable all inputs during animation
		SetInputHandlersEnabled(false);

		try
		{
			await AnimateUp();
		}
		finally
		{
			_isAnimating = false;
			SetInputHandlersEnabled(true);
		}
	}

	/// <summary>
	/// Animate transition to previous game (swipe down)
	/// </summary>
	public async Task AnimateToPrevious()
	{
		if (_isAnimating)
		{
			return;
		}

		_isAnimating = true;

		// Disable all inputs during animation
		SetInputHandlersEnabled(false);

		try
		{
			await AnimateDown();
		}
		finally
		{
			_isAnimating = false;
			SetInputHandlersEnabled(true);
		}
	}

	private void HandleNextGameRequest()
	{
		if (_isAnimating || !_canGoNext)
		{
			return;
		}

		OnNextGameRequested?.Invoke();
	}

	private void HandlePreviousGameRequest()
	{
		if (_isAnimating || !_canGoPrevious)
		{
			return;
		}

		OnPreviousGameRequested?.Invoke();
	}

	private void HandleSwipeProgress(float progress)
	{
		// Visual feedback during swipe
		if (_isAnimating)
		{
			return;
		}

		var offset = progress * _imageSpacing * 0.3f; // 30% of full distance for preview

		if (_topImage)
		{
			_topImage.rectTransform.anchoredPosition = new Vector2(0, _imageSpacing - offset);
		}

		if (_centerImage)
		{
			_centerImage.rectTransform.anchoredPosition = new Vector2(0, -offset);
		}

		if (_bottomImage)
		{
			_bottomImage.rectTransform.anchoredPosition = new Vector2(0, -_imageSpacing - offset);
		}

		// If progress returns to 0 (drag ended), reset positions
		if (Mathf.Approximately(progress, 0f))
		{
			ResetPositions();
		}
	}

	private void SetInputHandlersEnabled(bool enabled)
	{
		foreach (var handler in _inputHandlers)
		{
			if (handler != null)
			{
				handler.IsEnabled = enabled;
				if (!enabled)
				{
					handler.ResetInputState();
				}
			}
		}
	}

	private async Task AnimateUp()
	{
		var sequence = DOTween.Sequence();

		sequence.Append(_topImage.rectTransform
			.DOAnchorPos(new Vector2(0, _imageSpacing * 2), _animationDuration)
			.SetEase(_animationEase));
		sequence.Join(_centerImage.rectTransform
			.DOAnchorPos(new Vector2(0, _imageSpacing), _animationDuration)
			.SetEase(_animationEase));
		sequence.Join(_bottomImage.rectTransform
			.DOAnchorPos(Vector2.zero, _animationDuration)
			.SetEase(_animationEase));

		await sequence.AsyncWaitForCompletion();

		// Rotate references
		var temp = _topImage;
		_topImage = _centerImage;
		_centerImage = _bottomImage;
		_bottomImage = temp;

		// Reset position
		_bottomImage.rectTransform.anchoredPosition = new Vector2(0, -_imageSpacing);
	}

	private async Task AnimateDown()
	{
		var sequence = DOTween.Sequence();

		sequence.Append(_topImage.rectTransform
			.DOAnchorPos(Vector2.zero, _animationDuration)
			.SetEase(_animationEase));
		sequence.Join(_centerImage.rectTransform
			.DOAnchorPos(new Vector2(0, -_imageSpacing), _animationDuration)
			.SetEase(_animationEase));
		sequence.Join(_bottomImage.rectTransform
			.DOAnchorPos(new Vector2(0, -_imageSpacing * 2), _animationDuration)
			.SetEase(_animationEase));

		await sequence.AsyncWaitForCompletion();

		// Rotate references
		var temp = _bottomImage;
		_bottomImage = _centerImage;
		_centerImage = _topImage;
		_topImage = temp;

		// Reset position
		_topImage.rectTransform.anchoredPosition = new Vector2(0, _imageSpacing);
	}

	private void ResetPositions()
	{
		if (_topImage)
		{
			_topImage.rectTransform.DOAnchorPos(new Vector2(0, _imageSpacing), 0.2f);
		}

		if (_centerImage)
		{
			_centerImage.rectTransform.DOAnchorPos(Vector2.zero, 0.2f);
		}

		if (_bottomImage)
		{
			_bottomImage.rectTransform.DOAnchorPos(new Vector2(0, -_imageSpacing), 0.2f);
		}
	}

	private void OnDestroy()
	{
		// Unsubscribe from all handlers
		foreach (var handler in _inputHandlers)
		{
			if (handler != null)
			{
				handler.OnNextGameRequested -= HandleNextGameRequest;
				handler.OnPreviousGameRequested -= HandlePreviousGameRequest;
				handler.OnDragProgress -= HandleSwipeProgress;
			}
		}

		_inputHandlers.Clear();
	}
}
}