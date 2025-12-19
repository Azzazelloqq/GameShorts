using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Code.Core.GameStats;
using Code.Core.GameSwiper.InputHandlers;
using Code.Core.GameSwiper.MVVM.ViewModels;
using DG.Tweening;
using R3;
using TMPro;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.Views
{
internal class GameSwiperView : ViewMonoBehavior<GameSwiperViewModel>
{
	private const float DragProgressEpsilon = 0.001f;
	
	[Header("Game Item Slots")]
	[SerializeField]
	private RectTransform _previousSlotRoot;

	[SerializeField]
	private RectTransform _currentSlotRoot;

	[SerializeField]
	private RectTransform _nextSlotRoot;

	[Header("Prefabs")]
	[SerializeField]
	private GameItemView _gameItemViewPrefab;

	[Header("Input Handlers")]
	[SerializeField]
	private List<GameSwiperInputHandler> _inputHandlers = new();

	[Header("Loading")]
	[SerializeField]
	private GameObject _globalLoadingIndicator;

	[Header("Animation Settings")]
	[SerializeField]
	private float _animationDuration = 0.3f;

	[SerializeField]
	private Ease _animationEase = Ease.OutQuad;

	[SerializeField]
	private bool _useScreenHeight = true;

	[SerializeField]
	private float _imageSpacing = 1920f;

	[Header("Stats UI")]
	[SerializeField]
	private TMP_Text _gameTitleText;

	[Header("Drag Feedback")]
	[SerializeField]
	[Tooltip("Fraction of the full distance the cards travel while the player is dragging.")]
	[Range(0.1f, 1f)]
	private float _maxSwipeVisualOffsetRatio = 0.33f;
	
	private float ActualImageSpacing => _useScreenHeight ? Screen.height : _imageSpacing;

	private RectTransform _previousRect;
	private RectTransform _currentRect;
	private RectTransform _nextRect;
	private GameItemView _previousViewInstance;
	private GameItemView _currentViewInstance;
	private GameItemView _nextViewInstance;
	private readonly Dictionary<GameItemViewModel, GameItemView> _viewInstances = new();
	private readonly HashSet<GameItemViewModel> _activeViewModels = new();
	private readonly List<GameItemViewModel> _inactiveViewModelsBuffer = new();
	private bool _hasLoggedMissingPrefab;

	private GameItemViewModel _previousViewModel;
	private GameItemViewModel _currentViewModel;
	private GameItemViewModel _nextViewModel;

	private bool _isAnimating;
	private bool _hasPendingViewRefresh;
	private bool _isInitialized;
	private bool _isCleanedUp;
	private bool _isUserDragging;
	private bool _isModelTransitioning;
	private GameItemViewModel _pendingPreviousViewModel;
	private GameItemViewModel _pendingCurrentViewModel;
	private GameItemViewModel _pendingNextViewModel;
	private bool _hasPendingViewModelCache;
	private IDisposable _currentPresentationBinding;

	private bool ShouldDeferViewRefresh => _isAnimating || _isModelTransitioning || _isUserDragging;

	protected override void OnInitialize()
	{
		PrepareView();
		UpdateGameViews(true);
	}

	protected override ValueTask OnInitializeAsync(CancellationToken token)
	{
		PrepareView();
		UpdateGameViews(true);
		return default;
	}

	protected override void OnDispose()
	{
		CleanupBindings();
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		CleanupBindings();
		return default;
	}

	private void PrepareView()
	{
		if (_isInitialized)
		{
			return;
		}

		_isInitialized = true;
		CacheSlotComponents();
		SetupInputHandlers();
		BindToViewModel();
		ResetLayoutPositions(0f);
	}

	private void CacheSlotComponents()
	{
		_previousRect = _previousSlotRoot;
		_currentRect = _currentSlotRoot;
		_nextRect = _nextSlotRoot;
	}

	private void SetupInputHandlers()
	{
		foreach (var handler in _inputHandlers)
		{
			handler.OnNextGameRequested += HandleNextGameRequest;
			handler.OnPreviousGameRequested += HandlePreviousGameRequest;
			handler.OnDragProgress += HandleSwipeProgress;
		}
	}

	private void BindToViewModel()
	{
		compositeDisposable.AddDisposable(viewModel.ShouldShowLoadingIndicator.Subscribe(OnGlobalLoadingIndicatorChanged));
		compositeDisposable.AddDisposable(viewModel.IsTransitioning.Subscribe(OnTransitioningChanged));
		compositeDisposable.AddDisposable(viewModel.CanGoNext.Subscribe(_ => UpdateNavigationStates()));
		compositeDisposable.AddDisposable(viewModel.CanGoPrevious.Subscribe(_ => UpdateNavigationStates()));
		compositeDisposable.AddDisposable(viewModel.PreviousGameViewModel.Subscribe(_ => UpdateGameViews()));
		compositeDisposable.AddDisposable(viewModel.CurrentGameViewModel.Subscribe(_ => UpdateGameViews()));
		compositeDisposable.AddDisposable(viewModel.NextGameViewModel.Subscribe(_ => UpdateGameViews()));
		compositeDisposable.AddDisposable(viewModel.SwipeProgress.Subscribe(OnSwipeProgressChanged));
		compositeDisposable.AddDisposable(viewModel.CurrentGameViewModel.Subscribe(_ =>
		{
			BindCurrentPresentation(viewModel.CurrentGameViewModel.CurrentValue);
		}));
		viewModel.OnNextGameStartedAsync += AnimateToNext;
		viewModel.OnPreviousGameStartedAsync += AnimateToPrevious;

		BindCurrentPresentation(viewModel.CurrentGameViewModel.CurrentValue);
	}

	private void CleanupBindings()
	{
		if (_isCleanedUp)
		{
			return;
		}

		_isCleanedUp = true;

		foreach (var handler in _inputHandlers)
		{
			handler.OnNextGameRequested -= HandleNextGameRequest;
			handler.OnPreviousGameRequested -= HandlePreviousGameRequest;
			handler.OnDragProgress -= HandleSwipeProgress;
		}

		if (viewModel != null)
		{
			viewModel.OnNextGameStartedAsync -= AnimateToNext;
			viewModel.OnPreviousGameStartedAsync -= AnimateToPrevious;
		}

		DisposeSlotBinding(ref _currentPresentationBinding);
		DisposeAllViews();
	}

	private void UpdateGameViews(bool forceRefresh = false)
	{
		var targetPrevious = viewModel.PreviousGameViewModel.CurrentValue;
		var targetCurrent = viewModel.CurrentGameViewModel.CurrentValue;
		var targetNext = viewModel.NextGameViewModel.CurrentValue;

		if (ShouldDeferViewRefresh && !forceRefresh)
		{
			CachePendingViewModels(targetPrevious, targetCurrent, targetNext);
			_hasPendingViewRefresh = true;
			return;
		}

		if (_hasPendingViewRefresh && _hasPendingViewModelCache)
		{
			targetPrevious = _pendingPreviousViewModel;
			targetCurrent = _pendingCurrentViewModel;
			targetNext = _pendingNextViewModel;
		}

		_hasPendingViewRefresh = false;
		_hasPendingViewModelCache = false;
		
		_activeViewModels.Clear();
		UpdateSlot(ref _previousViewModel, ref _previousViewInstance, targetPrevious, _previousSlotRoot);
		UpdateSlot(ref _currentViewModel, ref _currentViewInstance, targetCurrent, _currentSlotRoot);
		UpdateSlot(ref _nextViewModel, ref _nextViewInstance, targetNext, _nextSlotRoot);
		CleanupUnusedViews();
	}

	private void CachePendingViewModels(
		GameItemViewModel previous,
		GameItemViewModel current,
		GameItemViewModel next)
	{
		_pendingPreviousViewModel = previous;
		_pendingCurrentViewModel = current;
		_pendingNextViewModel = next;
		_hasPendingViewModelCache = true;
	}

	private void BindCurrentPresentation(GameItemViewModel currentViewModel)
	{
		DisposeSlotBinding(ref _currentPresentationBinding);

		if (currentViewModel == null)
		{
			UpdateStatsUI(null);
			return;
		}

		_currentPresentationBinding = currentViewModel.Presentation.Subscribe(UpdateStatsUI);
	}

	private void UpdateSlot(
		ref GameItemViewModel storedViewModel,
		ref GameItemView slotViewInstance,
		GameItemViewModel targetViewModel,
		RectTransform slotRoot)
	{
		if (storedViewModel == targetViewModel && targetViewModel != null)
		{
			_activeViewModels.Add(targetViewModel);
			if (slotViewInstance != null && slotRoot != null)
			{
				AttachViewToSlot(slotViewInstance, slotRoot);
			}
			return;
		}

		storedViewModel = targetViewModel;

		if (targetViewModel == null)
		{
			DeactivateSlotView(ref slotViewInstance);
			return;
		}

		_activeViewModels.Add(targetViewModel);
		slotViewInstance = ResolveViewInstance(targetViewModel, slotRoot);
	}

	private GameItemView ResolveViewInstance(GameItemViewModel targetViewModel, RectTransform slotRoot)
	{
		if (targetViewModel == null)
		{
			return null;
		}

		if (_viewInstances.TryGetValue(targetViewModel, out var existingView))
		{
			if (existingView != null && !existingView.IsDisposed)
			{
				AttachViewToSlot(existingView, slotRoot);
				return existingView;
			}

			_viewInstances.Remove(targetViewModel);
		}

		if (slotRoot == null)
		{
			return null;
		}

		if (_gameItemViewPrefab == null)
		{
			if (!_hasLoggedMissingPrefab)
			{
				Debug.LogError($"{nameof(GameSwiperView)} is missing GameItemView prefab reference.");
				_hasLoggedMissingPrefab = true;
			}

			return null;
		}

		var viewInstance = Instantiate(_gameItemViewPrefab, slotRoot);
		AttachViewToSlot(viewInstance, slotRoot);
		viewInstance.Initialize(targetViewModel);
		_viewInstances[targetViewModel] = viewInstance;
		return viewInstance;
	}

	private static void AttachViewToSlot(GameItemView viewInstance, RectTransform slotRoot)
	{
		if (viewInstance == null || slotRoot == null)
		{
			return;
		}

		if (viewInstance.transform is not RectTransform viewRect)
		{
			return;
		}

		viewRect.SetParent(slotRoot, false);
		viewRect.anchorMin = Vector2.zero;
		viewRect.anchorMax = Vector2.one;
		viewRect.offsetMin = Vector2.zero;
		viewRect.offsetMax = Vector2.zero;
		viewRect.anchoredPosition3D = Vector3.zero;
		viewRect.localScale = Vector3.one;
		viewInstance.gameObject.SetActive(true);
	}

	private void DeactivateSlotView(ref GameItemView slotViewInstance)
	{
		if (slotViewInstance == null)
		{
			return;
		}

		slotViewInstance.gameObject.SetActive(false);
		slotViewInstance = null;
	}

	private void CleanupUnusedViews()
	{
		if (_viewInstances.Count == 0)
		{
			_activeViewModels.Clear();
			return;
		}

		_inactiveViewModelsBuffer.Clear();

		foreach (var pair in _viewInstances)
		{
			var viewModel = pair.Key;
			var viewInstance = pair.Value;

			var shouldRemove = viewModel == null ||
				!_activeViewModels.Contains(viewModel) ||
				viewInstance == null ||
				viewInstance.IsDisposed;

			if (!shouldRemove)
			{
				continue;
			}

			if (viewInstance != null && !viewInstance.IsDisposed)
			{
				viewInstance.Dispose();
			}

			if (viewModel != null)
			{
				_inactiveViewModelsBuffer.Add(viewModel);
			}
		}

		foreach (var viewModel in _inactiveViewModelsBuffer)
		{
			_viewInstances.Remove(viewModel);
		}

		_inactiveViewModelsBuffer.Clear();
		_activeViewModels.Clear();
	}

	private void HandleNextGameRequest()
	{
		if (IsInteractionLocked() || !viewModel.CanGoNext.CurrentValue)
		{
			return;
		}

		_ = viewModel.GoToNextCommand.ExecuteAsync();
	}

	private void HandlePreviousGameRequest()
	{
		if (IsInteractionLocked() || !viewModel.CanGoPrevious.CurrentValue)
		{
			return;
		}

		_ = viewModel.GoToPreviousCommand.ExecuteAsync();
	}

	private void HandleSwipeProgress(float progress)
	{
		if (IsInteractionLocked())
		{
			return;
		}

		viewModel.UpdateSwipeProgressCommand.Execute(progress);
	}

	private void OnSwipeProgressChanged(float progress)
	{
		UpdateUserDraggingState(progress);

		if (IsInteractionLocked())
		{
			return;
		}

		ApplySwipeOffset(progress);
	}

	private void UpdateUserDraggingState(float progress)
	{
		var isDragging = Mathf.Abs(progress) > DragProgressEpsilon;
		if (_isUserDragging == isDragging)
		{
			return;
		}

		_isUserDragging = isDragging;

		if (!_isUserDragging && !IsInteractionLocked() && _hasPendingViewRefresh)
		{
			UpdateGameViews(true);
		}
	}

	private void UpdateStatsUI(GamePresentationData? presentationData)
	{
		if (_gameTitleText != null)
		{
			_gameTitleText.text = presentationData?.DisplayName ?? "Загрузка...";
		}
	}

	private void ApplySwipeOffset(float progress)
	{
		var clampedRatio = Mathf.Clamp01(_maxSwipeVisualOffsetRatio);
		var clampedProgress = Mathf.Clamp(progress, -1f, 1f);
		var offset = clampedProgress * ActualImageSpacing * clampedRatio;
		SetSlotPosition(_previousRect, ActualImageSpacing + offset);
		SetSlotPosition(_currentRect, offset);
		SetSlotPosition(_nextRect, -ActualImageSpacing + offset);
	}

	private void SetSlotPosition(RectTransform rect, float targetY)
	{
		if (rect == null)
		{
			return;
		}

		rect.localPosition = new Vector3(0, targetY, 0);
	}

	private void OnGlobalLoadingIndicatorChanged(bool isVisible)
	{
		if (_globalLoadingIndicator != null)
		{
			_globalLoadingIndicator.SetActive(isVisible);
		}
	}

	private void OnTransitioningChanged(bool isTransitioning)
	{
		_isModelTransitioning = isTransitioning;

		if (!isTransitioning && !_isAnimating && _hasPendingViewRefresh)
		{
			UpdateGameViews(true);
		}

		UpdateInteractionState();
	}

	private void UpdateNavigationStates()
	{
		var canGoNext = viewModel.CanGoNext.CurrentValue && !IsInteractionLocked();
		var canGoPrevious = viewModel.CanGoPrevious.CurrentValue && !IsInteractionLocked();

		foreach (var handler in _inputHandlers)
		{
			handler.SetNavigationAvailability(canGoNext, canGoPrevious);
		}
	}

	private bool IsInteractionLocked()
	{
		return _isAnimating || _isModelTransitioning;
	}

	private void UpdateInteractionState()
	{
		var canInteract = !IsInteractionLocked();
		UpdateInputHandlersEnabled(canInteract);
		UpdateNavigationStates();
	}

	private void UpdateInputHandlersEnabled(bool isInputEnabled)
	{
		foreach (var handler in _inputHandlers)
		{
			handler.IsEnabled = isInputEnabled;
			if (!isInputEnabled)
			{
				handler.ResetInputState();
			}
		}
	}

	private async Task AnimateToNext()
	{
		_isAnimating = true;
		UpdateInteractionState();

		if (_previousRect == null || _currentRect == null || _nextRect == null)
		{
			FinishAnimation();
			return;
		}

		var sequence = DOTween.Sequence();
		sequence.Append(_previousRect.DOLocalMoveY(ActualImageSpacing * 2f, _animationDuration).SetEase(_animationEase));
		sequence.Join(_currentRect.DOLocalMoveY(ActualImageSpacing, _animationDuration).SetEase(_animationEase));
		sequence.Join(_nextRect.DOLocalMoveY(0f, _animationDuration).SetEase(_animationEase));

		await sequence.AsyncWaitForCompletion();

		FinishAnimation();
	}

	private async Task AnimateToPrevious()
	{
		_isAnimating = true;
		UpdateInteractionState();

		if (_previousRect == null || _currentRect == null || _nextRect == null)
		{
			FinishAnimation();
			return;
		}

		var sequence = DOTween.Sequence();
		sequence.Append(_previousRect.DOLocalMoveY(0f, _animationDuration).SetEase(_animationEase));
		sequence.Join(_currentRect.DOLocalMoveY(-ActualImageSpacing, _animationDuration).SetEase(_animationEase));
		sequence.Join(_nextRect.DOLocalMoveY(-ActualImageSpacing * 2f, _animationDuration).SetEase(_animationEase));

		await sequence.AsyncWaitForCompletion();

		FinishAnimation();
	}

	private void FinishAnimation()
	{
		ResetLayoutPositions(0f);
		_isAnimating = false;
		UpdateGameViews(true);
		UpdateInteractionState();
	}

	private void ResetLayoutPositions(float duration = 0.2f)
	{
		MoveSlot(_previousRect, ActualImageSpacing, duration);
		MoveSlot(_currentRect, 0f, duration);
		MoveSlot(_nextRect, -ActualImageSpacing, duration);
	}

	private void MoveSlot(RectTransform rect, float targetY, float duration)
	{
		if (rect == null)
		{
			return;
		}

		rect.DOKill();

		if (duration <= 0f)
		{
			rect.localPosition = new Vector3(0, targetY, 0);
			return;
		}

		rect.DOLocalMove(new Vector3(0, targetY, 0), duration);
	}

	private void DisposeSlotBinding(ref IDisposable disposable)
	{
		disposable?.Dispose();
		disposable = null;
	}

	private void DisposeAllViews()
	{
		if (_viewInstances.Count > 0)
		{
			foreach (var view in _viewInstances.Values)
			{
				if (view != null && !view.IsDisposed)
				{
					view.Dispose();
				}
			}
		}

		_viewInstances.Clear();
		_activeViewModels.Clear();
		_inactiveViewModelsBuffer.Clear();

		_previousViewInstance = null;
		_currentViewInstance = null;
		_nextViewInstance = null;
	}
}
}
