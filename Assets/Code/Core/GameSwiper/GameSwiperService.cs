using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.GamesLoader;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using UnityEngine;

namespace Code.Core.GameSwiper
{
/// <summary>
/// Service for managing game switching with RenderTexture animation support
/// </summary>
public class GameSwiperService : IDisposable
{
	private readonly IGameProvider _gameProvider;
	private readonly IInGameLogger _logger;
	private bool _isTransitioning;
	private bool _disposed;

	public bool IsTransitioning => _isTransitioning;
	public bool CanSwipeNext => !_isTransitioning && _gameProvider?.QueueService?.HasNext == true;
	public bool CanSwipePrevious => !_isTransitioning && _gameProvider?.QueueService?.HasPrevious == true;

	// Events for UI notifications
	public event Action<TransitionState> OnTransitionStateChanged;
	public event Action<Type, Type> OnGameChanged;

	public enum TransitionState
	{
		Idle,
		Preparing,
		Animating,
		Completing
	}

	public GameSwiperService(IGameProvider gameProvider, IInGameLogger logger)
	{
		_gameProvider = gameProvider ?? throw new ArgumentNullException(nameof(gameProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	/// <summary>
	/// Performs transition to the next game
	/// </summary>
	public async Task<bool> SwipeToNextGameAsync(CancellationToken cancellationToken = default)
	{
		if (!CanSwipeNext)
		{
			_logger.LogWarning("Cannot swipe to next game - either transitioning or no next game available");
			return false;
		}

		try
		{
			_isTransitioning = true;
			OnTransitionStateChanged?.Invoke(TransitionState.Preparing);

			var currentGameType = _gameProvider.QueueService.CurrentGameType;
			var nextGameType = _gameProvider.QueueService.NextGameType;

			_logger.Log($"Starting transition from {currentGameType?.Name ?? "null"} to {nextGameType?.Name ?? "null"}");

			// 1. Preparation: get RenderTextures and pause current game
			var currentRenderTexture = _gameProvider.CurrentGameRenderTexture;
			var nextRenderTexture = await PrepareNextGameAsync(cancellationToken);

			if (nextRenderTexture == null)
			{
				_logger.LogError("Failed to get render texture for next game");
				return false;
			}

			// 2. Pause current game
			_gameProvider.PauseCurrentGame();

			// 3. Notify UI about animation readiness
			OnTransitionStateChanged?.Invoke(TransitionState.Animating);

			// UI should perform animation independently
			// Here we just wait for a small delay to simulate animation
			await Task.Delay(500, cancellationToken);

			// 4. Complete transition
			OnTransitionStateChanged?.Invoke(TransitionState.Completing);

			// Stop current game
			_gameProvider.StopCurrentGame();

			// Move to next game in queue
			_gameProvider.QueueService.MoveNext();

			// Load and start new game
			var newGame = await _gameProvider.GamesLoader.LoadGameAsync(
				_gameProvider.QueueService.CurrentGameType,
				cancellationToken);

			if (newGame != null)
			{
				_gameProvider.StartCurrentGame();

				// Update preloaded games
				await _gameProvider.UpdatePreloadedGamesAsync(cancellationToken);

				_logger.Log("Successfully completed transition to next game");
				OnGameChanged?.Invoke(currentGameType, nextGameType);
				return true;
			}

			_logger.LogError("Failed to load next game");
			return false;
		}
		catch (OperationCanceledException)
		{
			_logger.Log("Swipe to next game was cancelled");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error during swipe to next game: {ex.Message}");
			return false;
		}
		finally
		{
			_isTransitioning = false;
			OnTransitionStateChanged?.Invoke(TransitionState.Idle);
		}
	}

	/// <summary>
	/// Performs transition to the previous game
	/// </summary>
	public async Task<bool> SwipeToPreviousGameAsync(CancellationToken cancellationToken = default)
	{
		if (!CanSwipePrevious)
		{
			_logger.LogWarning("Cannot swipe to previous game - either transitioning or no previous game available");
			return false;
		}

		try
		{
			_isTransitioning = true;
			OnTransitionStateChanged?.Invoke(TransitionState.Preparing);

			var currentGameType = _gameProvider.QueueService.CurrentGameType;
			var previousGameType = _gameProvider.QueueService.PreviousGameType;

			_logger.Log(
				$"Starting transition from {currentGameType?.Name ?? "null"} to {previousGameType?.Name ?? "null"}");

			// 1. Preparation: get RenderTextures and pause current game
			var currentRenderTexture = _gameProvider.CurrentGameRenderTexture;
			var previousRenderTexture = await PreparePreviousGameAsync(cancellationToken);

			if (previousRenderTexture == null)
			{
				_logger.LogError("Failed to get render texture for previous game");
				return false;
			}

			// 2. Pause current game
			_gameProvider.PauseCurrentGame();

			// 3. Notify UI about animation readiness
			OnTransitionStateChanged?.Invoke(TransitionState.Animating);

			// UI should perform animation independently
			// Here we just wait for a small delay to simulate animation
			await Task.Delay(500, cancellationToken);

			// 4. Complete transition
			OnTransitionStateChanged?.Invoke(TransitionState.Completing);

			// Stop current game
			_gameProvider.StopCurrentGame();

			// Move to previous game in queue
			_gameProvider.QueueService.MovePrevious();

			// Load and start new game
			var newGame = await _gameProvider.GamesLoader.LoadGameAsync(
				_gameProvider.QueueService.CurrentGameType,
				cancellationToken);

			if (newGame != null)
			{
				_gameProvider.StartCurrentGame();

				// Update preloaded games
				await _gameProvider.UpdatePreloadedGamesAsync(cancellationToken);

				_logger.Log("Successfully completed transition to previous game");
				OnGameChanged?.Invoke(currentGameType, previousGameType);
				return true;
			}

			_logger.LogError("Failed to load previous game");
			return false;
		}
		catch (OperationCanceledException)
		{
			_logger.Log("Swipe to previous game was cancelled");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error during swipe to previous game: {ex.Message}");
			return false;
		}
		finally
		{
			_isTransitioning = false;
			OnTransitionStateChanged?.Invoke(TransitionState.Idle);
		}
	}

	/// <summary>
	/// Prepares the next game and returns its RenderTexture
	/// </summary>
	private async Task<RenderTexture> PrepareNextGameAsync(CancellationToken cancellationToken)
	{
		if (!_gameProvider.IsNextGameReady)
		{
			_logger.Log("Next game is not ready, preloading...");
			await _gameProvider.GamesLoader.PreloadGameAsync(
				_gameProvider.QueueService.NextGameType,
				cancellationToken);
		}

		return _gameProvider.NextGameRenderTexture;
	}

	/// <summary>
	/// Prepares the previous game and returns its RenderTexture
	/// </summary>
	private async Task<RenderTexture> PreparePreviousGameAsync(CancellationToken cancellationToken)
	{
		if (!_gameProvider.IsPreviousGameReady)
		{
			_logger.Log("Previous game is not ready, preloading...");
			await _gameProvider.GamesLoader.PreloadGameAsync(
				_gameProvider.QueueService.PreviousGameType,
				cancellationToken);
		}

		return _gameProvider.PreviousGameRenderTexture;
	}

	/// <summary>
	/// Gets current RenderTextures for display
	/// </summary>
	public (RenderTexture current, RenderTexture next, RenderTexture previous) GetRenderTextures()
	{
		return (
			_gameProvider.CurrentGameRenderTexture,
			_gameProvider.NextGameRenderTexture,
			_gameProvider.PreviousGameRenderTexture
		);
	}

	/// <summary>
	/// Pauses all games
	/// </summary>
	public void PauseAll()
	{
		_logger.Log("Pausing all games");
		_gameProvider.PauseAllGames();
	}

	/// <summary>
	/// Resumes the current game
	/// </summary>
	public void ResumeCurrent()
	{
		_logger.Log("Resuming current game");
		_gameProvider.UnpauseCurrentGame();
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_disposed = true;
		_logger.Log("Disposing GameSwiperService");
	}
}
}