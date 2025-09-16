using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.GamesLoader;
using Code.Core.Tools;
using Code.Generated.Addressables;
using InGameLogger;
using LightDI.Runtime;
using R3;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.GameSwiper
{
/// <summary>
/// Controller for managing the connection between GameSwiper and GameSwiperView
/// </summary>
public struct Ctx
{
	public Transform PlaceForAllUi;
	public IGameProvider GameProvider; // Game provider reference
}

public class GameSwiperController : BaseDisposable
{
	private readonly IGameProvider _gameProvider;
	private readonly GameSwiperService _swiperService;
	private readonly IInGameLogger _logger;
	private CancellationTokenSource _cancellationTokenSource;
	private readonly IResourceLoader _resourceLoader;
	private readonly Ctx _ctx;
	private GameSwiperView _gameSwiperView;
	private readonly ReactiveTrigger _onPreviewGame;
	private readonly ReactiveTrigger _onNextGame;
	private bool _isInitialized;

	public GameSwiperController(Ctx ctx, [Inject] IInGameLogger logger, [Inject] IResourceLoader resourceLoader)
	{
		_ctx = ctx;
		_gameProvider = ctx.GameProvider ?? throw new ArgumentNullException(nameof(ctx.GameProvider));
		_cancellationTokenSource = new CancellationTokenSource();
		_logger = logger;
		_resourceLoader = resourceLoader;

		// Create the swipe management service
		_swiperService = new GameSwiperService(_gameProvider, _logger);
		_swiperService.OnTransitionStateChanged += HandleTransitionStateChanged;
		_swiperService.OnGameChanged += HandleGameChanged;

		_onNextGame = new ReactiveTrigger();
		_onPreviewGame = new ReactiveTrigger();

		// Subscriptions will be set up after initialization
	}

	/// <summary>
	/// Async initialization of the controller
	/// </summary>
	public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (_isInitialized)
		{
			_logger.LogWarning("GameSwiperController is already initialized");
			return;
		}

		try
		{
			_logger.Log("Initializing GameSwiperController");

			// Load UI
			await LoadGameSwiperViewAsync(cancellationToken);

			// Set up event subscriptions
			AddDispose(_onNextGame.Subscribe(HandleNextGameRequested));
			AddDispose(_onPreviewGame.Subscribe(HandlePreviousGameRequested));

			// Update navigation button states
			if (_gameSwiperView != null && _gameProvider?.QueueService != null)
			{
				_gameSwiperView.UpdateNavigationButtons(
					_gameProvider.QueueService.HasNext,
					_gameProvider.QueueService.HasPrevious);

				// Set initial preview textures
				UpdatePreviewTextures();
			}

			_isInitialized = true;
			_logger.Log("GameSwiperController initialized successfully");
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to initialize GameSwiperController: {ex.Message}");
			throw;
		}
	}

	private async ValueTask LoadGameSwiperViewAsync(CancellationToken cancellationToken)
	{
		try
		{
			// Load GameSwiperView prefab
			var swiperViewPrefab = await _resourceLoader.LoadResourceAsync<GameObject>(
				ResourceIdsContainer.DefaultLocalGroup.GameSwiper, cancellationToken);

			if (swiperViewPrefab != null)
			{
				// Create UI instance
				var disposables = new CompositeDisposable();
				var swiperViewInstance = AddComponent(Object.Instantiate(swiperViewPrefab, _ctx.PlaceForAllUi));
				_gameSwiperView = swiperViewInstance.GetComponent<GameSwiperView>();
				_gameSwiperView.SetCtx(new GameSwiperView.Ctx
				{
					Disposables = disposables,
					OnNextGameRequested = _onNextGame,
					OnPreviousGameRequested = _onPreviewGame
				});
			}
			else
			{
				_logger.LogWarning(
					$"GameSwiperView prefab not found at path: {ResourceIdsContainer.DefaultLocalGroup.GameSwiper}");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to load GameSwiperView: {ex.Message}");
		}
	}

	private async void HandleNextGameRequested()
	{
		if (!_isInitialized)
		{
			_logger.LogWarning("GameSwiperController is not initialized");
			return;
		}

		if (!_swiperService.CanSwipeNext)
		{
			return;
		}

		try
		{
			_logger.Log("GameSwiperController: Next game requested");

			// Get RenderTextures for animation
			var (current, next, _) = _swiperService.GetRenderTextures();

			// Start transition in a separate task
			var transitionTask = Task.Run(async () =>
			{
				await _swiperService.SwipeToNextGameAsync(_cancellationTokenSource.Token);
			});

			// Run animation in parallel with transition logic
			if (_gameSwiperView != null && current != null && next != null)
			{
				await _gameSwiperView.AnimateTransition(
					current,
					next,
					GameSwiperView.TransitionDirection.Next);
			}

			// Wait for transition logic to complete
			await transitionTask;

			// Update displayed texture
			_gameSwiperView?.SetCurrentGameTexture(_gameProvider.CurrentGameRenderTexture);

			// Update preview textures for next swipe
			UpdatePreviewTextures();

			_logger.Log("GameSwiperController: Successfully switched to next game");
		}
		catch (OperationCanceledException)
		{
			_logger.Log("GameSwiperController: Next game operation was cancelled");
		}
		catch (Exception ex)
		{
			_logger.LogError($"GameSwiperController: Error switching to next game: {ex.Message}");
		}
	}

	private async void HandlePreviousGameRequested()
	{
		if (!_isInitialized)
		{
			_logger.LogWarning("GameSwiperController is not initialized");
			return;
		}

		if (!_swiperService.CanSwipePrevious)
		{
			return;
		}

		try
		{
			_logger.Log("GameSwiperController: Previous game requested");

			// Get RenderTextures for animation
			var (current, _, previous) = _swiperService.GetRenderTextures();

			// Start transition in a separate task
			var transitionTask = Task.Run(async () =>
			{
				await _swiperService.SwipeToPreviousGameAsync(_cancellationTokenSource.Token);
			});

			// Run animation in parallel with transition logic
			if (_gameSwiperView != null && current != null && previous != null)
			{
				await _gameSwiperView.AnimateTransition(
					current,
					previous,
					GameSwiperView.TransitionDirection.Previous);
			}

			// Wait for transition logic to complete
			await transitionTask;

			// Update displayed texture
			_gameSwiperView?.SetCurrentGameTexture(_gameProvider.CurrentGameRenderTexture);

			// Update preview textures for next swipe
			UpdatePreviewTextures();

			_logger.Log("GameSwiperController: Successfully switched to previous game");
		}
		catch (OperationCanceledException)
		{
			_logger.Log("GameSwiperController: Previous game operation was cancelled");
		}
		catch (Exception ex)
		{
			_logger.LogError($"GameSwiperController: Error switching to previous game: {ex.Message}");
		}
	}

	private void HandleTransitionStateChanged(GameSwiperService.TransitionState state)
	{
		_logger.Log($"GameSwiperController: Transition state changed to {state}");

		// Update UI based on state
		switch (state)
		{
			case GameSwiperService.TransitionState.Preparing:
			case GameSwiperService.TransitionState.Animating:
			case GameSwiperService.TransitionState.Completing:
				_gameSwiperView?.SetLoadingState(true);
				break;
			case GameSwiperService.TransitionState.Idle:
				_gameSwiperView?.SetLoadingState(false);
				_gameSwiperView?.UpdateNavigationButtons(
					_gameProvider.QueueService.HasNext,
					_gameProvider.QueueService.HasPrevious);
				break;
		}
	}

	private void HandleGameChanged(Type from, Type to)
	{
		_logger.Log($"GameSwiperController: Game changed from {from?.Name ?? "null"} to {to?.Name ?? "null"}");

		// Update current texture
		if (_gameProvider.CurrentGameRenderTexture != null)
		{
			_gameSwiperView?.SetCurrentGameTexture(_gameProvider.CurrentGameRenderTexture);
		}

		// Update preview textures when game changes
		UpdatePreviewTextures();
	}

	private void UpdatePreviewTextures()
	{
		if (_gameSwiperView == null || _gameProvider == null)
		{
			return;
		}

		var nextTexture = _gameProvider.NextGameRenderTexture;
		var previousTexture = _gameProvider.PreviousGameRenderTexture;

		_gameSwiperView.SetPreviewTextures(nextTexture, previousTexture);
	}

	protected override void OnDispose()
	{
		_cancellationTokenSource?.Cancel();
		_cancellationTokenSource?.Dispose();

		if (_swiperService != null)
		{
			_swiperService.OnTransitionStateChanged -= HandleTransitionStateChanged;
			_swiperService.OnGameChanged -= HandleGameChanged;
			_swiperService.Dispose();
		}

		base.OnDispose();
	}
}
}