using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using LightDI.Runtime;
using UnityEngine;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Simple bridge provider between user and game management services
/// </summary>
public class ShortGameServiceProvider : IShortGameServiceProvider
{
	public IShortGame CurrentGame => GetGameForType(_queueService?.CurrentGameType);
	public IShortGame NextGame => GetGameForType(_queueService?.NextGameType);
	public IShortGame PreviousGame => GetGameForType(_queueService?.PreviousGameType);

	public Type CurrentGameType => _queueService?.CurrentGameType;
	public Type NextGameType => _queueService?.NextGameType;
	public Type PreviousGameType => _queueService?.PreviousGameType;

	public RenderTexture CurrentGameRenderTexture => CurrentGame?.GetRenderTexture();
	public RenderTexture NextGameRenderTexture => NextGame?.GetRenderTexture();
	public RenderTexture PreviousGameRenderTexture => PreviousGame?.GetRenderTexture();

	public bool HasCurrentGame => CurrentGame != null;
	public bool HasNextGame => NextGame != null;
	public bool HasPreviousGame => PreviousGame != null;

	public bool IsCurrentGameReady => CurrentGame?.IsPreloaded ?? false;
	public bool IsNextGameReady => NextGame?.IsPreloaded ?? false;
	public bool IsPreviousGameReady => PreviousGame?.IsPreloaded ?? false;

	private readonly IInGameLogger _logger;
	private readonly IGameRegistry _gameRegistry;
	private readonly IGameQueueService _queueService;
	private readonly IGamesLoader _gamesLoader;
	private readonly ShortGameLoaderSettings _settings;
	private bool _initialized;
	private bool _disposed;
	
	internal ShortGameServiceProvider(
		[Inject] IInGameLogger logger,
		[Inject] IGameRegistry gameRegistry,
		[Inject] IGameQueueService queueService,
		[Inject] IGamesLoader gamesLoader,
		ShortGameLoaderSettings settings)
	{
		_logger = logger;
		_gameRegistry = gameRegistry;
		_queueService = queueService;
		_gamesLoader = gamesLoader;
		_settings = settings;

		_logger.Log("ShortGameServiceProvider constructed with external dependencies");
	}

	public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (_disposed)
		{
			throw new ObjectDisposedException(nameof(ShortGameServiceProvider));
		}

		if (_initialized)
		{
			return;
		}

		if (_queueService.TotalGamesCount == 0)
		{
			_logger.LogWarning("ShortGameServiceProvider.InitializeAsync called with empty registry.");
			return;
		}

		_logger.Log(
			$"Preloading short games window (radius={_settings.PreloadRadius}, timeout={_settings.ReadinessTimeout.TotalSeconds:0.#}s)");
		await _gamesLoader.PreloadWindowAsync(cancellationToken);

		if (_gamesLoader.ActiveGameType == null)
		{
			_logger.Log("No active game found, activating the first entry in the queue.");
			await _gamesLoader.ActivateNextGameAsync(cancellationToken);
		}

		FocusCurrentGameInput();
		_initialized = true;
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_logger.Log("Disposing GameProvider");
		_disposed = true;

		StopAllGames();

