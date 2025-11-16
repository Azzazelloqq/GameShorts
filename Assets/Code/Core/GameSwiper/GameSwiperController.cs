using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.GamesLoader;
using Code.Core.GameSwiper.MVVM.Models;
using Code.Core.GameSwiper.MVVM.ViewModels;
using Code.Core.GameSwiper.MVVM.Views;
using Code.Generated.Addressables;
using InGameLogger;
using LightDI.Runtime;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.GameSwiper
{
/// <summary>
/// Controller that manages the MVVM-based GameSwiper.
/// Creates and initializes Model, ViewModel, and View components.
/// </summary>
public class GameSwiperController : IDisposable
{
	private readonly IShortGameServiceProvider _shortGameServiceProvider;
	private readonly IInGameLogger _logger;
	private readonly IResourceLoader _resourceLoader;
	private readonly Transform _uiRoot;
	
	// MVVM components
	private GameSwiperModel _model;
	private GameSwiperViewModel _viewModel;
	private GameSwiperView _view;
	
	// Legacy support (to be removed later)
	private GameSwiper _legacySwiper;
	private bool _useLegacy = false; // Can be toggled for migration
	
	private bool _isInitialized;
	private CancellationTokenSource _cancellationTokenSource;
	private bool _disposed;
	private bool _isTransitioning;

	public GameSwiperController(
		Transform uiRoot,
		IShortGameServiceProvider shortGameServiceProvider,
		[Inject] IInGameLogger logger,
		[Inject] IResourceLoader resourceLoader)
	{
		_uiRoot = uiRoot ?? throw new ArgumentNullException(nameof(uiRoot));
		_shortGameServiceProvider = shortGameServiceProvider ?? throw new ArgumentNullException(nameof(shortGameServiceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
		_cancellationTokenSource = new CancellationTokenSource();
	}

	/// <summary>
	/// Load UI and setup MVVM connections
	/// </summary>
	public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (_isInitialized)
		{
			_logger.LogWarning("GameSwiperController already initialized");
			return;
		}

		try
		{
			if (_useLegacy)
			{
				// Use legacy implementation
				await InitializeLegacyAsync(cancellationToken);
			}
			else
			{
				// Use MVVM implementation
				await InitializeSwiperView(cancellationToken);
			}

			_isInitialized = true;
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to initialize GameSwiperController: {ex.Message}");
			throw;
		}
	}
	
	/// <summary>
	/// Initialize MVVM-based GameSwiper
	/// </summary>
	private async ValueTask InitializeSwiperView(CancellationToken cancellationToken)
	{
		// Create Model
		var settings = new SwiperSettings
		{
			AnimationDuration = 0.3f,
			UseScreenHeight = true,
			ImageSpacing = 1920f
		};
		_model = new GameSwiperModel(_shortGameServiceProvider, settings);
		
		// Create ViewModel
		_viewModel = new GameSwiperViewModel(_model);
		await _viewModel.InitializeAsync(cancellationToken);
		
		// Load and create View
		var prefab = await _resourceLoader.LoadResourceAsync<GameObject>(
			ResourceIdsContainer.DefaultLocalGroup.GameSwiper,
			cancellationToken);
		
		if (prefab == null)
		{
			_logger.LogError("GameSwiper prefab not found");
			return;
		}
		
		// Instantiate and get MVVM view component
		var instance = Object.Instantiate(prefab, _uiRoot);
		_view = instance.GetComponent<GameSwiperView>();
		
		if (_view == null)
		{
			// If no MVVM view, try adding it
			_view = instance.AddComponent<GameSwiperView>();
		}
		
		// Initialize View with ViewModel
		_view.Initialize(_viewModel);
		
		// Initialize ViewModel async operations
		await _viewModel.InitializeAsync(cancellationToken);
		await _view.InitializeAsync(cancellationToken);
		
		// Subscribe to ViewModel events
		_viewModel.OnNextGameStarted += OnMVVMNextGameStarted;
		_viewModel.OnPreviousGameStarted += OnMVVMPreviousGameStarted;
		
		_logger.Log("MVVM GameSwiper initialized successfully");
	}
	
	/// <summary>
	/// Initialize legacy GameSwiper (for backwards compatibility)
	/// </summary>
	private async ValueTask InitializeLegacyAsync(CancellationToken cancellationToken)
	{
		// Load the GameSwiper prefab
		var prefab = await _resourceLoader.LoadResourceAsync<GameObject>(
			ResourceIdsContainer.DefaultLocalGroup.GameSwiper,
			cancellationToken);

		if (prefab == null)
		{
			_logger.LogError("GameSwiper prefab not found");
			return;
		}

		// Instantiate and get component
		var instance = Object.Instantiate(prefab, _uiRoot);
		_legacySwiper = instance.GetComponent<GameSwiper>();

		if (_legacySwiper == null)
		{
			_logger.LogError("GameSwiper component not found on prefab");
			Object.Destroy(instance);
			return;
		}

		_legacySwiper.OnNextGameRequested += HandleNextGameRequested;
		_legacySwiper.OnPreviousGameRequested += HandlePreviousGameRequested;

		// Wait for current game to be ready
		if (!_shortGameServiceProvider.IsCurrentGameReady)
		{
			_logger.Log("Waiting for initial game to be ready...");
			_legacySwiper.SetLoadingState(true);

			int waitTime = 0;
			const int maxWaitTime = 10000; // 10 seconds timeout
			
			while (!_shortGameServiceProvider.IsCurrentGameReady && waitTime < maxWaitTime)
			{
				await Task.Delay(100, cancellationToken);
				waitTime += 100;
			}
			
			if (waitTime >= maxWaitTime)
			{
				_logger.LogWarning("Timeout waiting for initial game to be ready");
			}
		}

		// Force an initial update even if game is not ready
		UpdateLegacySwiperState();
		
		// Start the current game so it begins rendering
		if (_shortGameServiceProvider.IsCurrentGameReady)
		{
			_shortGameServiceProvider.StartCurrentGame();
			_logger.Log("Started current game");
		}
		
		// Log the current state for debugging
		_logger.Log($"Initial state - Current: {_shortGameServiceProvider.IsCurrentGameReady}, " +
			$"Next: {_shortGameServiceProvider.IsNextGameReady}, " +
			$"Previous: {_shortGameServiceProvider.IsPreviousGameReady}");
	}
	
	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;

		// Cancel any ongoing operations
		_cancellationTokenSource?.Cancel();
		_cancellationTokenSource?.Dispose();

		// Dispose MVVM components
		if (_view != null)
		{
			_view.Dispose();
			_view = null;
		}
		
		if (_viewModel != null)
		{
			_viewModel.OnNextGameStarted -= OnMVVMNextGameStarted;
			_viewModel.OnPreviousGameStarted -= OnMVVMPreviousGameStarted;
			_viewModel.Dispose();
			_viewModel = null;
		}
		
		if (_model != null)
		{
			_model.Dispose();
			_model = null;
		}

		// Dispose legacy components
		if (_legacySwiper != null)
		{
			_legacySwiper.OnNextGameRequested -= HandleNextGameRequested;
			_legacySwiper.OnPreviousGameRequested -= HandlePreviousGameRequested;
		}
	}

	/// <summary>
	/// Handle next game event from MVVM ViewModel
	/// </summary>
	private void OnMVVMNextGameStarted()
	{
		_logger.Log("Next game started via MVVM");
	}
	
	/// <summary>
	/// Handle previous game event from MVVM ViewModel
	/// </summary>
	private void OnMVVMPreviousGameStarted()
	{
		_logger.Log("Previous game started via MVVM");
	}
	
	/// <summary>
	/// Handle next game request from legacy UI
	/// </summary>
	private void HandleNextGameRequested()
	{
		// Fire and forget with proper error handling
		_ = HandleNextGameRequestedAsync();
	}

	/// <summary>
	/// Actual async implementation for next game switch
	/// </summary>
	private async Task HandleNextGameRequestedAsync()
	{
		if (_isTransitioning)
		{
			return;
		}

		_isTransitioning = true;

		try
		{
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token))
			{
				cts.CancelAfter(TimeSpan.FromSeconds(15)); // Overall timeout

				if (!_shortGameServiceProvider.HasNextGame)
				{
					_logger.LogWarning("No next game available");
					return;
				}

				// Show loading if game is not ready
				if (!_shortGameServiceProvider.IsNextGameReady)
				{
					_legacySwiper.SetLoadingState(true);
				}

				// FIRST: Switch game logic and prepare all data
				var success = await _shortGameServiceProvider.SwipeToNextGameAsync(cts.Token);
				
				if (!success)
				{
					_logger.LogError("Failed to switch to next game");
					_legacySwiper.ResetTransitionRequest(); // Reset the transition flag if switch failed
					return;
				}

				// SECOND: Prepare textures for animation
				var previousRT = _shortGameServiceProvider.PreviousGameRenderTexture;
				var currentRT = _shortGameServiceProvider.CurrentGameRenderTexture;
				var nextRT = _shortGameServiceProvider.NextGameRenderTexture;
				_legacySwiper.PrepareTexturesForNextAnimation(previousRT, currentRT, nextRT);

				// THIRD: Animate UI transition with prepared textures
				await _legacySwiper.AnimateToNext();
				
				// FOURTH: Update final state after animation
				UpdateLegacySwiperState();
			}
		}
		catch (OperationCanceledException)
		{
			_logger.Log("Next game switch was cancelled");
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error switching to next game: {ex.Message}");
		}
		finally
		{
			_legacySwiper.SetLoadingState(false);
			_legacySwiper.ResetTransitionRequest(); // Ensure flag is reset even if exception occurred
		}
	}

	/// <summary>
	/// Handle previous game request from UI
	/// </summary>
	private void HandlePreviousGameRequested()
	{
		// Fire and forget with proper error handling
		_ = HandlePreviousGameRequestedAsync();
	}

	/// <summary>
	/// Actual async implementation for previous game switch
	/// </summary>
	private async Task HandlePreviousGameRequestedAsync()
	{
		if (_isTransitioning)
		{
			return;
		}

		_isTransitioning = true;

		try
		{
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource(_cancellationTokenSource.Token))
			{
				cts.CancelAfter(TimeSpan.FromSeconds(15)); // Overall timeout

				if (!_shortGameServiceProvider.HasPreviousGame)
				{
					_logger.LogWarning("No previous game available");
					return;
				}

				// Show loading if game is not ready
				if (!_shortGameServiceProvider.IsPreviousGameReady)
				{
					_legacySwiper.SetLoadingState(true);
				}

				// FIRST: Switch game logic and prepare all data
				var success = await _shortGameServiceProvider.SwipeToPreviousGameAsync(cts.Token);
				
				if (!success)
				{
					_logger.LogError("Failed to switch to previous game");
					_legacySwiper.ResetTransitionRequest(); // Reset the transition flag if switch failed
					return;
				}

				// SECOND: Prepare textures for animation
				var previousRT = _shortGameServiceProvider.PreviousGameRenderTexture;
				var currentRT = _shortGameServiceProvider.CurrentGameRenderTexture;
				var nextRT = _shortGameServiceProvider.NextGameRenderTexture;
				_legacySwiper.PrepareTexturesForPreviousAnimation(previousRT, currentRT, nextRT);

				// THIRD: Animate UI transition with prepared textures
				await _legacySwiper.AnimateToPrevious();
				
				// FOURTH: Update final state after animation
				UpdateLegacySwiperState();
			}
		}
		catch (OperationCanceledException)
		{
			_logger.Log("Previous game switch was cancelled");
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error switching to previous game: {ex.Message}");
		}
		finally
		{
			_legacySwiper.SetLoadingState(false);
			_legacySwiper.ResetTransitionRequest(); // Ensure flag is reset even if exception occurred
		}
	}

	/// <summary>
	/// Update legacy swiper visual state from game provider
	/// </summary>
	private void UpdateLegacySwiperState()
	{
		// Get render textures
		var previousRT = _shortGameServiceProvider.PreviousGameRenderTexture;
		var currentRT = _shortGameServiceProvider.CurrentGameRenderTexture;
		var nextRT = _shortGameServiceProvider.NextGameRenderTexture;
		
		// Log render texture states for debugging
		_logger.Log($"UpdateSwiperState - RenderTextures: " +
			$"Previous={previousRT != null}, " +
			$"Current={currentRT != null}, " +
			$"Next={nextRT != null}");
		
		// Update textures
		_legacySwiper.UpdateTextures(previousRT, currentRT, nextRT);

		// Update navigation states for all input handlers
		_legacySwiper.UpdateNavigationStates(
			_shortGameServiceProvider.HasNextGame,
			_shortGameServiceProvider.HasPreviousGame
		);
		
		// Update loading state based on current game readiness
		var showLoading = !_shortGameServiceProvider.IsCurrentGameReady;
		_legacySwiper.SetLoadingState(showLoading);
	}
}
}