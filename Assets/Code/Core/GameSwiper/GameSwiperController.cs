using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.GamesLoader;
using Code.Generated.Addressables;
using InGameLogger;
using LightDI.Runtime;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.GameSwiper
{
/// <summary>
/// Controller that connects GameSwiper UI with IGameProvider business logic
/// Handles communication between visual component and game system
/// </summary>
public class GameSwiperController : IDisposable
{
	private readonly IShortGameServiceProvider _shortGameServiceProvider;
	private readonly IInGameLogger _logger;
	private readonly IResourceLoader _resourceLoader;
	private readonly Transform _uiRoot;
	private GameSwiper _gameSwiper;
	private bool _isInitialized;
	private bool _isTransitioning;
	private CancellationTokenSource _cancellationTokenSource;
	private bool _disposed;

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
	/// Load UI and setup connections
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
			_gameSwiper = instance.GetComponent<GameSwiper>();

			if (_gameSwiper == null)
			{
				_logger.LogError("GameSwiper component not found on prefab");
				Object.Destroy(instance);
				return;
			}

			_gameSwiper.OnNextGameRequested += HandleNextGameRequested;
			_gameSwiper.OnPreviousGameRequested += HandlePreviousGameRequested;

			// Wait for current game to be ready
			if (!_shortGameServiceProvider.IsCurrentGameReady)
			{
				_logger.Log("Waiting for initial game to be ready...");
				_gameSwiper.SetLoadingState(true);

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
			UpdateSwiperState();
			
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

		if (_gameSwiper != null)
		{
			_gameSwiper.OnNextGameRequested -= HandleNextGameRequested;
			_gameSwiper.OnPreviousGameRequested -= HandlePreviousGameRequested;
		}
	}

	/// <summary>
	/// Handle next game request from UI
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
					_gameSwiper.SetLoadingState(true);
				}

				// Animate UI transition first
				await _gameSwiper.AnimateToNext();

				// Then switch game logic
				var success = await _shortGameServiceProvider.SwipeToNextGameAsync(cts.Token);
				
				if (!success)
				{
					_logger.LogError("Failed to switch to next game");
					// Rollback animation
					await _gameSwiper.AnimateToPrevious();
					return;
				}

				// Update UI with new textures
				UpdateSwiperState();
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
			_gameSwiper.SetLoadingState(false);
			_isTransitioning = false;
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
					_gameSwiper.SetLoadingState(true);
				}

				// Animate UI transition first
				await _gameSwiper.AnimateToPrevious();

				// Then switch game logic
				var success = await _shortGameServiceProvider.SwipeToPreviousGameAsync(cts.Token);
				
				if (!success)
				{
					_logger.LogError("Failed to switch to previous game");
					// Rollback animation
					await _gameSwiper.AnimateToNext();
					return;
				}

				// Update UI with new textures
				UpdateSwiperState();
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
			_gameSwiper.SetLoadingState(false);
			_isTransitioning = false;
		}
	}

	/// <summary>
	/// Update swiper visual state from game provider
	/// </summary>
	private void UpdateSwiperState()
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
		_gameSwiper.UpdateTextures(previousRT, currentRT, nextRT);

		// Update navigation states for all input handlers
		_gameSwiper.UpdateNavigationStates(
			_shortGameServiceProvider.HasNextGame,
			_shortGameServiceProvider.HasPreviousGame
		);
		
		// Update loading state based on current game readiness
		var showLoading = !_shortGameServiceProvider.IsCurrentGameReady;
		_gameSwiper.SetLoadingState(showLoading);
	}
}
}