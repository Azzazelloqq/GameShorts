using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShortGamesCore.Source.LifeCycleService;
using InGameLogger;
using LightDI.Runtime;
using UnityEngine;

namespace Code.Core.GamesLoader
{

/// <summary>
/// Queue-based game loader that loads games sequentially with preloading support
/// </summary>
public class QueueShortGamesLoader : IGamesLoader
{
	// Events
	public event Action<Type> OnGameLoadingStarted;
	public event Action<Type, IShortGame> OnGameLoadingCompleted;
	public event Action<Type, Exception> OnGameLoadingFailed;
	public event Action<Type> OnPreloadingStarted;
	public event Action<Type> OnPreloadingCompleted;

	// Dependencies
	private readonly IShortGameLifeCycleService _lifeCycleService;
	private readonly IInGameLogger _logger;

	// Queue management
	private readonly List<Type> _gameQueue = new();
	private readonly HashSet<Type> _preloadedTypes = new();
	private int _currentIndex = -1;
	
	// Public property for preload depth
	public int PreloadDepth { get; set; } = 1;

	// State tracking
	private bool _isLoading;
	private bool _isInitialized;
	private readonly CancellationTokenSource _disposeCancellationTokenSource = new();

	// Preloading tasks
	private readonly Dictionary<Type, Task> _preloadingTasks = new();

	// Properties
	public IShortGame CurrentGame => _lifeCycleService?.CurrentGame;
	public int CurrentGameIndex => _currentIndex;
	public int TotalGamesCount => _gameQueue.Count;
	public bool IsLoading => _isLoading;
	public IReadOnlyList<Type> GameQueue => _gameQueue.AsReadOnly();

	public QueueShortGamesLoader(
		[Inject] IShortGameLifeCycleService lifeCycleService,
		[Inject] IInGameLogger logger)
	{
		_lifeCycleService = lifeCycleService ?? throw new ArgumentNullException(nameof(lifeCycleService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}
	
	public void Dispose()
	{
		_logger.Log("Disposing QueueShortGamesLoader");

		try
		{
			// Cancel any ongoing operations
			if (_disposeCancellationTokenSource is { IsCancellationRequested: false })
			{
				_disposeCancellationTokenSource.Cancel();
			}

			
			ClearQueue();

			_lifeCycleService?.Dispose();
		}
		catch (Exception ex)
		{
			// Log error but don't rethrow to avoid cascading failures
			_logger.LogError($"Error during QueueShortGamesLoader disposal: {ex.Message}");
		}
		finally
		{
			try
			{
				_disposeCancellationTokenSource?.Dispose();
			}
			catch (Exception ex)
			{
				_logger.LogError($"Error disposing cancellation token source: {ex.Message}");
			}
		}
	}

	public async ValueTask InitializeAsync(IReadOnlyList<Type> gameTypes, CancellationToken cancellationToken = default)
	{
		if (gameTypes == null)
		{
			throw new ArgumentNullException(nameof(gameTypes));
		}

		if (gameTypes.Count == 0)
		{
			throw new ArgumentException("Game types collection cannot be empty", nameof(gameTypes));
		}

		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, _disposeCancellationTokenSource.Token);

		_logger.Log($"Initializing QueueShortGamesLoader with {gameTypes.Count} games, preload depth: {PreloadDepth}");

		// Clear existing queue
		ClearQueue();

		// Set up new queue
		_gameQueue.AddRange(gameTypes);
		_currentIndex = -1;

		// Validate preload depth
		if (PreloadDepth < 1)
		{
			PreloadDepth = 1;
		}

		// Preload initial games
		await PreloadNextGamesAsync(linkedCts.Token);

		_isInitialized = true;
		_logger.Log("QueueShortGamesLoader initialized successfully");
	}

	public async ValueTask<IShortGame> LoadNextGameAsync(CancellationToken cancellationToken = default)
	{
		if (!_isInitialized)
		{
			_logger.LogError("Loader not initialized. Call InitializeAsync first.");
			return null;
		}

		if (_gameQueue.Count == 0)
		{
			_logger.LogWarning("Game queue is empty");
			return null;
		}

		if (_currentIndex >= _gameQueue.Count - 1)
		{
			_logger.Log("Already at the last game in queue");
			return CurrentGame;
		}

		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, _disposeCancellationTokenSource.Token);

		_currentIndex++;
		return await LoadGameAtIndexAsync(_currentIndex, linkedCts.Token);
	}

