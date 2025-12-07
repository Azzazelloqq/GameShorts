using System;
using System.Collections.Generic;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using LightDI.Runtime;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Implementation of game registry that manages all available games
/// </summary>
public class GameRegistry : IGameRegistry
{
	private readonly IInGameLogger _logger;
	private readonly List<Type> _registeredGames = new();

	public event Action<Type> OnGameRegistered;
	public event Action<Type> OnGameUnregistered;

	public IReadOnlyList<Type> RegisteredGames => _registeredGames.AsReadOnly();
	public int Count => _registeredGames.Count;

	public GameRegistry([Inject] IInGameLogger logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	
	public void Dispose()
	{
		_registeredGames.Clear();
	}

	public bool RegisterGame(Type gameType)
	{
		if (gameType == null)
		{
			throw new ArgumentNullException(nameof(gameType));
		}

		if (!typeof(IShortGame).IsAssignableFrom(gameType))
		{
			_logger.LogError($"Type {gameType.Name} does not implement IShortGame");
			return false;
		}

		if (_registeredGames.Contains(gameType))
		{
			_logger.LogWarning($"Game type {gameType.Name} is already registered");
			return false;
		}

		_registeredGames.Add(gameType);
		_logger.Log($"Registered game: {gameType.Name}");
		OnGameRegistered?.Invoke(gameType);

		return true;
	}

	public void RegisterGames(IEnumerable<Type> gameTypes)
	{
		if (gameTypes == null)
		{
			throw new ArgumentNullException(nameof(gameTypes));
		}

		foreach (var gameType in gameTypes)
		{
			RegisterGame(gameType);
		}
	}

	public bool UnregisterGame(Type gameType)
	{
		if (gameType == null)
		{
			return false;
		}

		if (_registeredGames.Remove(gameType))
		{
			_logger.Log($"Unregistered game: {gameType.Name}");
			OnGameUnregistered?.Invoke(gameType);
			return true;
		}

		return false;
	}

	public bool IsGameRegistered(Type gameType)
	{
		return gameType != null && _registeredGames.Contains(gameType);
	}

	public Type GetGameTypeByIndex(int index)
	{
		if (index < 0 || index >= _registeredGames.Count)
		{
			return null;
		}

		return _registeredGames[index];
	}

	public int GetIndexOfGameType(Type gameType)
	{
		return gameType == null ? -1 : _registeredGames.IndexOf(gameType);
	}
}
}