using System;
using System.Collections.Generic;
using System.Linq;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using LightDI.Runtime;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.ShortGamesCore.Source.Pool
{
public class SimpleShortGamePool : IShortGamesPool
{
	private readonly Dictionary<Type, Queue<IShortGamePoolable>> _pooledGames = new();
	private readonly Dictionary<Type, HashSet<IShortGamePoolable>> _activeGames = new();
	private readonly IInGameLogger _logger;

	private readonly int _maxInstancesPerType;

	public SimpleShortGamePool([Inject] IInGameLogger logger, int maxInstancesPerType = 3)
	{
		_logger = logger;
		_maxInstancesPerType = maxInstancesPerType;
	}

	public bool TryGetShortGame<T>(out T game) where T : class, IShortGamePoolable
	{
		if (TryGetShortGame(typeof(T), out var pooledGame))
		{
			game = pooledGame as T;
			return game != null;
		}

		game = null;
		return false;
	}

	public bool TryGetShortGame(Type gameType, out IShortGamePoolable game)
	{
		game = null;

		if (!_pooledGames.TryGetValue(gameType, out var gamesQueue) || gamesQueue.Count == 0)
		{
			_logger.Log($"No games of type {gameType.Name} in pool");
			return false;
		}

		game = gamesQueue.Dequeue();

		if (!_activeGames.ContainsKey(gameType))
		{
			_activeGames[gameType] = new HashSet<IShortGamePoolable>();
		}

		_activeGames[gameType].Add(game);

		if (game is Component component && component != null)
		{
			component.gameObject.SetActive(true);
		}

		_logger.Log($"Retrieved {gameType.Name} from pool. Remaining in pool: {gamesQueue.Count}");

		return true;
	}

	public void ReleaseShortGame<T>(T game) where T : class, IShortGamePoolable
	{
		ReleaseShortGame((IShortGamePoolable)game);
	}

	public void ReleaseShortGame(IShortGamePoolable game)
	{
		if (game == null)
		{
			_logger.LogWarning("Trying to release null game to pool");
			return;
		}

		var gameType = game.GetType();

		if (_activeGames.TryGetValue(gameType, out var activeGame))
		{
			activeGame.Remove(game);
		}

		if (!_pooledGames.ContainsKey(gameType))
		{
			_pooledGames[gameType] = new Queue<IShortGamePoolable>();
		}

		var gamesQueue = _pooledGames[gameType];

		if (gamesQueue.Count >= _maxInstancesPerType)
		{
			_logger.LogWarning(
				$"Pool for {gameType.Name} is full ({_maxInstancesPerType} instances). Destroying excess game.");

			// Dispose the game before destroying it
			try
			{
				game.Dispose();
				_logger.Log($"Disposed excess game: {gameType.Name}");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error disposing excess game {gameType.Name}: {ex.Message}");
			}

			if (game is Component component && component != null)
			{
				if (Application.isEditor && !Application.isPlaying)
				{
					Object.DestroyImmediate(component.gameObject);
				}
				else
				{
					Object.Destroy(component.gameObject);
				}
			}

			return;
		}

		if (game is Component comp && comp != null)
		{
			comp.gameObject.SetActive(false);
		}

		gamesQueue.Enqueue(game);

		_logger.Log($"Released {gameType.Name} to pool. Total in pool: {gamesQueue.Count}");
	}

	public void WarmUpPool<T>(T game) where T : class, IShortGamePoolable
	{
		WarmUpPool((IShortGamePoolable)game);
	}

	public void WarmUpPool(IShortGamePoolable game)
	{
		if (game == null)
		{
			_logger.LogWarning("Trying to warm up pool with null game");
			return;
		}

		var gameType = game.GetType();

		if (!_pooledGames.ContainsKey(gameType))
		{
			_pooledGames[gameType] = new Queue<IShortGamePoolable>();
		}

		var gamesQueue = _pooledGames[gameType];

		if (gamesQueue.Count >= _maxInstancesPerType)
		{
			_logger.LogWarning(
				$"Cannot warm up pool for {gameType.Name} - already at max capacity ({_maxInstancesPerType})");

			// Dispose the game before destroying it
			try
			{
				game.Dispose();
				_logger.Log($"Disposed excess game during warm up: {gameType.Name}");
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error disposing excess game during warm up {gameType.Name}: {ex.Message}");
			}

			if (game is Component component)
			{
				if (Application.isEditor && !Application.isPlaying)
				{
					Object.DestroyImmediate(component.gameObject);
				}
				else
				{
					Object.Destroy(component.gameObject);
				}
			}

			return;
		}

		if (game is Component comp && comp != null)
		{
			comp.gameObject.SetActive(false);
		}

		gamesQueue.Enqueue(game);

		_logger.Log($"Warmed up pool with {gameType.Name}. Total in pool: {gamesQueue.Count}");
	}

	public IEnumerable<Type> GetPooledGameTypes()
	{
		return _pooledGames.Where(kvp => kvp.Value.Count > 0).Select(kvp => kvp.Key);
	}

	public void ClearPoolForType<T>() where T : class, IShortGamePoolable
	{
		ClearPoolForType(typeof(T));
	}

	public void ClearPoolForType(Type gameType)
	{
		if (_pooledGames.TryGetValue(gameType, out var gamesQueue))
		{
			_logger.Log($"Clearing pool for {gameType.Name}. Destroying {gamesQueue.Count} instances.");

			while (gamesQueue.Count > 0)
			{
				var game = gamesQueue.Dequeue();
				
				// Dispose the game before destroying it
				try
				{
					game.Dispose();
				}
				catch (Exception ex)
				{
					_logger.LogError($"Error disposing pooled game {gameType.Name}: {ex.Message}");
				}

				if (game is Component component && component != null)
				{
					if (Application.isEditor && !Application.isPlaying)
					{
						Object.DestroyImmediate(component.gameObject);
					}
					else
					{
						Object.Destroy(component.gameObject);
					}
				}
			}

			_pooledGames.Remove(gameType);
		}

		if (_activeGames.TryGetValue(gameType, out var activeGames))
		{
			_logger.Log($"Clearing {activeGames.Count} active games of type {gameType.Name}");

			foreach (var game in activeGames)
			{
				// Dispose the active game before destroying it
				try
				{
					game.Dispose();
				}
				catch (Exception ex)
				{
					_logger.LogError($"Error disposing active game {gameType.Name}: {ex.Message}");
				}

				if (game is Component component && component != null)
				{
					if (Application.isEditor && !Application.isPlaying)
					{
						Object.DestroyImmediate(component.gameObject);
					}
					else
					{
						Object.Destroy(component.gameObject);
					}
				}
			}

			_activeGames.Remove(gameType);
		}
	}

	public void Dispose()
	{
		_logger.Log("Disposing SimpleShortGamePool");

		foreach (var kvp in _pooledGames)
		{
			var gameType = kvp.Key;
			var gamesQueue = kvp.Value;

			_logger.Log($"Destroying {gamesQueue.Count} pooled {gameType.Name} instances");

			while (gamesQueue.Count > 0)
			{
				var game = gamesQueue.Dequeue();
				
				// Dispose the game before destroying it
				try
				{
					game.Dispose();
				}
				catch (Exception ex)
				{
					_logger.LogError($"Error disposing pooled game {gameType.Name}: {ex.Message}");
				}

				if (game is Component component && component != null)
				{
					if (Application.isEditor && !Application.isPlaying)
					{
						Object.DestroyImmediate(component.gameObject);
					}
					else
					{
						Object.Destroy(component.gameObject);
					}
				}
			}
		}

		foreach (var kvp in _activeGames)
		{
			var gameType = kvp.Key;
			var activeGames = kvp.Value;

			_logger.Log($"Destroying {activeGames.Count} active {gameType.Name} instances");

			foreach (var game in activeGames)
			{
				// Dispose the active game before destroying it
				try
				{
					game.Dispose();
				}
				catch (Exception ex)
				{
					_logger.LogError($"Error disposing active game {gameType.Name}: {ex.Message}");
				}

				if (game is Component component && component != null)
				{
					if (Application.isEditor && !Application.isPlaying)
					{
						Object.DestroyImmediate(component.gameObject);
					}
					else
					{
						Object.Destroy(component.gameObject);
					}
				}
			}
		}

		_pooledGames.Clear();
		_activeGames.Clear();
	}
}
}