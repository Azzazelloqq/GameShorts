using System;
using System.Collections.Generic;
using System.Linq;
using InGameLogger;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Implementation of game queue management service
/// </summary>
internal class GameQueueService : IGameQueueService
{
	private readonly IInGameLogger _logger;
	private readonly List<Type> _gameQueue = new();
	private int _currentIndex = -1;

	public event Action OnQueueUpdated;

	public Type CurrentGameType => GetGameTypeAtIndex(_currentIndex);

	public Type NextGameType => HasNext ? GetGameTypeAtIndex(_currentIndex + 1) : null;

	public Type PreviousGameType => HasPrevious ? GetGameTypeAtIndex(_currentIndex - 1) : null;

	public int CurrentIndex => _currentIndex;

	public int TotalGamesCount => _gameQueue.Count;

	public bool HasNext => _currentIndex < _gameQueue.Count - 1;

	public bool HasPrevious => _currentIndex > 0;

	public GameQueueService(IInGameLogger logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	public void Initialize(IReadOnlyList<Type> gameTypes)
	{
		if (gameTypes == null)
		{
			throw new ArgumentNullException(nameof(gameTypes));
		}

		if (gameTypes.Count == 0)
		{
			throw new ArgumentException("Game types collection cannot be empty", nameof(gameTypes));
		}

		_logger.Log($"Initializing GameQueueService with {gameTypes.Count} games");

		_gameQueue.Clear();
		_gameQueue.AddRange(gameTypes);
		_currentIndex = -1;

		OnQueueUpdated?.Invoke();
	}
	
	public void Dispose()
	{
		_gameQueue.Clear();
		_currentIndex = -1;
	}

	public bool MoveNext()
	{
		// Check if we can move to the next position
		if (_currentIndex >= _gameQueue.Count - 1)
		{
			_logger.Log("Cannot move to next game - already at the end of the queue");
			return false;
		}

		_currentIndex++;
		_logger.Log($"Moved to next game at index {_currentIndex}: {CurrentGameType?.Name}");

		OnQueueUpdated?.Invoke();
		return true;
	}

	public bool MovePrevious()
	{
		if (!HasPrevious)
		{
			_logger.Log("Cannot move to previous game - already at the beginning of the queue");
			return false;
		}

		_currentIndex--;
		_logger.Log($"Moved to previous game at index {_currentIndex}: {CurrentGameType?.Name}");

		OnQueueUpdated?.Invoke();
		return true;
	}

	public bool MoveToIndex(int index)
	{
		if (index < 0 || index >= _gameQueue.Count)
		{
			_logger.LogError($"Invalid index {index}. Queue size: {_gameQueue.Count}");
			return false;
		}

		_currentIndex = index;
		_logger.Log($"Moved to game at index {_currentIndex}: {CurrentGameType?.Name}");

		OnQueueUpdated?.Invoke();
		return true;
	}

	public Type GetGameTypeAtIndex(int index)
	{
		if (index < 0 || index >= _gameQueue.Count)
		{
			return null;
		}

		return _gameQueue[index];
	}

	public IEnumerable<Type> GetGamesToPreload()
	{
		var gamesToPreload = new List<Type>();

		// Add current game if valid
		if (CurrentGameType != null)
		{
			gamesToPreload.Add(CurrentGameType);
		}

		// Add next game if exists
		if (NextGameType != null)
		{
			gamesToPreload.Add(NextGameType);
		}

		// Add previous game if exists
		if (PreviousGameType != null)
		{
			gamesToPreload.Add(PreviousGameType);
		}

		return gamesToPreload.Distinct();
	}

	public void Reset()
	{
		_logger.Log("Resetting game queue to the beginning");
		_currentIndex = -1;
		OnQueueUpdated?.Invoke();
	}

	public void Clear()
	{
		_logger.Log("Clearing game queue");
		_gameQueue.Clear();
		_currentIndex = -1;
		OnQueueUpdated?.Invoke();
	}
}
}