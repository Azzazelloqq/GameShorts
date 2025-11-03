using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using LightDI.Runtime;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Queue-based game loader that loads games sequentially from a queue
/// </summary>
internal class QueueShortGamesLoader : IGamesLoader
{
	private readonly IShortGameFactory _gameFactory;
	private readonly IGameQueueService _queueService;
	private readonly IInGameLogger _logger;

	private readonly Dictionary<Type, IShortGame> _loadedGames = new();
	private readonly Dictionary<Type, IShortGame> _preloadedGames = new();
	private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);

	private bool _isLoading;
	private bool _disposed;

	// Events
	public event Action<Type> OnGameLoadingStarted;
	public event Action<Type, IShortGame> OnGameLoadingCompleted;
	public event Action<Type, Exception> OnGameLoadingFailed;
	public event Action<Type> OnGamePreloadingStarted;
	public event Action<Type, IShortGame> OnGamePreloadingCompleted;
	public event Action<Type, Exception> OnGamePreloadingFailed;

	// Properties
	public bool IsLoading => _isLoading;
	public IReadOnlyDictionary<Type, IShortGame> LoadedGames => _loadedGames;
	public IReadOnlyDictionary<Type, IShortGame> PreloadedGames => _preloadedGames;

	// Additional properties for queue management
	public int CurrentGameIndex => _queueService?.CurrentIndex ?? -1;
	public int TotalGamesCount => _queueService?.TotalGamesCount ?? 0;
	public Type CurrentGameType => _queueService?.CurrentGameType;
	public Type NextGameType => _queueService?.NextGameType;
	public Type PreviousGameType => _queueService?.PreviousGameType;

	public QueueShortGamesLoader(
		[Inject] IShortGameFactory gameFactory,
		[Inject] IGameQueueService queueService,
		[Inject] IInGameLogger logger)
	{
		_gameFactory = gameFactory ?? throw new ArgumentNullException(nameof(gameFactory));
		_queueService = queueService ?? throw new ArgumentNullException(nameof(queueService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}


	/// <summary>
	/// Loads the next game in the queue
	/// </summary>
	public async ValueTask<IShortGame> LoadNextGameAsync(CancellationToken cancellationToken = default)
	{
		if (!_queueService.HasNext)
		{
			_logger.Log("No next game in queue");
			return null;
		}

		// Move to next game
		_queueService.MoveNext();

		// Load the current game
		var game = await LoadCurrentGameAsync(cancellationToken);

		// Preload upcoming games
		_ = PreloadUpcomingGamesAsync(cancellationToken);

		return game;
	}

	/// <summary>
	/// Loads the previous game in the queue
	/// </summary>
	public async ValueTask<IShortGame> LoadPreviousGameAsync(CancellationToken cancellationToken = default)
	{
		if (!_queueService.HasPrevious)
		{
			_logger.Log("No previous game in queue");
			return null;
		}

		// Move to previous game
		_queueService.MovePrevious();

		// Load the current game
		var game = await LoadCurrentGameAsync(cancellationToken);

		// Preload upcoming games
		_ = PreloadUpcomingGamesAsync(cancellationToken);

		return game;
	}

	/// <summary>
	/// Loads a specific game by index
	/// </summary>
	public async ValueTask<IShortGame> LoadGameByIndexAsync(int index, CancellationToken cancellationToken = default)
	{
		if (!_queueService.MoveToIndex(index))
		{
			_logger.LogError($"Invalid game index: {index}");
			return null;
		}

		var game = await LoadCurrentGameAsync(cancellationToken);

		_ = PreloadUpcomingGamesAsync(cancellationToken);

		return game;
	}

	public async ValueTask<IShortGame> LoadGameAsync(Type gameType, CancellationToken cancellationToken = default)
	{
		ValidateGameType(gameType);

		if (_disposed)
		{
			_logger.LogError("Cannot load game - loader is disposed");
			return null;
		}

		await _loadingSemaphore.WaitAsync(cancellationToken);
		try
		{
			_isLoading = true;
			_logger.Log($"Starting to load game: {gameType.Name}");
			OnGameLoadingStarted?.Invoke(gameType);

			if (_loadedGames.TryGetValue(gameType, out var existingGame))
			{
				return existingGame;
			}

			if (_preloadedGames.TryGetValue(gameType, out var game))
			{
				_logger.Log($"Using preloaded game: {gameType.Name}");
				_preloadedGames.Remove(gameType);
			}
			else
			{
				game = await _gameFactory.CreateShortGameAsync(gameType, cancellationToken);

				if (game == null)
				{
					var error = new Exception($"Failed to create game instance: {gameType.Name}");
					_logger.LogError(error.Message);
					OnGameLoadingFailed?.Invoke(gameType, error);
					return null;
				}

				// Preload if not already preloaded
				if (!game.IsPreloaded)
				{
					await game.PreloadGameAsync(cancellationToken);
				}
			}

			game.StartGame();
			_loadedGames[gameType] = game;

			_logger.Log($"Successfully loaded and started game: {gameType.Name}");
			OnGameLoadingCompleted?.Invoke(gameType, game);

			return game;
		}
		catch (OperationCanceledException)
		{
			_logger.Log($"Loading of {gameType.Name} was cancelled");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error loading game {gameType.Name}: {ex.Message}");
			OnGameLoadingFailed?.Invoke(gameType, ex);
			return null;
		}
		finally
		{
			_isLoading = false;
			_loadingSemaphore.Release();
		}
	}

	public async ValueTask<IShortGame> PreloadGameAsync(Type gameType, CancellationToken cancellationToken = default)
	{
		ValidateGameType(gameType);

		if (_disposed)
		{
			_logger.LogError("Cannot preload game - loader is disposed");
			return null;
		}

		await _loadingSemaphore.WaitAsync(cancellationToken);
		try
		{
			_logger.Log($"Starting to preload game: {gameType.Name}");
			OnGamePreloadingStarted?.Invoke(gameType);

			if (_loadedGames.TryGetValue(gameType, out var loadedGame))
			{
				return loadedGame;
			}

			if (_preloadedGames.TryGetValue(gameType, out var preloadedGame))
			{
				return preloadedGame;
			}

			var game = await _gameFactory.CreateShortGameAsync(gameType, cancellationToken);

			if (game == null)
			{
				var error = new Exception($"Failed to create game instance for preloading: {gameType.Name}");
				_logger.LogError(error.Message);
				OnGamePreloadingFailed?.Invoke(gameType, error);
				return null;
			}

			// Preload the game
			await game.PreloadGameAsync(cancellationToken);

			_preloadedGames[gameType] = game;

			_logger.Log($"Successfully preloaded game: {gameType.Name}");
			OnGamePreloadingCompleted?.Invoke(gameType, game);

			return game;
		}
		catch (OperationCanceledException)
		{
			_logger.Log($"Preloading of {gameType.Name} was cancelled");
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error preloading game {gameType.Name}: {ex.Message}");
			OnGamePreloadingFailed?.Invoke(gameType, ex);
			return null;
		}
		finally
		{
			_loadingSemaphore.Release();
		}
	}

	public async ValueTask<IReadOnlyDictionary<Type, IShortGame>> PreloadGamesAsync(
		IEnumerable<Type> gameTypes,
		CancellationToken cancellationToken = default)
	{
		if (gameTypes == null)
		{
			throw new ArgumentNullException(nameof(gameTypes));
		}

		var gameTypesList = gameTypes.ToList();
		if (gameTypesList.Count == 0)
		{
			return new Dictionary<Type, IShortGame>();
		}

		_logger.Log($"Starting to preload {gameTypesList.Count} games");

		var preloadTasks = gameTypesList
			.Select(type => PreloadGameAsync(type, cancellationToken))
			.ToList();

		var results = await Task.WhenAll(preloadTasks.Select(t => t.AsTask()));

		var preloadedGames = new Dictionary<Type, IShortGame>();
		for (var i = 0; i < gameTypesList.Count; i++)
		{
			if (results[i] != null)
			{
				preloadedGames[gameTypesList[i]] = results[i];
			}
		}

		_logger.Log($"Preloaded {preloadedGames.Count} out of {gameTypesList.Count} games");
		return preloadedGames;
	}

	public bool StartPreloadedGame(Type gameType)
	{
		ValidateGameType(gameType);

		if (!_preloadedGames.TryGetValue(gameType, out var game))
		{
			_logger.LogError($"Game {gameType.Name} is not preloaded");
			return false;
		}

		// Move from preloaded to loaded
		_preloadedGames.Remove(gameType);
		_loadedGames[gameType] = game;

		// Start the game
		game.StartGame();

		_logger.Log($"Started preloaded game: {gameType.Name}");
		OnGameLoadingCompleted?.Invoke(gameType, game);

		return true;
	}

	public void UnloadGame(Type gameType)
	{
		if (gameType == null)
		{
			return;
		}

		// Remove from loaded games
		if (_loadedGames.TryGetValue(gameType, out var loadedGame))
		{
			_logger.Log($"Unloading loaded game: {gameType.Name}");
			loadedGame.StopGame();
			loadedGame.Dispose();
			_loadedGames.Remove(gameType);
		}

		// Remove from preloaded games
		if (_preloadedGames.TryGetValue(gameType, out var preloadedGame))
		{
			_logger.Log($"Unloading preloaded game: {gameType.Name}");
			preloadedGame.Dispose();
			_preloadedGames.Remove(gameType);
		}
	}

	public void UnloadAllGames()
	{
		_logger.Log("Unloading all games");

		// Unload loaded games
		foreach (var kvp in _loadedGames)
		{
			try
			{
				kvp.Value.StopGame();
				kvp.Value.Dispose();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error unloading game {kvp.Key.Name}: {ex.Message}");
			}
		}

		_loadedGames.Clear();

		// Unload preloaded games
		foreach (var kvp in _preloadedGames)
		{
			try
			{
				kvp.Value.Dispose();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error unloading preloaded game {kvp.Key.Name}: {ex.Message}");
			}
		}

		_preloadedGames.Clear();
	}

	public IShortGame GetGame(Type gameType)
	{
		if (gameType == null)
		{
			return null;
		}

		if (_loadedGames.TryGetValue(gameType, out var loadedGame))
		{
			return loadedGame;
		}

		if (_preloadedGames.TryGetValue(gameType, out var preloadedGame))
		{
			return preloadedGame;
		}

		return null;
	}

	public bool IsGameLoaded(Type gameType)
	{
		if (gameType == null)
		{
			return false;
		}

		return _loadedGames.ContainsKey(gameType) || _preloadedGames.ContainsKey(gameType);
	}

	/// <summary>
	/// Resets the queue to the beginning
	/// </summary>
	public void Reset()
	{
		_logger.Log("Resetting QueueShortGamesLoader");
		_queueService.Reset();
		UnloadAllGames();
	}

	public void Dispose()
	{
		if (_disposed)
		{
			return;
		}

		_logger.Log("Disposing QueueShortGamesLoader");

		_disposed = true;

		UnloadAllGames();

		_queueService?.Clear();
		_loadingSemaphore?.Dispose();
	}

	private async ValueTask<IShortGame> LoadCurrentGameAsync(CancellationToken cancellationToken)
	{
		var currentType = _queueService.CurrentGameType;
		if (currentType == null)
		{
			_logger.LogError("No current game type in queue");
			return null;
		}

		// Check if it's already preloaded
		if (_preloadedGames.ContainsKey(currentType))
		{
			// Start the preloaded game
			StartPreloadedGame(currentType);
			return GetGame(currentType);
		}

		// Load the game
		return await LoadGameAsync(currentType, cancellationToken);
	}

	private async Task PreloadUpcomingGamesAsync(CancellationToken cancellationToken)
	{
		try
		{
			var gamesToPreload = _queueService.GetGamesToPreload();

			foreach (var gameType in gamesToPreload)
			{
				// Skip if already loaded or preloaded
				if (IsGameLoaded(gameType))
				{
					continue;
				}

				await PreloadGameAsync(gameType, cancellationToken);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error preloading upcoming games: {ex.Message}");
		}
	}

	private void ValidateGameType(Type gameType)
	{
		if (gameType == null)
		{
			throw new ArgumentNullException(nameof(gameType));
		}

		if (!typeof(IShortGame).IsAssignableFrom(gameType))
		{
			throw new ArgumentException($"Type {gameType.Name} does not implement IShortGame", nameof(gameType));
		}
	}
}
}