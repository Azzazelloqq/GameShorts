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
	private RawImage _topImage; // Previous game (Y: +imageSpacing)

	[SerializeField]
	private RawImage _centerImage; // Current game (Y: 0, visible)

	[SerializeField]
	private RawImage _bottomImage; // Next game (Y: -imageSpacing)
	
	private GameSwiperImageFitter _topImageFitter;
	private GameSwiperImageFitter _centerImageFitter;
	private GameSwiperImageFitter _bottomImageFitter;

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
	private bool _useScreenHeight = true; // Use actual screen height for spacing
	
	[SerializeField]
	private float _imageSpacing = 1920f; // Default fallback if not using screen height
	
	private float ActualImageSpacing => _useScreenHeight ? Screen.height : _imageSpacing;

	// Events for external communication
	public event Action OnNextGameRequested;
	public event Action OnPreviousGameRequested;

	// State
	private bool _isAnimating;
	private bool _canGoNext = true;
	private bool _canGoPrevious = true;

	private void Awake()
	{
		SetupImageFitters();
		SetupInitialPositions();
		SetupInputHandlers();
	}
	
	private void SetupImageFitters()
	{
		// Add aspect ratio fitters to all images
		if (_topImage != null)
		{
			_topImageFitter = _topImage.GetComponent<GameSwiperImageFitter>();
			if (_topImageFitter == null)
			{
				_topImageFitter = _topImage.gameObject.AddComponent<GameSwiperImageFitter>();
			}
		}
		
		if (_centerImage != null)
		{
			_centerImageFitter = _centerImage.GetComponent<GameSwiperImageFitter>();
			if (_centerImageFitter == null)
			{
				_centerImageFitter = _centerImage.gameObject.AddComponent<GameSwiperImageFitter>();
			}
		}
		
		if (_bottomImage != null)
		{
			_bottomImageFitter = _bottomImage.GetComponent<GameSwiperImageFitter>();
			if (_bottomImageFitter == null)
			{
				_bottomImageFitter = _bottomImage.gameObject.AddComponent<GameSwiperImageFitter>();
			}
		}
	}

	private void SetupInitialPositions()
	{
		// Ensure all images have proper size to fill the screen width/height
		if (_topImage)
		{
			SetupImageSize(_topImage.rectTransform);
			_topImage.rectTransform.anchoredPosition = new Vector2(0, ActualImageSpacing);
		}

		if (_centerImage)
		{
			SetupImageSize(_centerImage.rectTransform);
			_centerImage.rectTransform.anchoredPosition = Vector2.zero;
		}

		if (_bottomImage)
		{
			SetupImageSize(_bottomImage.rectTransform);
			_bottomImage.rectTransform.anchoredPosition = new Vector2(0, -ActualImageSpacing);
		}
	}
	
	private void SetupImageSize(RectTransform rectTransform)
	{
		// Set anchors to center
		rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		
		// Set size to match screen dimensions
		rectTransform.sizeDelta = new Vector2(Screen.width, Screen.height);
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
		// Always update textures in the correct positions
		// Top = Previous, Center = Current, Bottom = Next
		if (_topImage)
		{
			_topImage.texture = previous;
			_topImage.enabled = previous != null;
			// Update aspect ratio fitter when texture changes
			if (_topImageFitter != null)
			{
				_topImageFitter.OnTextureChanged();
			}
		}

		if (_centerImage)
		{
			_centerImage.texture = current;
			_centerImage.enabled = current != null;
			// Update aspect ratio fitter when texture changes
			if (_centerImageFitter != null)
			{
				_centerImageFitter.OnTextureChanged();
			}
		}

		if (_bottomImage)
		{
			_bottomImage.texture = next;
			_bottomImage.enabled = next != null;
			// Update aspect ratio fitter when texture changes
			if (_bottomImageFitter != null)
			{
				_bottomImageFitter.OnTextureChanged();
			}
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

		// Invert offset - positive progress should move up (to show next game from bottom)
		var offset = -progress * ActualImageSpacing * 0.3f; // 30% of full distance for preview

		if (_topImage)
		{
			_topImage.rectTransform.anchoredPosition = new Vector2(0, ActualImageSpacing - offset);
		}

		if (_centerImage)
		{
			_centerImage.rectTransform.anchoredPosition = new Vector2(0, -offset);
		}

		if (_bottomImage)
		{
			_bottomImage.rectTransform.anchoredPosition = new Vector2(0, -ActualImageSpacing - offset);
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
			.DOAnchorPos(new Vector2(0, ActualImageSpacing * 2), _animationDuration)
			.SetEase(_animationEase));
		sequence.Join(_centerImage.rectTransform
			.DOAnchorPos(new Vector2(0, ActualImageSpacing), _animationDuration)
			.SetEase(_animationEase));
		sequence.Join(_bottomImage.rectTransform
			.DOAnchorPos(Vector2.zero, _animationDuration)
			.SetEase(_animationEase));

		await sequence.AsyncWaitForCompletion();

		// After animation, reset positions without rotating references
		// The textures will be updated by UpdateTextures call from controller
		_topImage.rectTransform.anchoredPosition = new Vector2(0, ActualImageSpacing);
		_centerImage.rectTransform.anchoredPosition = Vector2.zero;
		_bottomImage.rectTransform.anchoredPosition = new Vector2(0, -ActualImageSpacing);
	}

	private async Task AnimateDown()
	{
		var sequence = DOTween.Sequence();

		sequence.Append(_topImage.rectTransform
			.DOAnchorPos(Vector2.zero, _animationDuration)
			.SetEase(_animationEase));
		sequence.Join(_centerImage.rectTransform
			.DOAnchorPos(new Vector2(0, -ActualImageSpacing), _animationDuration)
			.SetEase(_animationEase));
		sequence.Join(_bottomImage.rectTransform
			.DOAnchorPos(new Vector2(0, -ActualImageSpacing * 2), _animationDuration)
			.SetEase(_animationEase));

		await sequence.AsyncWaitForCompletion();

		// After animation, reset positions without rotating references
		// The textures will be updated by UpdateTextures call from controller
		_topImage.rectTransform.anchoredPosition = new Vector2(0, ActualImageSpacing);
		_centerImage.rectTransform.anchoredPosition = Vector2.zero;
		_bottomImage.rectTransform.anchoredPosition = new Vector2(0, -ActualImageSpacing);
	}

	private void ResetPositions()
	{
		if (_topImage)
		{
			_topImage.rectTransform.DOAnchorPos(new Vector2(0, ActualImageSpacing), 0.2f);
		}

		if (_centerImage)
		{
			_centerImage.rectTransform.DOAnchorPos(Vector2.zero, 0.2f);
		}

		if (_bottomImage)
		{
			_bottomImage.rectTransform.DOAnchorPos(new Vector2(0, -ActualImageSpacing), 0.2f);
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