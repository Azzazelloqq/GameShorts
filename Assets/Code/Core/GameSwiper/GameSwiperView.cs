using System;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.GameSwiper
{
public class GameSwiperView : BaseMonoBehaviour
{
	public enum TransitionDirection
	{
		Next,
		Previous
	}

	[Header("UI Controls")]
	[SerializeField]
	private Button _nextGameButton;

	[SerializeField]
	private Button _previousGameButton;

	[SerializeField]
	private GameObject _loadingIndicator;

	private bool _isTransitioning;

	[Header("Render Textures")]
	[SerializeField]
	private RawImage _currentGameImage;

	[SerializeField]
	private RawImage _transitionGameImage;

	[SerializeField]
	private RectTransform _gamesContainer;

	[SerializeField]
	private RawImage _nextGamePreview;

	[SerializeField]
	private RawImage _previousGamePreview;

	[Header("Animation Settings")]
	[SerializeField]
	private float _transitionDuration = 0.5f;

	[SerializeField]
	private Ease _transitionEase = Ease.InOutQuad;

	[Header("Interactive Swipe")]
	[SerializeField]
	private InteractiveSwipeHandler _swipeHandler;

	[SerializeField]
	private bool _useInteractiveSwipe = true;

	public struct Ctx
	{
		public ReactiveTrigger OnNextGameRequested;
		public ReactiveTrigger OnPreviousGameRequested;
		public CompositeDisposable Disposables;
	}

	private Ctx _ctx;

	public void SetCtx(Ctx ctx)
	{
		_ctx = ctx;

		// Subscribe to button clicks
		if (_nextGameButton != null)
		{
			_nextGameButton.OnClickAsObservable()
				.Subscribe(_ =>
				{
					if (!_isTransitioning)
					{
						_ctx.OnNextGameRequested.Notify();
					}
				})
				.AddTo(_ctx.Disposables);
		}

		if (_previousGameButton != null)
		{
			_previousGameButton.OnClickAsObservable()
				.Subscribe(_ =>
				{
					if (!_isTransitioning)
					{
						_ctx.OnPreviousGameRequested.Notify();
					}
				})
				.AddTo(_ctx.Disposables);
		}

		// Setup interactive swipe if enabled
		if (_useInteractiveSwipe && _swipeHandler != null)
		{
			SetupInteractiveSwipe();
		}
	}

	public void SetLoadingState(bool isLoading)
	{
		if (_loadingIndicator != null)
		{
			_loadingIndicator.SetActive(isLoading);
		}

		// Disable buttons during loading
		if (_nextGameButton != null)
		{
			_nextGameButton.interactable = !isLoading;
		}

		if (_previousGameButton != null)
		{
			_previousGameButton.interactable = !isLoading;
		}
	}

	/// <summary>
	/// Sets the current RenderTexture for display
	/// </summary>
	public void SetCurrentGameTexture(RenderTexture texture)
	{
		if (_currentGameImage != null && texture != null)
		{
			_currentGameImage.texture = texture;
		}
	}

	/// <summary>
	/// Animates transition between games
	/// </summary>
	public async Task AnimateTransition(RenderTexture from, RenderTexture to, TransitionDirection direction)
	{
		if (_currentGameImage == null || _transitionGameImage == null || _gamesContainer == null)
		{
			return;
		}

		// Set textures
		_currentGameImage.texture = from;
		_transitionGameImage.texture = to;

		// Initial positions
		var containerWidth = _gamesContainer.rect.width;
		var startPos = _gamesContainer.anchoredPosition;
		var targetPos = startPos;

		// Determine animation direction
		if (direction == TransitionDirection.Next)
		{
			// Swipe left (next game comes from right)
			_transitionGameImage.rectTransform.anchoredPosition = new Vector2(containerWidth, 0);
			targetPos = new Vector2(-containerWidth, startPos.y);
		}
		else
		{
			// Swipe right (previous game comes from left)
			_transitionGameImage.rectTransform.anchoredPosition = new Vector2(-containerWidth, 0);
			targetPos = new Vector2(containerWidth, startPos.y);
		}

		// Make both images visible
		_currentGameImage.gameObject.SetActive(true);
		_transitionGameImage.gameObject.SetActive(true);

		// Animate transition
		var sequence = DOTween.Sequence();

		// Move container with both images
		sequence.Append(_gamesContainer.DOAnchorPos(targetPos, _transitionDuration).SetEase(_transitionEase));
		sequence.Join(_transitionGameImage.rectTransform.DOAnchorPos(Vector2.zero, _transitionDuration)
			.SetEase(_transitionEase));

		await sequence.AsyncWaitForCompletion();

		// After animation, change current texture
		_currentGameImage.texture = to;
		_gamesContainer.anchoredPosition = startPos;

		// Hide temporary image
		_transitionGameImage.gameObject.SetActive(false);
	}

	/// <summary>
	/// Updates button states based on availability of neighboring games
	/// </summary>
	public void UpdateNavigationButtons(bool hasNext, bool hasPrevious)
	{
		if (_nextGameButton != null)
		{
			_nextGameButton.gameObject.SetActive(hasNext);
		}

		if (_previousGameButton != null)
		{
			_previousGameButton.gameObject.SetActive(hasPrevious);
		}

		// Update swipe availability
		if (_swipeHandler != null)
		{
			_swipeHandler.UpdateSwipeAvailability(hasNext, hasPrevious);
		}
	}

	/// <summary>
	/// Sets up render textures for preview during swipe
	/// </summary>
	public void SetPreviewTextures(RenderTexture next, RenderTexture previous)
	{
		if (_nextGamePreview != null && next != null)
		{
			_nextGamePreview.texture = next;
		}

		if (_previousGamePreview != null && previous != null)
		{
			_previousGamePreview.texture = previous;
		}
	}

	private void SetupInteractiveSwipe()
	{
		// Subscribe to swipe events
		_swipeHandler.OnSwipeProgress += HandleSwipeProgress;
		_swipeHandler.OnSwipeComplete += HandleSwipeComplete;
		_swipeHandler.OnSwipeCancelled += HandleSwipeCancelled;
	}

	private void HandleSwipeProgress(InteractiveSwipeHandler.SwipeDirection direction, float progress)
	{
		// Optional: Update UI based on swipe progress
		// For example, show loading indicator when close to threshold
		if (progress > 0.8f && !_isTransitioning)
		{
			// Visual feedback that swipe is about to trigger
		}
	}

	private void HandleSwipeComplete(InteractiveSwipeHandler.SwipeDirection direction)
	{
		if (_isTransitioning)
		{
			return;
		}

		_isTransitioning = true;

		switch (direction)
		{
			case InteractiveSwipeHandler.SwipeDirection.Down:
				_ctx.OnNextGameRequested?.Notify();
				break;
			case InteractiveSwipeHandler.SwipeDirection.Up:
				_ctx.OnPreviousGameRequested?.Notify();
				break;
		}

		// Reset transitioning flag after a delay
		Observable.Timer(TimeSpan.FromSeconds(_transitionDuration))
			.Subscribe(_ => _isTransitioning = false)
			.AddTo(_ctx.Disposables);
	}

	private void HandleSwipeCancelled()
	{
		// Swipe was cancelled, reset any visual states
		_isTransitioning = false;
	}

	private void OnDestroy()
	{
		if (_swipeHandler != null)
		{
			_swipeHandler.OnSwipeProgress -= HandleSwipeProgress;
			_swipeHandler.OnSwipeComplete -= HandleSwipeComplete;
			_swipeHandler.OnSwipeCancelled -= HandleSwipeCancelled;
		}
	}
}
}