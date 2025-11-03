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

	// Events for external communication
	public event Action OnNextGameRequested;
	public event Action OnPreviousGameRequested;

	private GameSwiperImageFitter _topImageFitter;
	private GameSwiperImageFitter _centerImageFitter;
	private GameSwiperImageFitter _bottomImageFitter;

	private float ActualImageSpacing => _useScreenHeight ? Screen.height : _imageSpacing;

	// State
	private bool _isAnimating;
	private bool _canGoNext = true;
	private bool _canGoPrevious = true;
	private bool _isTransitionRequested; // Flag to prevent position reset during transition

	private void Awake()
	{
		SetupImageFitters();
		SetupInitialPositions();
		SetupInputHandlers();
	}

	private void SetupImageFitters()
	{
		_topImageFitter = _topImage?.GetComponent<GameSwiperImageFitter>();
		if (_topImageFitter == null && _topImage != null)
		{
			_topImageFitter = _topImage.gameObject.AddComponent<GameSwiperImageFitter>();
			// Component will initialize itself via Awake
		}

		_centerImageFitter = _centerImage?.GetComponent<GameSwiperImageFitter>();
		if (_centerImageFitter == null && _centerImage != null)
		{
			_centerImageFitter = _centerImage.gameObject.AddComponent<GameSwiperImageFitter>();
			// Component will initialize itself via Awake
		}

		_bottomImageFitter = _bottomImage?.GetComponent<GameSwiperImageFitter>();
		if (_bottomImageFitter == null && _bottomImage != null)
		{
			_bottomImageFitter = _bottomImage.gameObject.AddComponent<GameSwiperImageFitter>();
			// Component will initialize itself via Awake
		}

		// Initial setup for aspect ratio fitting
		if (_topImageFitter != null)
		{
			_topImageFitter.OnTextureChanged();
		}

		if (_centerImageFitter != null)
		{
			_centerImageFitter.OnTextureChanged();
		}

		if (_bottomImageFitter != null)
		{
			_bottomImageFitter.OnTextureChanged();
		}
	}

	private void SetupInitialPositions()
	{
		if (_topImage)
		{
			_topImage.rectTransform.anchoredPosition = new Vector2(0, ActualImageSpacing);
		}

		if (_centerImage)
		{
			_centerImage.rectTransform.anchoredPosition = Vector2.zero;
		}

		if (_bottomImage)
		{
			_bottomImage.rectTransform.anchoredPosition = new Vector2(0, -ActualImageSpacing);
		}

		// Hide the loading indicator by default
		if (_loadingIndicator)
		{
			_loadingIndicator.SetActive(false);
		}
	}

	private void SetupInputHandlers()
	{
		// Subscribe to events from all handlers
		foreach (var handler in _inputHandlers)
		{
			if (handler != null)
			{
				handler.OnNextGameRequested += HandleNextGameRequest;
				handler.OnPreviousGameRequested += HandlePreviousGameRequest;
				handler.OnDragProgress += HandleSwipeProgress;
			}
		}
	}

	/// <summary>
	/// Enable or disable the swiper component
	/// </summary>
	public void SetEnabled(bool enabled)
	{
		gameObject.SetActive(enabled);
		if (!enabled)
		{
			SetInputHandlersEnabled(false);
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
			_topImage.raycastTarget = false; // Don't block input for preview games
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
			_centerImage.raycastTarget = false; // Don't block input - let the game UI handle it
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
			_bottomImage.raycastTarget = false; // Don't block input for preview games
			// Update aspect ratio fitter when texture changes
			if (_bottomImageFitter != null)
			{
				_bottomImageFitter.OnTextureChanged();
			}
		}
	}

	/// <summary>
	/// Reset transition request flag (call when transition fails or is cancelled)
	/// </summary>
	public void ResetTransitionRequest()
	{
		_isTransitionRequested = false;
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
	/// Prepare textures for swipe up animation (to next game)
	/// This rotates textures to match what the animation expects
	/// </summary>
	public void PrepareTexturesForNextAnimation(RenderTexture previous, RenderTexture current, RenderTexture next)
	{
		// For swipe up animation, the bottom image will move to center,
		// So we need: top=previous, center=current, bottom=next,
		// But after the game logic switch, we have new textures
		// We want the bottom to show what will be the new current,
		// So: top stays with old previous, center stays with old current, bottom gets new current

		if (_topImage)
		{
			// Top will animate out of screen, can keep old previous or clear
			_topImage.texture = null;
			_topImage.enabled = false;
		}

		if (_centerImage)
		{
			// Center will move to top, should show the old current (which is new previous)
			_centerImage.texture = previous;
			_centerImage.enabled = previous != null;
			if (_centerImageFitter != null)
			{
				_centerImageFitter.OnTextureChanged();
			}
		}

		if (_bottomImage)
		{
			// Bottom will move to center, should show a new current
			_bottomImage.texture = current;
			_bottomImage.enabled = current != null;
			if (_bottomImageFitter != null)
			{
				_bottomImageFitter.OnTextureChanged();
			}
		}
	}

	/// <summary>
	/// Prepare textures for swipe down animation (to previous game)
	/// This rotates textures to match what the animation expects
	/// </summary>
	public void PrepareTexturesForPreviousAnimation(RenderTexture previous, RenderTexture current, RenderTexture next)
	{
		// For swipe down animation, the top image will move to center,
		// So we need the top to show what will be the new current

		if (_topImage)
		{
			// Top will move to center, should show new current
			_topImage.texture = current;
			_topImage.enabled = current != null;
			if (_topImageFitter != null)
			{
				_topImageFitter.OnTextureChanged();
			}
		}

		if (_centerImage)
		{
			// Center will move to bottom, should show old current (which is new next)
			_centerImage.texture = next;
			_centerImage.enabled = next != null;
			if (_centerImageFitter != null)
			{
				_centerImageFitter.OnTextureChanged();
			}
		}

		if (_bottomImage)
		{
			// Bottom will animate out of screen, can keep old next or clear
			_bottomImage.texture = null;
			_bottomImage.enabled = false;
		}
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
			_isTransitionRequested = false; // Reset transition flag after animation completes
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
			_isTransitionRequested = false; // Reset transition flag after animation completes
			SetInputHandlersEnabled(true);
		}
	}

	private void HandleNextGameRequest()
	{
		if (_isAnimating || !_canGoNext)
		{
			return;
		}

		_isTransitionRequested = true;
		OnNextGameRequested?.Invoke();
	}

	private void HandlePreviousGameRequest()
	{
		if (_isAnimating || !_canGoPrevious)
		{
			return;
		}

		_isTransitionRequested = true;
		OnPreviousGameRequested?.Invoke();
	}

	private void HandleSwipeProgress(float progress)
	{
		// Visual feedback during swipe
		if (_isAnimating || _isTransitionRequested)
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
		// But not if a transition was already requested
		if (Mathf.Approximately(progress, 0f) && !_isTransitionRequested)
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

		// After animation, reset positions
		// The controller will call UpdateTextures with the final textures
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

		// After animation, reset positions
		// The controller will call UpdateTextures with the final textures
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