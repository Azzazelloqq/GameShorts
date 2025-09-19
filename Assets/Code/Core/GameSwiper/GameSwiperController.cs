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

				while (!_shortGameServiceProvider.IsCurrentGameReady)
				{
					await Task.Delay(100, cancellationToken);
				}
			}

			UpdateSwiperState();

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
		_gameSwiper.OnNextGameRequested -= HandleNextGameRequested;
		_gameSwiper.OnPreviousGameRequested -= HandlePreviousGameRequested;
	}

	/// <summary>
	/// Handle next game request from UI
	/// </summary>
	private async void HandleNextGameRequested()
	{
		if (_isTransitioning)
		{
			return;
		}

		_isTransitioning = true;

		try
		{
			if (!_shortGameServiceProvider.HasNextGame)
			{
				_logger.LogError("No next game available");
				return;
			}

			if (!_shortGameServiceProvider.IsNextGameReady)
			{
				_gameSwiper.SetLoadingState(true);
				
				var timeout = 10000; 
				var elapsed = 0;
				while (!_shortGameServiceProvider.IsNextGameReady && elapsed < timeout)
				{
					await Task.Delay(100);
					elapsed += 100;
				}
				
				if (!_shortGameServiceProvider.IsNextGameReady)
				{
					_logger.LogError($"Next game failed to be ready in {timeout}ms");
					return;
				}
			}

			// Animate UI transition
			await _gameSwiper.AnimateToNext();

			// Then switch game logic
			await _shortGameServiceProvider.SwipeToNextGameAsync();

			// Update UI with new textures
			UpdateSwiperState();
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
	private async void HandlePreviousGameRequested()
	{
		if (_isTransitioning)
		{
			return;
		}

		_isTransitioning = true;

		try
		{
			if (!_shortGameServiceProvider.HasPreviousGame)
			{
				return;
			}

			if (!_shortGameServiceProvider.IsPreviousGameReady)
			{
				_gameSwiper.SetLoadingState(true);

				var timeout = 10000; // 10 seconds timeout
				var elapsed = 0;
				while (!_shortGameServiceProvider.IsPreviousGameReady && elapsed < timeout)
				{
					await Task.Delay(100);
					elapsed += 100;
				}

				if (!_shortGameServiceProvider.IsPreviousGameReady)
				{
					_logger.LogError($"Previous game failed to be ready in {timeout}ms");
					return;
				}
			}

			await _gameSwiper.AnimateToPrevious();

			await _shortGameServiceProvider.SwipeToPreviousGameAsync();

			UpdateSwiperState();
		}
		catch (OperationCanceledException)
		{
			throw;
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
		// Update textures
		_gameSwiper.UpdateTextures(
			_shortGameServiceProvider.PreviousGameRenderTexture,
			_shortGameServiceProvider.CurrentGameRenderTexture,
			_shortGameServiceProvider.NextGameRenderTexture
		);

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