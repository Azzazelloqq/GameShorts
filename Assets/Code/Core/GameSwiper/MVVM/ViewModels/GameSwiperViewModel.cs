using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Code.Core.GamesLoader;
using Code.Core.GameStats;
using Code.Core.GameSwiper.MVVM.Models;
using Code.Core.ShortGamesCore.Source.GameCore;
using Cysharp.Threading.Tasks;
using InGameLogger;
using LightDI.Runtime;
using R3;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.ViewModels
{
internal class GameSwiperViewModel : ViewModelBase<GameSwiperModel>
{
	private readonly IInGameLogger _logger;
	private readonly IShortGameServiceProvider _gameServiceProvider;
	private readonly IGameStatsService _gameStatsService;
	public ReadOnlyReactiveProperty<int> CurrentGameIndex => model.CurrentGameIndex;
	public ReadOnlyReactiveProperty<bool> CanGoNext => model.CanGoNext;
	public ReadOnlyReactiveProperty<bool> CanGoPrevious => model.CanGoPrevious;
	public ReadOnlyReactiveProperty<bool> IsTransitioning => model.IsTransitioning;
	public ReadOnlyReactiveProperty<bool> IsLoading => model.IsLoading;
	public ReadOnlyReactiveProperty<bool> ShouldShowLoadingIndicator => _shouldShowLoadingIndicator;

	public ReadOnlyReactiveProperty<float> SwipeProgress => _swipeProgress;
	public ReadOnlyReactiveProperty<bool> IsEnabled => _isEnabled;
	public ReadOnlyReactiveProperty<SwipeDirection> LastSwipeDirection => _lastSwipeDirection;

	public ReadOnlyReactiveProperty<GameItemViewModel> PreviousGameViewModel { get; }
	public ReadOnlyReactiveProperty<GameItemViewModel> CurrentGameViewModel { get; }
	public ReadOnlyReactiveProperty<GameItemViewModel> NextGameViewModel { get; }

	public IActionAsyncCommand GoToNextCommand { get; private set; }
	public IActionAsyncCommand GoToPreviousCommand { get; private set; }
	public IActionCommand ResetSwipeCommand { get; private set; }
	public IRelayCommand<float> UpdateSwipeProgressCommand { get; private set; }
	public event Func<Task> OnNextGameStartedAsync;
	public event Func<Task> OnPreviousGameStartedAsync;
	public event Action<float> OnSwipeProgressChanged;

	private readonly ReactiveProperty<GameItemViewModel> _previousGameVM;
	private readonly ReactiveProperty<GameItemViewModel> _currentGameVM;
	private readonly ReactiveProperty<GameItemViewModel> _nextGameVM;
	private readonly Dictionary<int, GameItemViewModel> _gameViewModelCache;
	private readonly ReactiveProperty<bool> _isEnabled;
	private CancellationTokenSource _navigationCts;
	private CancellationTokenSource _loadingIndicatorCts;
	private readonly ReactiveProperty<bool> _shouldShowLoadingIndicator;
	private readonly ReactiveProperty<float> _swipeProgress;
	private readonly ReactiveProperty<SwipeDirection> _lastSwipeDirection;
	private SwipeDirection _activePreviewDirection = SwipeDirection.None;

	private const float LoadingIndicatorDelaySeconds = 0.1f;
	private const float SwipePreviewEpsilon = 0.001f;

	public GameSwiperViewModel(
		GameSwiperModel model,
		IShortGameServiceProvider gameServiceProvider,
		IGameStatsService gameStatsService,
		[Inject] IInGameLogger logger) : base(model)
	{
		_logger = logger;
		_gameServiceProvider = gameServiceProvider ?? throw new ArgumentNullException(nameof(gameServiceProvider));
		_gameStatsService = gameStatsService ?? throw new ArgumentNullException(nameof(gameStatsService));
		_swipeProgress = AddDisposable(new ReactiveProperty<float>(0f));
		_isEnabled = AddDisposable(new ReactiveProperty<bool>(true));
		_lastSwipeDirection = AddDisposable(new ReactiveProperty<SwipeDirection>(SwipeDirection.None));
		_shouldShowLoadingIndicator = AddDisposable(new ReactiveProperty<bool>(false));

		_previousGameVM = AddDisposable(new ReactiveProperty<GameItemViewModel>(null));
		_currentGameVM = AddDisposable(new ReactiveProperty<GameItemViewModel>(null));
		_nextGameVM = AddDisposable(new ReactiveProperty<GameItemViewModel>(null));

		PreviousGameViewModel = _previousGameVM;
		CurrentGameViewModel = _currentGameVM;
		NextGameViewModel = _nextGameVM;

		_gameViewModelCache = new Dictionary<int, GameItemViewModel>();
		_navigationCts = null;
	}

	protected override void OnInitialize()
	{
		InitializeSync();
		_ = InitializeGameSwiperAsync(CancellationToken.None);
	}

	protected override async ValueTask OnInitializeAsync(CancellationToken token)
	{
		InitializeSync();
		await InitializeGameSwiperAsync(token);
	}

	protected override void OnDispose()
	{
		_navigationCts?.Cancel();
		_navigationCts?.Dispose();
		_navigationCts = null;

		_gameViewModelCache.Clear();

		_loadingIndicatorCts?.Cancel();
		_loadingIndicatorCts?.Dispose();
		_loadingIndicatorCts = null;

		OnNextGameStartedAsync = null;
		OnPreviousGameStartedAsync = null;
		OnSwipeProgressChanged = null;
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		_navigationCts?.Cancel();
		_navigationCts?.Dispose();
		_navigationCts = null;

		_gameViewModelCache.Clear();

		_loadingIndicatorCts?.Cancel();
		_loadingIndicatorCts?.Dispose();
		_loadingIndicatorCts = null;

		OnNextGameStartedAsync = null;
		OnPreviousGameStartedAsync = null;
		OnSwipeProgressChanged = null;

		return default;
	}

	private void InitializeSync()
	{
		GoToNextCommand = new ActionAsyncCommand(OnGoToNext, CanGoToNext);
		GoToPreviousCommand = new ActionAsyncCommand(OnGoToPrevious, CanGoToPrevious);
		ResetSwipeCommand = new ActionCommand(OnResetSwipe);
		UpdateSwipeProgressCommand = new RelayCommand<float>(OnUpdateSwipeProgress);
		compositeDisposable.AddDisposable(GoToNextCommand);
		compositeDisposable.AddDisposable(GoToPreviousCommand);
		compositeDisposable.AddDisposable(ResetSwipeCommand);
		compositeDisposable.AddDisposable(UpdateSwipeProgressCommand);

		compositeDisposable.AddDisposable(model.PreviousGame.Subscribe(game =>
			OnSlotModelChanged(GameSlot.Previous, game)));
		compositeDisposable.AddDisposable(model.CurrentGame.Subscribe(game => OnSlotModelChanged(GameSlot.Current, game)));
		compositeDisposable.AddDisposable(model.NextGame.Subscribe(game => OnSlotModelChanged(GameSlot.Next, game)));
		compositeDisposable.AddDisposable(model.CurrentGameIndex.Subscribe(_ => UpdateNavigationState()));
		compositeDisposable.AddDisposable(model.IsTransitioning.Subscribe(OnTransitioningChanged));
		compositeDisposable.AddDisposable(model.IsLoading.Subscribe(OnModelLoadingStateChanged));
		compositeDisposable.AddDisposable(SwipeProgress);
		compositeDisposable.AddDisposable(IsEnabled);
		compositeDisposable.AddDisposable(LastSwipeDirection);
		compositeDisposable.AddDisposable(_previousGameVM);
		compositeDisposable.AddDisposable(_currentGameVM);
		compositeDisposable.AddDisposable(_nextGameVM);
		compositeDisposable.AddDisposable(ShouldShowLoadingIndicator);
	}

	private GameItemViewModel GetOrCreateGameViewModel(GameItemModel gameModel)
	{
		if (!_gameViewModelCache.ContainsKey(gameModel.Index))
		{
			var viewModel = new GameItemViewModel(gameModel, _gameStatsService, _logger);
			viewModel.Initialize();

			_gameViewModelCache[gameModel.Index] = viewModel;
			compositeDisposable.AddDisposable(viewModel);
		}

		return _gameViewModelCache[gameModel.Index];
	}

	private void OnSlotModelChanged(GameSlot slot, GameItemModel gameModel)
	{
		var slotProperty = GetSlotProperty(slot);
		var previousViewModel = slotProperty.Value;

		if (gameModel == null)
		{
			slotProperty.Value = null;
			previousViewModel?.UpdateUIVisibility(false, 0f);
			return;
		}

		var viewModel = GetOrCreateGameViewModel(gameModel);
		slotProperty.Value = viewModel;
		ApplySlotPresentation(viewModel, slot);
	}

	private ReactiveProperty<GameItemViewModel> GetSlotProperty(GameSlot slot)
	{
		return slot switch
		{
			GameSlot.Previous => _previousGameVM,
			GameSlot.Current => _currentGameVM,
			GameSlot.Next => _nextGameVM,
			_ => _currentGameVM
		};
	}

	private void ApplySlotPresentation(GameItemViewModel viewModel, GameSlot slot)
	{
		switch (slot)
		{
			case GameSlot.Current:
				viewModel.UpdateUIVisibility(true, 1f);
				break;
			case GameSlot.Previous:
			case GameSlot.Next:
				viewModel.UpdateUIVisibility(true, 0.7f);
				break;
			default:
				viewModel.UpdateUIVisibility(false, 0f);
				break;
		}
	}

	private CancellationToken CreateNavigationToken()
	{
		_navigationCts?.Cancel();
		_navigationCts?.Dispose();
		_navigationCts = new CancellationTokenSource();
		return _navigationCts.Token;
	}

	private void OnTransitioningChanged(bool isTransitioning)
	{
		_isEnabled.Value = isTransitioning;
	}

	private void OnModelLoadingStateChanged(bool isLoading)
	{
		_loadingIndicatorCts?.Cancel();
		_loadingIndicatorCts?.Dispose();
		_loadingIndicatorCts = null;

		if (!isLoading)
		{
			_shouldShowLoadingIndicator.Value = false;
			return;
		}

		_shouldShowLoadingIndicator.Value = false;
		_loadingIndicatorCts = new CancellationTokenSource();
		_ = ShowGlobalLoadingIndicatorWithDelayAsync(_loadingIndicatorCts.Token);
	}

	private async UniTask ShowGlobalLoadingIndicatorWithDelayAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(TimeSpan.FromSeconds(LoadingIndicatorDelaySeconds), token);
		}
		catch (TaskCanceledException)
		{
			return;
		}

		if (!token.IsCancellationRequested && model.IsLoading.CurrentValue)
		{
			_shouldShowLoadingIndicator.Value = true;
		}
	}

	private bool CanGoToNext()
	{
		return model.CanGoNext.CurrentValue && !model.IsTransitioning.CurrentValue;
	}

	private async UniTask OnGoToNext()
	{
		_lastSwipeDirection.Value = SwipeDirection.Up;

		var success = await NavigateAsync(
			ct => _gameServiceProvider.SwipeToNextGameAsync(ct),
			() => _gameServiceProvider.IsNextGameReady,
			() => _gameServiceProvider.HasNextGame,
			1,
			() => OnNextGameStartedAsync?.Invoke() ?? Task.CompletedTask);

		// Always reset swipe progress after an attempt to avoid leaving UI in an offset state when navigation fails.
		ResetSwipeProgress();
	}

	private bool CanGoToPrevious()
	{
		return model.CanGoPrevious.CurrentValue && !model.IsTransitioning.CurrentValue;
	}

	private async UniTask OnGoToPrevious()
	{
		_lastSwipeDirection.Value = SwipeDirection.Down;

		var success = await NavigateAsync(
			ct => _gameServiceProvider.SwipeToPreviousGameAsync(ct),
			() => _gameServiceProvider.IsPreviousGameReady,
			() => _gameServiceProvider.HasPreviousGame,
			-1,
			() => OnPreviousGameStartedAsync?.Invoke() ?? Task.CompletedTask);

		// Always reset swipe progress after an attempt to avoid leaving UI in an offset state when navigation fails.
		ResetSwipeProgress();
	}

	private async ValueTask InitializeGameSwiperAsync(CancellationToken token)
	{
		model.SetLoadingState(true);

		try
		{
			await WaitForGameReadyAsync(() => _gameServiceProvider.IsCurrentGameReady, token);

			await RefreshGamesFromServiceAsync(token);
			UpdateNavigationState();

			if (_gameServiceProvider.IsCurrentGameReady)
			{
				_gameServiceProvider.StartCurrentGame();
				model.UpdateActiveStates(model.CurrentGameIndex.CurrentValue);
			}
		}
		finally
		{
			model.SetLoadingState(false);
		}
	}

	private async UniTask WaitForGameReadyAsync(Func<bool> predicate, CancellationToken token)
	{
		if (predicate())
		{
			return;
		}

		var waitTime = 0;
		const int maxWaitTime = 10000;

		while (!predicate() && waitTime < maxWaitTime)
		{
			await Task.Delay(100, token);
			waitTime += 100;
		}
	}

	private async UniTask RefreshGamesFromServiceAsync(CancellationToken token)
	{
		var currentIndex = model.CurrentGameIndex.CurrentValue;

		var previous = _gameServiceProvider.HasPreviousGame
			? await BuildSlotStateAsync(
				currentIndex - 1,
				_gameServiceProvider.PreviousGameRenderTexture,
				_gameServiceProvider.IsPreviousGameReady,
				_gameServiceProvider.PreviousGameType,
				_gameServiceProvider.PreviousGame,
				token)
			: default;

		var current = _gameServiceProvider.HasCurrentGame
			? await BuildSlotStateAsync(
				currentIndex,
				_gameServiceProvider.CurrentGameRenderTexture,
				_gameServiceProvider.IsCurrentGameReady,
				_gameServiceProvider.CurrentGameType,
				_gameServiceProvider.CurrentGame,
				token)
			: throw new Exception("Can't get game stats");

		var next = _gameServiceProvider.HasNextGame
			? await BuildSlotStateAsync(
				currentIndex + 1,
				_gameServiceProvider.NextGameRenderTexture,
				_gameServiceProvider.IsNextGameReady,
				_gameServiceProvider.NextGameType,
				_gameServiceProvider.NextGame,
				token)
			: default;

		model.UpdateGameSlots(previous, current, next, currentIndex);
	}

	private async UniTask<GameSlotState> BuildSlotStateAsync(
		int index,
		RenderTexture renderTexture,
		bool isReady,
		Type gameType,
		IShortGame gameInstance,
		CancellationToken token)
	{
		var resolvedType = gameType ?? gameInstance?.GetType();
		GamePresentationData? presentation = null;

		if (resolvedType != null)
		{
			var stats = await _gameStatsService.GetStatsAsync(resolvedType, token);
			presentation = new GamePresentationData(resolvedType, FormatGameName(resolvedType), stats);
		}

		return new GameSlotState(index, renderTexture, isReady, presentation);
	}

	private void UpdateNavigationState()
	{
		model.UpdateNavigationState(_gameServiceProvider.HasPreviousGame, _gameServiceProvider.HasNextGame);
	}

	private static string FormatGameName(Type gameType)
	{
		return gameType?.Name ?? "Unknown Game";
	}

	private async UniTask<bool> NavigateAsync(
		Func<CancellationToken, Task<bool>> navigationAction,
		Func<bool> isTargetGameReady,
		Func<bool> hasTargetGame,
		int direction,
		Func<Task> transitionAction = null)
	{
		if (!hasTargetGame() || model.IsTransitioning.CurrentValue)
		{
			return false;
		}

		model.SetTransitionState(true);
		CancellationTokenSource navigationSource = null;

		try
		{
			if (!isTargetGameReady())
			{
				model.SetLoadingState(true);
			}

			var navigationToken = CreateNavigationToken();
			navigationSource = _navigationCts;
			bool success;

			try
			{
				success = await navigationAction(navigationToken);
			}
			catch (OperationCanceledException) when (navigationToken.IsCancellationRequested)
			{
				return false;
			}

			if (success)
			{
				var newIndex = model.CurrentGameIndex.CurrentValue + direction;
				model.UpdateCurrentGameIndex(newIndex);

				await RefreshGamesFromServiceAsync(navigationToken);
				model.UpdateActiveStates(newIndex);
				UpdateNavigationState();

				if (transitionAction != null)
				{
					await transitionAction();
				}
			}

			return success;
		}
		finally
		{
			navigationSource?.Dispose();
			_navigationCts = null;
			model.SetLoadingState(false);
			model.SetTransitionState(false);
		}
	}

	private void OnResetSwipe()
	{
		ResetSwipeProgress();
	}

	private void OnUpdateSwipeProgress(float progress)
	{
		_swipeProgress.Value = progress;
		UpdateNeighbourPreviewRendering(progress);
		OnSwipeProgressChanged?.Invoke(progress);
	}

	private void UpdateNeighbourPreviewRendering(float progress)
	{
		var direction = progress switch
		{
			> SwipePreviewEpsilon => SwipeDirection.Up,
			< -SwipePreviewEpsilon => SwipeDirection.Down,
			_ => SwipeDirection.None
		};

		if (direction == _activePreviewDirection)
		{
			return;
		}

		_activePreviewDirection = direction;

		switch (direction)
		{
			case SwipeDirection.Up:
				_gameServiceProvider.SetNeighbourRenderingEnabled(enableNext: true, enablePrevious: false);
				break;
			case SwipeDirection.Down:
				_gameServiceProvider.SetNeighbourRenderingEnabled(enableNext: false, enablePrevious: true);
				break;
			default:
				_gameServiceProvider.SetNeighbourRenderingEnabled(enableNext: false, enablePrevious: false);
				break;
		}
	}

	private void ResetSwipeProgress()
	{
		_swipeProgress.Value = 0f;
		_lastSwipeDirection.Value = SwipeDirection.None;

		// Important: ResetSwipeCommand is used when the swipe is cancelled/finished.
		// Ensure we also reset the enabled-game preview state back to "current only".
		_activePreviewDirection = SwipeDirection.None;
		_gameServiceProvider.SetNeighbourRenderingEnabled(enableNext: false, enablePrevious: false);
	}

	public void HandleSwipeInput(float deltaY)
	{
		if (!IsEnabled.CurrentValue)
		{
			return;
		}

		var progress = Mathf.Clamp(SwipeProgress.CurrentValue + deltaY, -1f, 1f);
		UpdateSwipeProgressCommand.Execute(progress);

		const float swipeThreshold = 0.4f;

		if (progress > swipeThreshold && CanGoToNext())
		{
			_ = GoToNextCommand.ExecuteAsync();
		}
		else if (progress < -swipeThreshold && CanGoToPrevious())
		{
			_ = GoToPreviousCommand.ExecuteAsync();
		}
	}
}

internal enum GameSlot
{
	Previous,
	Current,
	Next
}

internal enum SwipeDirection
{
	None,
	Up,
	Down
}
}