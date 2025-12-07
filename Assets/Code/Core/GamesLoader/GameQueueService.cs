using System;
using System.Collections.Generic;
using System.Linq;
using InGameLogger;
using LightDI.Runtime;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Implementation of game queue management service
/// </summary>
public class GameQueueService : IGameQueueService
{
	private readonly IInGameLogger _logger;
	private readonly List<Type> _gameQueue = new();
	private int _currentIndex = -1;

	public event Action<IReadOnlyList<Type>, int, QueueChangeReason> OnQueueUpdated;

	public Type CurrentGameType => GetGameTypeAtIndex(_currentIndex);

	public Type NextGameType => HasNext ? GetGameTypeAtIndex(_currentIndex + 1) : null;

	public Type PreviousGameType => HasPrevious ? GetGameTypeAtIndex(_currentIndex - 1) : null;

	public int CurrentIndex => _currentIndex;

	public int TotalGamesCount => _gameQueue.Count;

	public bool HasNext => _currentIndex < _gameQueue.Count - 1;

	public bool HasPrevious => _currentIndex > 0;

	public GameQueueService([Inject]IInGameLogger logger)
	{
		_logger = logger;
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

		NotifyQueueUpdated(QueueChangeReason.Initialized);
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

		NotifyQueueUpdated(QueueChangeReason.Moved);
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

		NotifyQueueUpdated(QueueChangeReason.Moved);
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

		NotifyQueueUpdated(QueueChangeReason.Moved);
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

	public IEnumerable<Type> GetGamesToPreload(int radius = 1)
	{
		if (_gameQueue.Count == 0)
		{
			return Array.Empty<Type>();
		}

		var safeRadius = Math.Max(0, radius);
		var cursor = _currentIndex < 0 ? 0 : _currentIndex;
		var startIndex = Math.Max(0, cursor - safeRadius);
		var endIndex = Math.Min(_gameQueue.Count - 1, cursor + safeRadius);

		var slice = new List<Type>();
		for (var i = startIndex; i <= endIndex; i++)
		{
			slice.Add(_gameQueue[i]);
		}

		return slice.Distinct();
	}

	public void Reset()
	{
		_logger.Log("Resetting game queue to the beginning");
		_currentIndex = -1;
		NotifyQueueUpdated(QueueChangeReason.Reset);
	}

	public void Clear()
	{
		_logger.Log("Clearing game queue");
		_gameQueue.Clear();
		_currentIndex = -1;
		NotifyQueueUpdated(QueueChangeReason.Cleared);
	}

	public void InsertAt(int index, Type gameType)
	{
		if (gameType == null)
		{
			throw new ArgumentNullException(nameof(gameType));
		}

		if (index < 0 || index > _gameQueue.Count)
		{
			throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is outside of queue bounds.");
		}

		_gameQueue.Insert(index, gameType);

		if (index <= _currentIndex)
		{
			_currentIndex++;
		}

		NotifyQueueUpdated(QueueChangeReason.Inserted);
	}

	public bool RemoveAt(int index)
	{
		if (index < 0 || index >= _gameQueue.Count)
		{
			return false;
		}

		_gameQueue.RemoveAt(index);

		if (_gameQueue.Count == 0)
		{
			_currentIndex = -1;
		}
		else if (_currentIndex >= _gameQueue.Count)
		{
			_currentIndex = _gameQueue.Count - 1;
		}
		else if (index <= _currentIndex)
		{
			_currentIndex = Math.Max(-1, _currentIndex - 1);
		}

		NotifyQueueUpdated(QueueChangeReason.Removed);
		return true;
	}

	public bool Remove(Type gameType)
	{
		if (gameType == null)
		{
			return false;
		}

		var index = _gameQueue.IndexOf(gameType);
		if (index < 0)
		{
			return false;
		}

		return RemoveAt(index);
	}

	public void ReplaceAll(IEnumerable<Type> gameTypes)
	{
		if (gameTypes == null)
		{
			throw new ArgumentNullException(nameof(gameTypes));
		}

		_gameQueue.Clear();
		_gameQueue.AddRange(gameTypes);

		if (_gameQueue.Count == 0)
		{
			_currentIndex = -1;
		}
		else
		{
			if (_currentIndex >= _gameQueue.Count)
			{
				_currentIndex = _gameQueue.Count - 1;
			}

			if (_currentIndex < -1)
			{
				_currentIndex = -1;
			}
		}

		NotifyQueueUpdated(QueueChangeReason.Replaced);
	}

	private void NotifyQueueUpdated(QueueChangeReason reason)
	{
		OnQueueUpdated?.Invoke(_gameQueue.AsReadOnly(), _currentIndex, reason);
	}
}
}