	public async ValueTask<IShortGame> LoadPreviousGameAsync(CancellationToken cancellationToken = default)
	{
		if (!_isInitialized)
		{
			_logger.LogError("Loader not initialized. Call InitializeAsync first.");
			return null;
		}

		if (_gameQueue.Count == 0)
		{
			_logger.LogWarning("Game queue is empty");
			return null;
		}

		if (_currentIndex <= 0)
		{
			_logger.Log("Already at the first game in queue");
			return CurrentGame;
		}

		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, _disposeCancellationTokenSource.Token);

		_currentIndex--;
		return await LoadGameAtIndexAsync(_currentIndex, linkedCts.Token);
	}

	public async ValueTask<IShortGame> LoadGameByIndexAsync(int index, CancellationToken cancellationToken = default)
	{
		if (!_isInitialized)
		{
			_logger.LogError("Loader not initialized. Call InitializeAsync first.");
			return null;
		}

		if (index < 0 || index >= _gameQueue.Count)
		{
			_logger.LogError($"Invalid game index: {index}. Queue size: {_gameQueue.Count}");
			return null;
		}

		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, _disposeCancellationTokenSource.Token);

		_currentIndex = index;
		return await LoadGameAtIndexAsync(_currentIndex, linkedCts.Token);
	}

	public void AddGameToQueue(Type gameType)
	{
		if (gameType == null)
		{
			throw new ArgumentNullException(nameof(gameType));
		}

		if (!typeof(IShortGame).IsAssignableFrom(gameType))
		{
			_logger.LogError($"Type {gameType.Name} does not implement IShortGame");
			return;
		}

		_gameQueue.Add(gameType);
		_logger.Log($"Added {gameType.Name} to queue. Total games: {_gameQueue.Count}");
	}

	public bool RemoveGameFromQueue(Type gameType)
	{
		if (gameType == null)
		{
			return false;
		}

		var index = _gameQueue.IndexOf(gameType);
		if (index == -1)
		{
			return false;
		}

		_gameQueue.RemoveAt(index);

		// Adjust current index if needed
		if (_currentIndex >= index && _currentIndex > 0)
		{
			_currentIndex--;
		}

		// Clean up preloaded types
		_preloadedTypes.Remove(gameType);

		// Cancel any ongoing preload for this type
		if (_preloadingTasks.ContainsKey(gameType))
		{
			_preloadingTasks.Remove(gameType);
		}

		_logger.Log($"Removed {gameType.Name} from queue. Total games: {_gameQueue.Count}");
		return true;
	}

	public void ClearQueue()
	{
		_logger.Log("Clearing game queue");
		
		StopCurrentGame();
		
		_gameQueue.Clear();
		_preloadedTypes.Clear();
		_preloadingTasks.Clear();
		_currentIndex = -1;
		_isInitialized = false;
	}

	public void Reset()
	{
		_logger.Log("Resetting loader to initial state");

		StopCurrentGame();
		_currentIndex = -1;
	}

	public async ValueTask<IShortGame> LoadGameAsync(Type gameType, CancellationToken cancellationToken = default)
	{
		if (gameType == null)
		{
			throw new ArgumentNullException(nameof(gameType));
		}

		if (!typeof(IShortGame).IsAssignableFrom(gameType))
		{
			_logger.LogError($"Type {gameType.Name} does not implement IShortGame");
			return null;
		}

		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, _disposeCancellationTokenSource.Token);

		_isLoading = true;

		try
		{
			_logger.Log($"Loading game directly: {gameType.Name}");
			OnGameLoadingStarted?.Invoke(gameType);

			var game = await _lifeCycleService.LoadGameAsync(gameType, linkedCts.Token);

			if (game != null)
			{
				_logger.Log($"Successfully loaded game: {gameType.Name}");
				OnGameLoadingCompleted?.Invoke(gameType, game);

				// Update current index if this game is in the queue
				var index = _gameQueue.IndexOf(gameType);
				if (index >= 0)
				{
					_currentIndex = index;
					// Preload next games in the background
					_ = PreloadNextGamesAsync(cancellationToken);
				}
			}
			else
			{
				var error = new Exception($"Failed to load game: {gameType.Name}");
				_logger.LogError(error.Message);
				OnGameLoadingFailed?.Invoke(gameType, error);
			}

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
		}
	}

	public void StopCurrentGame()
	{
		_lifeCycleService.StopCurrentGame();
	}

	private async ValueTask<IShortGame> LoadGameAtIndexAsync(int index, CancellationToken cancellationToken)
	{
		if (_isLoading)
		{
			_logger.LogWarning("Already loading a game");
			return null;
		}

		_isLoading = true;
		var gameType = _gameQueue[index];

		try
		{
			_logger.Log($"Loading game at index {index}: {gameType.Name}");
			OnGameLoadingStarted?.Invoke(gameType);

			// Wait for preloading to complete if it's in progress
			if (_preloadingTasks.TryGetValue(gameType, out var preloadTask))
			{
				_logger.Log($"Waiting for {gameType.Name} to finish preloading");
				await preloadTask;
				_preloadingTasks.Remove(gameType);
			}

			// Load the game
			var game = await _lifeCycleService.LoadGameAsync(gameType, cancellationToken);

			if (game != null)
			{
				_logger.Log($"Successfully loaded game: {gameType.Name}");
				OnGameLoadingCompleted?.Invoke(gameType, game);

				// Preload next games in the background
				_ = PreloadNextGamesAsync(cancellationToken);
			}
			else
			{
				var error = new Exception($"Failed to load game: {gameType.Name}");
				_logger.LogError(error.Message);
				OnGameLoadingFailed?.Invoke(gameType, error);
			}

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
		}
	}

	private async Task PreloadNextGamesAsync(CancellationToken cancellationToken)
	{
		// Determine which games to preload
		var gamesToPreload = new List<Type>();

		for (var i = 1; i <= PreloadDepth; i++)
		{
			var nextIndex = _currentIndex + i;
			if (nextIndex < _gameQueue.Count)
			{
				var gameType = _gameQueue[nextIndex];
				if (!_preloadedTypes.Contains(gameType) && !_preloadingTasks.ContainsKey(gameType))
				{
					gamesToPreload.Add(gameType);
				}
			}
		}

		if (gamesToPreload.Count == 0)
		{
			_logger.Log("No games to preload");
			return;
		}

		// Start preloading tasks
		var preloadTasks = new List<Task>();

		foreach (var gameType in gamesToPreload)
		{
			var task = PreloadGameAsync(gameType, cancellationToken);
			_preloadingTasks[gameType] = task;
			preloadTasks.Add(task);
		}

		// Wait for all preloading to complete
		try
		{
			await Task.WhenAll(preloadTasks);
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error during preloading: {ex.Message}");
		}
	}

	private async Task PreloadGameAsync(Type gameType, CancellationToken cancellationToken)
	{
		try
		{
			_logger.Log($"Starting preload of {gameType.Name}");
			OnPreloadingStarted?.Invoke(gameType);

			// Use lifecycle service to preload the game
			await _lifeCycleService.PreloadGamesAsync(new[] { gameType }, cancellationToken);

			_preloadedTypes.Add(gameType);
			_logger.Log($"Successfully preloaded {gameType.Name}");
			OnPreloadingCompleted?.Invoke(gameType);
		}
		catch (OperationCanceledException)
		{
			_logger.Log($"Preloading of {gameType.Name} was cancelled");
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to preload {gameType.Name}: {ex.Message}");
		}
		finally
		{
			_preloadingTasks.Remove(gameType);
		}
	}
}
}