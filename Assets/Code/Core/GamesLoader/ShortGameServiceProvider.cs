using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.Factory;
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
	private readonly IInGameLogger _logger;
	private readonly IGameRegistry _gameRegistry;
	private readonly IGameQueueService _queueService;
	private readonly IGamesLoader _gamesLoader;
	private bool _disposed;

	public IShortGame CurrentGame => GetGameForType(_queueService?.CurrentGameType);
	public IShortGame NextGame => GetGameForType(_queueService?.NextGameType);
	public IShortGame PreviousGame => GetGameForType(_queueService?.PreviousGameType);

	public RenderTexture CurrentGameRenderTexture => CurrentGame?.GetRenderTexture();
	public RenderTexture NextGameRenderTexture => NextGame?.GetRenderTexture();
	public RenderTexture PreviousGameRenderTexture => PreviousGame?.GetRenderTexture();

	public bool HasCurrentGame => CurrentGame != null;
	public bool HasNextGame => NextGame != null;
	public bool HasPreviousGame => PreviousGame != null;

	public bool IsCurrentGameReady => CurrentGame?.IsPreloaded ?? false;
	public bool IsNextGameReady => NextGame?.IsPreloaded ?? false;
	public bool IsPreviousGameReady => PreviousGame?.IsPreloaded ?? false;

	public ShortGameServiceProvider(
		[Inject] IInGameLogger logger,
		IEnumerable<Type> games,
		IShortGameFactory gameFactory)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		
		_logger.Log("Initializing GameProvider");

		_gameRegistry = new GameRegistry(logger);
		_gameRegistry.RegisterGames(games);
		
		_queueService = new GameQueueService(logger);
		_gamesLoader = new QueueShortGamesLoader(gameFactory, _queueService, logger);

		_queueService.Initialize(_gameRegistry.RegisteredGames);
	}

	public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
	{
		await UpdatePreloadedGamesAsync(cancellationToken);
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
		if (CurrentGame != null)
		{
			_logger.Log("Starting current game");
			CurrentGame.StartGame();
		}
	}

	public void StartNextGame()
	{
		if (NextGame != null)
		{
			_logger.Log("Starting next game");
			NextGame.StartGame();
		}
	}

	public void StartPreviousGame()
	{
		if (PreviousGame != null)
		{
			_logger.Log("Starting previous game");
			PreviousGame.StartGame();
		}
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

	public async ValueTask UpdatePreloadedGamesAsync(CancellationToken cancellationToken = default)
	{
		if (_queueService == null || _gamesLoader == null)
		{
			return;
		}

		_logger.Log("Updating preloaded games");

		var gamesToPreload = _queueService.GetGamesToPreload();
		await _gamesLoader.PreloadGamesAsync(gamesToPreload, cancellationToken);
	}

	public Task<bool> SwipeToNextGameAsync(CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	public Task<bool> SwipeToPreviousGameAsync(CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
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
}
}