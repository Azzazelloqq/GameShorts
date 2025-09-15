using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShortGamesCore.Source.Pool;
using InGameLogger;
using LightDI.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.ShortGamesCore.Source.LifeCycleService
{
public class SimpleShortGameLifeCycleService : IShortGameLifeCycleService
{
	private readonly IShortGamesPool _pool;
	private readonly IShortGameFactory _factory;
	private readonly IInGameLogger _logger;

	private IShortGame _currentGame;
	private readonly List<Type> _preloadedGameTypes = new();
	private int _currentGameIndex = -1;
	private readonly CancellationTokenSource _disposeCancellationTokenSource = new();

	public IShortGame CurrentGame => _currentGame;

	public SimpleShortGameLifeCycleService(
		[Inject] IShortGamesPool pool,
		[Inject] IShortGameFactory factory,
		[Inject] IInGameLogger logger)
	{
		_pool = pool;
		_factory = factory;
		_logger = logger;
	}

	public void Dispose()
	{
		_logger.Log("Disposing SimpleShortGameLifeCycleService");

		StopCurrentGame();
		DisposePreloadedGames();
		
		if (!_disposeCancellationTokenSource.IsCancellationRequested)
		{
			_disposeCancellationTokenSource.Cancel();
		}

		_disposeCancellationTokenSource.Dispose();
	}
	
	public async ValueTask PreloadGamesAsync(IEnumerable<Type> gameTypes, CancellationToken cancellationToken = default)
	{
		using var linkedCts =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCancellationTokenSource.Token);

		var gameTypesList = gameTypes.ToList();
		_preloadedGameTypes.Clear();

		foreach (var gameType in gameTypesList)
		{
			try
			{
				await PreloadGameInternalAsync(gameType, linkedCts.Token);
				_preloadedGameTypes.Add(gameType);
				_logger.Log($"Preloaded game: {gameType.Name}");
			}
			catch (OperationCanceledException)
			{
				throw;
			}
			catch (Exception e)
			{
				_logger.LogError($"Failed to preload game {gameType.Name}: {e.Message}");
			}
		}

		if (_preloadedGameTypes.Count > 0)
		{
			_currentGameIndex = -1;
		}
	}

	public async ValueTask PreloadGameAsync<T>(CancellationToken cancellationToken = default) where T : class, IShortGame
	{
		using var linkedCts =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCancellationTokenSource.Token);

		try
		{
			await PreloadGameInternalAsync(typeof(T), linkedCts.Token);

			if (!_preloadedGameTypes.Contains(typeof(T)))
			{
				_preloadedGameTypes.Add(typeof(T));
			}

			_logger.Log($"Preloaded game: {typeof(T).Name}");
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (Exception e)
		{
			_logger.LogError($"Failed to preload game {typeof(T).Name}: {e.Message}");
		}
	}

	public async ValueTask<T> LoadGameAsync<T>(CancellationToken cancellationToken = default) where T : class, IShortGame
	{
		using var linkedCts =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCancellationTokenSource.Token);

		StopCurrentGame();

		T game = null;
		var fromPool = false;

		if (typeof(IPoolableShortGame).IsAssignableFrom(typeof(T)))
		{
			if (_pool.TryGetShortGame(typeof(T), out var pooledGame))
			{
				game = pooledGame as T;
				if (game != null)
				{
					fromPool = true;
					(game as IPoolableShortGame)?.OnUnpooled();
					_logger.Log($"Got game {typeof(T).Name} from pool");
				}
			}
		}

		if (game == null)
		{
			game = await _factory.CreateShortGameAsync(typeof(T), linkedCts.Token) as T;
			if (game == null)
			{
				_logger.LogError($"Failed to create game of type {typeof(T).Name}");
				return null;
			}

			_logger.Log($"Created new game instance: {typeof(T).Name}");
		}

		_currentGame = game;
		_currentGame.StartGame();

		var index = _preloadedGameTypes.IndexOf(typeof(T));
		if (index >= 0)
		{
			_currentGameIndex = index;
		}

		_logger.Log($"Started game: {typeof(T).Name} (from pool: {fromPool})");

		return game;
	}

	public async ValueTask<IShortGame> LoadGameAsync(Type gameType, CancellationToken cancellationToken = default)
	{
		using var linkedCts =
			CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCancellationTokenSource.Token);

		StopCurrentGame();

		IShortGame game = null;

		if (typeof(IPoolableShortGame).IsAssignableFrom(gameType))
		{
			if (_pool.TryGetShortGame(gameType, out var pooledGame))
			{
				game = pooledGame;
				pooledGame.OnUnpooled();
			}
		}

		game ??= await _factory.CreateShortGameAsync(gameType, linkedCts.Token);

		_currentGame = game;
		_currentGame.StartGame();

		var index = _preloadedGameTypes.IndexOf(gameType);
		if (index >= 0)
		{
			_currentGameIndex = index;
		}

		return game;
	}

	public void StopCurrentGame()
	{
		if (_currentGame == null)
		{
			return;
		}

		_currentGame.StopGame();

		if (_currentGame is IPoolableShortGame poolableGame)
		{
			poolableGame.OnPooled();
			_pool.ReleaseShortGame(poolableGame);
		}
		else
		{
			try
			{
				_currentGame.Dispose();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error disposing game {_currentGame.GetType().Name}: {ex.Message}");
			}
		}

		_currentGame = null;
	}

	public async ValueTask<IShortGame> LoadNextGameAsync(CancellationToken cancellationToken = default)
	{
		if (_preloadedGameTypes.Count == 0)
		{
			_logger.LogError("No preloaded games available");
			return null;
		}

		_currentGameIndex = (_currentGameIndex + 1) % _preloadedGameTypes.Count;
		var nextGameType = _preloadedGameTypes[_currentGameIndex];

		_logger.Log($"Loading next game: {nextGameType.Name} (index: {_currentGameIndex})");

		return await LoadGameAsync(nextGameType, cancellationToken);
	}

	public async ValueTask<IShortGame> LoadPreviousGameAsync(CancellationToken cancellationToken = default)
	{
		if (_preloadedGameTypes.Count == 0)
		{
			_logger.LogError("No preloaded games available");
			return null;
		}

		_currentGameIndex--;
		if (_currentGameIndex < 0)
		{
			_currentGameIndex = _preloadedGameTypes.Count - 1;
		}

		var previousGameType = _preloadedGameTypes[_currentGameIndex];

		_logger.Log($"Loading previous game: {previousGameType.Name} (index: {_currentGameIndex})");

		return await LoadGameAsync(previousGameType, cancellationToken);
	}

	public void ClearPreloadedGames()
	{
		_logger.Log("Clearing all preloaded games");

		foreach (var gameType in _preloadedGameTypes)
		{
			_pool.ClearPoolForType(gameType);
			_factory.UnloadGameResources(gameType);
		}

		_preloadedGameTypes.Clear();
	}

	private void DisposePreloadedGames()
	{
		_preloadedGameTypes.Clear();
		_pool.Dispose();
		_factory.Dispose();
	}

	private async ValueTask PreloadGameInternalAsync(Type gameType, CancellationToken cancellationToken)
	{
		await _factory.PreloadGameResourcesAsync(gameType, cancellationToken);

		if (typeof(IPoolableShortGame).IsAssignableFrom(gameType))
		{
			var game = await _factory.CreateShortGameAsync(gameType, cancellationToken);

			if (game is IPoolableShortGame poolableGame)
			{
				poolableGame.OnPooled();
				_pool.WarmUpPool(poolableGame);
				_logger.Log($"Added {gameType.Name} to pool for preloading");
			}
		}
	}
}
}