		_gameRegistry.Dispose();
		_queueService.Dispose();
		_gamesLoader.Dispose();
	}

	public void StartCurrentGame()
	{
		StartGameForType(_queueService?.CurrentGameType, "current");
		FocusCurrentGameInput();
	}

	public void StartNextGame()
	{
		StartGameForType(_queueService?.NextGameType, "next");
	}

	public void StartPreviousGame()
	{
		StartGameForType(_queueService?.PreviousGameType, "previous");
	}

	public void PauseCurrentGame()
	{
		if (CurrentGame != null)
		{
			_logger.Log("Pausing current game");
			CurrentGame.PauseGame();
		}
	}

	public void UnpauseCurrentGame()
	{
		if (CurrentGame != null)
		{
			_logger.Log("Unpausing current game");
			CurrentGame.UnpauseGame();
		}
	}

	public void PauseNextGame()
	{
		if (NextGame != null)
		{
			_logger.Log("Pausing next game");
			NextGame.PauseGame();
		}
	}

	public void UnpauseNextGame()
	{
		if (NextGame != null)
		{
			_logger.Log("Unpausing next game");
			NextGame.UnpauseGame();
		}
	}

	public void PausePreviousGame()
	{
		if (PreviousGame != null)
		{
			_logger.Log("Pausing previous game");
			PreviousGame.PauseGame();
		}
	}

	public void UnpausePreviousGame()
	{
		if (PreviousGame != null)
		{
			_logger.Log("Unpausing previous game");
			PreviousGame.UnpauseGame();
		}
	}

	public void PauseAllGames()
	{
		_logger.Log("Pausing all games");
		PauseCurrentGame();
		PauseNextGame();
		PausePreviousGame();
	}

	public void UnpauseAllGames()
	{
		_logger.Log("Unpausing all games");
		UnpauseCurrentGame();
		UnpauseNextGame();
		UnpausePreviousGame();
	}

	public void StopCurrentGame()
	{
		if (CurrentGame != null)
		{
			_logger.Log("Stopping current game");
			CurrentGame.StopGame();
		}
	}

	public void StopNextGame()
	{
		if (NextGame != null)
		{
			_logger.Log("Stopping next game");
			NextGame.StopGame();
		}
	}

	public void StopPreviousGame()
	{
		if (PreviousGame != null)
		{
			_logger.Log("Stopping previous game");
			PreviousGame.StopGame();
		}
	}

	public void StopAllGames()
	{
		_logger.Log("Stopping all games");
		StopCurrentGame();
		StopNextGame();
		StopPreviousGame();
	}

	public void EnableCurrentGameInput()
	{
		if (CurrentGame != null)
		{
			_logger.Log("Enabling current game input");
			CurrentGame.EnableInput();
		}
	}

	public void DisableCurrentGameInput()
	{
		if (CurrentGame != null)
		{
			_logger.Log("Disabling current game input");
			CurrentGame.DisableInput();
		}
	}

	public void EnableNextGameInput()
	{
		if (NextGame != null)
		{
			_logger.Log("Enabling next game input");
			NextGame.EnableInput();
		}
	}

	public void DisableNextGameInput()
	{
		if (NextGame != null)
		{
			_logger.Log("Disabling next game input");
			NextGame.DisableInput();
		}
	}

	public void EnablePreviousGameInput()
	{
		if (PreviousGame != null)
		{
			_logger.Log("Enabling previous game input");
			PreviousGame.EnableInput();
		}
	}

	public void DisablePreviousGameInput()
	{
		if (PreviousGame != null)
		{
			_logger.Log("Disabling previous game input");
			PreviousGame.DisableInput();
		}
	}

	public void EnableAllGamesInput()
	{
		_logger.Log("Enabling all games input");
		EnableCurrentGameInput();
		EnableNextGameInput();
		EnablePreviousGameInput();
	}

	public void DisableAllGamesInput()
	{
		_logger.Log("Disabling all games input");
		DisableCurrentGameInput();
		DisableNextGameInput();
		DisablePreviousGameInput();
	}

	public async ValueTask UpdatePreloadedGamesAsync(CancellationToken cancellationToken = default)
	{
		if (_queueService == null || _gamesLoader == null)
		{
			return;
		}

		_logger.Log("Updating preloaded games window");
		await _gamesLoader.PreloadWindowAsync(cancellationToken);
	}

	public async Task<bool> SwipeToNextGameAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			if (!HasNextGame)
			{
				_logger.LogWarning("No next game available");
				return false;
			}

			DisableAllGamesInput();

			var result = await _gamesLoader.ActivateNextGameAsync(cancellationToken);
			if (!result)
			{
				_logger.LogError("Failed to activate next game through loader");
				EnableCurrentGameInput();
				return false;
			}

			FocusCurrentGameInput();

			_logger.Log("Successfully switched to next game");
			return true;
		}
		catch (OperationCanceledException)
		{
			_logger.Log("SwipeToNextGameAsync was cancelled");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error switching to next game: {ex.Message}");
			return false;
		}
	}

	public async Task<bool> SwipeToPreviousGameAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			if (!HasPreviousGame)
			{
				_logger.LogWarning("No previous game available");
				return false;
			}

			DisableAllGamesInput();

			var result = await _gamesLoader.ActivatePreviousGameAsync(cancellationToken);
			if (!result)
			{
				_logger.LogError("Failed to activate previous game through loader");
				EnableCurrentGameInput();
				return false;
			}

			FocusCurrentGameInput();

			_logger.Log("Successfully switched to previous game");
			return true;
		}
		catch (OperationCanceledException)
		{
			_logger.Log("SwipeToPreviousGameAsync was cancelled");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error switching to previous game: {ex.Message}");
			return false;
		}
	}

	public ValueTask InitializeSwiperUIAsync(Transform uiRoot, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	private IShortGame GetGameForType(Type gameType)
	{
		if (gameType == null || _gamesLoader == null)
		{
			return null;
		}

		return _gamesLoader.GetGame(gameType);
	}

	private void StartGameForType(Type gameType, string slot)
	{
		if (gameType == null)
		{
			return;
		}

		_logger.Log($"Starting {slot} game via loader");

		if (!_gamesLoader.StartPreloadedGame(gameType))
		{
			GetGameForType(gameType)?.StartGame();
		}
	}

	private void FocusCurrentGameInput()
	{
		EnableCurrentGameInput();
		DisableNextGameInput();
		DisablePreviousGameInput();
	}
}
}