using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.GameCore;
using InGameLogger;
using LightDI.Runtime;

namespace Code.Core.GamesLoader
{
/// <summary>
/// Queue-based game loader that loads games sequentially from a queue
/// </summary>
public class QueueShortGamesLoader : IGamesLoader
{
	private readonly IShortGameFactory _gameFactory;
	private readonly IGameQueueService _queueService;
	private readonly IInGameLogger _logger;
	private static readonly TimeSpan PreloadPollDelay = TimeSpan.FromMilliseconds(50);
	private readonly CancellationTokenSource _lifetimeCts = new();
	private readonly object _backgroundLock = new();
	private CancellationTokenSource _upcomingPreloadCts;
	private Task _upcomingPreloadTask = Task.CompletedTask;

	private readonly Dictionary<Type, IShortGame> _loadedGames = new();
	private readonly Dictionary<Type, IShortGame> _preloadedGames = new();
	private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);
	private readonly SemaphoreSlim _activationSemaphore = new(1, 1);
	private readonly ShortGameLoaderSettings _settings;
	private Type _activeGameType;
	private Type _pendingActiveGameType;
	private CancellationTokenSource _preloadWindowCts;

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
	public Type ActiveGameType => _activeGameType;
	public IShortGame ActiveGame => _activeGameType != null && _loadedGames.TryGetValue(_activeGameType, out var active)
		? active
		: null;
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
		[Inject] IInGameLogger logger,
		ShortGameLoaderSettings settings)
	{
		_gameFactory = gameFactory;
		_queueService = queueService;
		_logger = logger;
		_settings = settings;
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

		// Preload upcoming games (single-flight, cancellable)
		StartPreloadUpcomingGames(cancellationToken);

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

		// Preload upcoming games (single-flight, cancellable)
		StartPreloadUpcomingGames(cancellationToken);

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

		// Preload upcoming games (single-flight, cancellable)
		StartPreloadUpcomingGames(cancellationToken);

		return game;
	}

	public async Cysharp.Threading.Tasks.UniTask<IShortGame> LoadGameAsync(Type gameType, CancellationToken cancellationToken = default)
	{
		ValidateGameType(gameType);

		if (_disposed)
		{
			_logger.LogError("Cannot load game - loader is disposed");
			return null;
		}

		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCts.Token);
		await _loadingSemaphore.WaitAsync(linkedCts.Token);
		IShortGame createdGame = null;
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
				// Ensure the prefab/resources are preloaded and kept in memory (addressables handles/prefabs).
				// This makes instance creation and subsequent in-game resource loads much cheaper.
				try
				{
					await _gameFactory.PreloadGameResourcesAsync(gameType, linkedCts.Token);
				}
				catch (OperationCanceledException)
				{
					throw;
				}
				catch (Exception ex)
				{
					_logger.LogError($"Failed to preload resources for {gameType.Name}: {ex.Message}");
				}

				game = await _gameFactory.CreateShortGameAsync(gameType, linkedCts.Token);
				createdGame = game;

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
					await game.PreloadGameAsync(linkedCts.Token);
				}
			}

			SafeEnableGame(gameType, game, "load-start");
			game.StartGame();
			_loadedGames[gameType] = game;
			MarkGameActive(gameType);

			_logger.Log($"Successfully loaded and started game: {gameType.Name}");
			OnGameLoadingCompleted?.Invoke(gameType, game);

			return game;
		}
		catch (OperationCanceledException)
		{
			_logger.Log($"Loading of {gameType.Name} was cancelled");
			if (createdGame != null)
			{
				SafeDispose(gameType, createdGame, "load-cancelled");
			}
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error loading game {gameType.Name}: {ex.Message}");
			OnGameLoadingFailed?.Invoke(gameType, ex);
			if (createdGame != null)
			{
				SafeDispose(gameType, createdGame, "load-failed");
			}
			return null;
		}
		finally
		{
			_isLoading = false;
			_loadingSemaphore.Release();
		}
	}

	public async Cysharp.Threading.Tasks.UniTask<IShortGame> PreloadGameAsync(Type gameType, CancellationToken cancellationToken = default)
	{
		ValidateGameType(gameType);

		if (_disposed)
		{
			_logger.LogError("Cannot preload game - loader is disposed");
			return null;
		}

		using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _lifetimeCts.Token);
		await _loadingSemaphore.WaitAsync(linkedCts.Token);
		IShortGame createdGame = null;
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

			// Preload and keep prefab/resources in memory before instantiation.
			// This aligns with the "keep everything in memory" strategy for instant game start.
			await _gameFactory.PreloadGameResourcesAsync(gameType, linkedCts.Token);

			var game = await _gameFactory.CreateShortGameAsync(gameType, linkedCts.Token);
			createdGame = game;

			if (game == null)
			{
				var error = new Exception($"Failed to create game instance for preloading: {gameType.Name}");
				_logger.LogError(error.Message);
				OnGamePreloadingFailed?.Invoke(gameType, error);
				return null;
			}

			// Ensure preloading happens while the game is disabled to avoid affecting other running games.
			SafeDisableGame(gameType, game, "preload");

			// Preload the game
			await game.PreloadGameAsync(linkedCts.Token);

			_preloadedGames[gameType] = game;

			_logger.Log($"Successfully preloaded game: {gameType.Name}");
			OnGamePreloadingCompleted?.Invoke(gameType, game);

			return game;
		}
		catch (OperationCanceledException)
		{
			_logger.Log($"Preloading of {gameType.Name} was cancelled");
			if (createdGame != null)
			{
				SafeDispose(gameType, createdGame, "preload-cancelled");
			}
			throw;
		}
		catch (Exception ex)
		{
			_logger.LogError($"Error preloading game {gameType.Name}: {ex.Message}");
			OnGamePreloadingFailed?.Invoke(gameType, ex);
			if (createdGame != null)
			{
				SafeDispose(gameType, createdGame, "preload-failed");
			}
			return null;
		}
		finally
		{
			_loadingSemaphore.Release();
		}
	}

	public async Cysharp.Threading.Tasks.UniTask<IReadOnlyDictionary<Type, IShortGame>> PreloadGamesAsync(
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

		var results = await UniTask.WhenAll(preloadTasks);

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

	public async UniTask PreloadWindowAsync(CancellationToken cancellationToken = default)
	{
		if (_disposed)
		{
			_logger.LogWarning("Skipping preload window - loader already disposed");
			return;
		}

		var windowTypes = _queueService.GetGamesToPreload(_settings.PreloadRadius)
			.Where(type => type != null)
			.Distinct()
			.ToList();

		if (windowTypes.Count == 0)
		{
			_logger.Log("Preload window requested but queue is empty");
			return;
		}

		var toPreload = windowTypes
			.Where(type => !IsGameLoaded(type))
			.ToList();

		if (toPreload.Count > 0)
		{
			_logger.Log($"Preloading window around index {_queueService.CurrentIndex}: {string.Join(", ", toPreload.Select(t => t.Name))}");
			await PreloadGamesAsync(toPreload, cancellationToken);
		}

		TrimPreloadedGames(windowTypes);
	}

	public bool StartPreloadedGame(Type gameType)
	{
		ValidateGameType(gameType);

		if (!_preloadedGames.Remove(gameType, out var game))
		{
			_logger.LogError($"Game {gameType.Name} is not preloaded");
			return false;
		}

		// Move from preloaded to loaded
		_loadedGames[gameType] = game;

		// Start the game
		SafeEnableGame(gameType, game, "start-preloaded");
		game.StartGame();
		MarkGameActive(gameType);

		_logger.Log($"Started preloaded game: {gameType.Name}");
		OnGameLoadingCompleted?.Invoke(gameType, game);

		return true;
	}

	public async Cysharp.Threading.Tasks.UniTask<bool> ActivateCurrentGameAsync(
		CancellationToken cancellationToken = default,
		bool waitForReady = true)
	{
		await _activationSemaphore.WaitAsync(cancellationToken);
		try
		{
			var activated = await ActivateCurrentSlotAsync("current", cancellationToken, waitForReady);
			if (activated)
			{
				if (waitForReady)
				{
					await PreloadWindowAsync(cancellationToken);
				}
				else
				{
					StartPreloadWindowInBackground();
				}
			}

			return activated;
		}
		finally
		{
			_activationSemaphore.Release();
		}
	}

	public async Cysharp.Threading.Tasks.UniTask<bool> ActivateNextGameAsync(
		CancellationToken cancellationToken = default,
		bool waitForReady = true)
	{
		await _activationSemaphore.WaitAsync(cancellationToken);
		try
		{
			if (!_queueService.HasNext)
			{
				_logger.LogWarning("Cannot activate next game - end of queue reached");
				return false;
			}

			if (!_queueService.MoveNext())
			{
				return false;
			}

			var activated = await ActivateCurrentSlotAsync("next", cancellationToken, waitForReady);
			if (activated)
			{
				if (waitForReady)
				{
					await PreloadWindowAsync(cancellationToken);
				}
				else
				{
					StartPreloadWindowInBackground();
				}
			}

			return activated;
		}
		finally
		{
			_activationSemaphore.Release();
		}
	}

	public async Cysharp.Threading.Tasks.UniTask<bool> ActivatePreviousGameAsync(
		CancellationToken cancellationToken = default,
		bool waitForReady = true)
	{
		await _activationSemaphore.WaitAsync(cancellationToken);
		try
		{
			if (!_queueService.HasPrevious)
			{
				_logger.LogWarning("Cannot activate previous game - start of queue reached");
				return false;
			}

			if (!_queueService.MovePrevious())
			{
				return false;
			}

			var activated = await ActivateCurrentSlotAsync("previous", cancellationToken, waitForReady);
			if (activated)
			{
				if (waitForReady)
				{
					await PreloadWindowAsync(cancellationToken);
				}
				else
				{
					StartPreloadWindowInBackground();
				}
			}

			return activated;
		}
		finally
		{
			_activationSemaphore.Release();
		}
	}

	public void UnloadGame(Type gameType)
	{
		if (gameType == null)
		{
			return;
		}

		_loadedGames.TryGetValue(gameType, out var loadedGame);
		_preloadedGames.TryGetValue(gameType, out var preloadedGame);

		// Safety: it should never happen, but if both maps point to the same instance, unload it once.
		if (loadedGame != null && preloadedGame != null && ReferenceEquals(loadedGame, preloadedGame))
		{
			_logger.LogWarning($"Game {gameType.Name} is present in both LoadedGames and PreloadedGames. Unloading once.");
			_loadedGames.Remove(gameType);
			_preloadedGames.Remove(gameType);
			SafeStopGame(gameType, loadedGame, "both-loaded-and-preloaded");
			SafeDispose(gameType, loadedGame, "both-loaded-and-preloaded");
		}
		else
		{
			// Remove from loaded games
			if (loadedGame != null)
			{
				_logger.Log($"Unloading loaded game: {gameType.Name}");
				_loadedGames.Remove(gameType);
				SafeStopGame(gameType, loadedGame, "loaded");
				SafeDispose(gameType, loadedGame, "loaded");
			}

			// Remove from preloaded games
			if (preloadedGame != null)
			{
				_logger.Log($"Unloading preloaded game: {gameType.Name}");
				_preloadedGames.Remove(gameType);
				SafeDispose(gameType, preloadedGame, "preloaded");
			}
		}

		if (_activeGameType == gameType)
		{
			_activeGameType = null;
		}

		// Release cached factory resources (prefab/addressables handles) when this game leaves the window.
		// Safe even if the game wasn't preloaded via factory (factory tracks ref-count internally).
		try
		{
			_gameFactory.UnloadGameResources(gameType);
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to unload factory resources for {gameType.Name}: {ex.Message}");
		}
	}

	public void UnloadAllGames()
	{
		_logger.Log("Unloading all games");

		var allTypes = _loadedGames.Keys
			.Concat(_preloadedGames.Keys)
			.Where(type => type != null)
			.Distinct()
			.ToList();

		foreach (var type in allTypes)
		{
			UnloadGame(type);
		}

		_loadedGames.Clear();
		_preloadedGames.Clear();
		_activeGameType = null;
		_pendingActiveGameType = null;
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
		try { _lifetimeCts.Cancel(); } catch { /* ignored */ }
		lock (_backgroundLock)
		{
			try { _upcomingPreloadCts?.Cancel(); } catch { /* ignored */ }
			try { _preloadWindowCts?.Cancel(); } catch { /* ignored */ }
			try { _preloadWindowCts?.Dispose(); } catch { /* ignored */ }
			_preloadWindowCts = null;
		}

		UnloadAllGames();

		_queueService?.Clear();

		// Don't block Unity main thread here. We finalize disposal asynchronously once background work stops.
		ScheduleDisposeCleanup();
	}

	private void StartPreloadUpcomingGames(CancellationToken externalToken)
	{
		if (_disposed)
		{
			return;
		}

		lock (_backgroundLock)
		{
			try { _upcomingPreloadCts?.Cancel(); } catch { /* ignored */ }
			try { _upcomingPreloadCts?.Dispose(); } catch { /* ignored */ }

			_upcomingPreloadCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, _lifetimeCts.Token);
			var token = _upcomingPreloadCts.Token;

			// single-flight: replace the previous task reference
			_upcomingPreloadTask = PreloadUpcomingGamesAsync(token);
		}
	}

	private void StartPreloadWindowInBackground()
	{
		if (_disposed)
		{
			return;
		}

		lock (_backgroundLock)
		{
			try { _preloadWindowCts?.Cancel(); } catch { /* ignored */ }
			try { _preloadWindowCts?.Dispose(); } catch { /* ignored */ }

			_preloadWindowCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
			var token = _preloadWindowCts.Token;
			PreloadWindowInBackgroundAsync(token).Forget();
		}
	}

	private async UniTask PreloadWindowInBackgroundAsync(CancellationToken token)
	{
		try
		{
			await PreloadWindowAsync(token);
		}
		catch (OperationCanceledException)
		{
			// Expected during shutdown / rapid navigation.
		}
		catch (Exception ex)
		{
			_logger.LogWarning($"Background preload window failed: {ex.Message}");
		}
	}

	private void ScheduleDisposeCleanup()
	{
		Task upcomingTask;
		CancellationTokenSource upcomingCts;

		lock (_backgroundLock)
		{
			upcomingTask = _upcomingPreloadTask ?? Task.CompletedTask;
			upcomingCts = _upcomingPreloadCts;
			_upcomingPreloadTask = Task.CompletedTask;
			_upcomingPreloadCts = null;
		}

		DisposeCleanupAsync(upcomingTask, upcomingCts).Forget();
	}

	private async UniTask DisposeCleanupAsync(Task upcomingTask, CancellationTokenSource upcomingCts)
	{
		try
		{
			// Best-effort: ensure no background code touches semaphores after this point.
			await upcomingTask;
		}
		catch (OperationCanceledException)
		{
			// Expected during shutdown.
		}
		catch (Exception ex)
		{
			_logger.LogWarning($"Background preload task failed during dispose: {ex.Message}");
		}
		finally
		{
			try { upcomingCts?.Dispose(); } catch { /* ignored */ }
			try { _lifetimeCts.Dispose(); } catch { /* ignored */ }
			try { _loadingSemaphore.Dispose(); } catch { /* ignored */ }
			try { _activationSemaphore.Dispose(); } catch { /* ignored */ }
		}
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
			if (_disposed)
			{
				return;
			}

			var gamesToPreload = _queueService.GetGamesToPreload();

			foreach (var gameType in gamesToPreload)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (_disposed)
				{
					return;
				}

				// Skip if already loaded or preloaded
				if (IsGameLoaded(gameType))
				{
					continue;
				}

				await PreloadGameAsync(gameType, cancellationToken);
			}
		}
		catch (OperationCanceledException)
		{
			// Expected during shutdown / rapid navigation.
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

	private async Task<bool> ActivateCurrentSlotAsync(
		string direction,
		CancellationToken cancellationToken,
		bool waitForReady)
	{
		var currentType = _queueService.CurrentGameType;
		if (currentType == null)
		{
			_logger.LogError($"Cannot activate {direction} game - queue returned null for current slot.");
			return false;
		}

		_logger.Log($"Activating {direction} game {currentType.Name} at index {_queueService.CurrentIndex}");

		var previousActive = _activeGameType;

		if (waitForReady)
		{
			var game = await EnsureGameReadyAsync(currentType, cancellationToken);
			if (game == null)
			{
				_logger.LogError($"Failed to activate {currentType.Name} - game could not be prepared.");
				return false;
			}

			if (!_loadedGames.ContainsKey(currentType))
			{
				_loadedGames[currentType] = game;
			}

			_pendingActiveGameType = null;
			MarkGameActive(currentType);
		}
		else
		{
			_pendingActiveGameType = currentType;
			_activeGameType = null;
		}

		if (previousActive != null && previousActive != currentType)
		{
			StopGameIfPossible(previousActive);
		}

		TrimLoadedGames();
		return true;
	}

	private async Task<IShortGame> EnsureGameReadyAsync(Type gameType, CancellationToken cancellationToken)
	{
		if (_loadedGames.TryGetValue(gameType, out var activeGame))
		{
			return activeGame;
		}

		if (_preloadedGames.ContainsKey(gameType))
		{
			return StartPreloadedAndGet(gameType);
		}

		var deadline = DateTime.UtcNow + _settings.ReadinessTimeout;
		while (DateTime.UtcNow < deadline)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (_preloadedGames.ContainsKey(gameType))
			{
				return StartPreloadedAndGet(gameType);
			}

			await Task.Delay(PreloadPollDelay, cancellationToken);
		}

		for (var attempt = 1; attempt <= _settings.FallbackLoadAttempts; attempt++)
		{
			_logger.LogWarning($"Game {gameType.Name} was not preloaded in time. Fallback load attempt {attempt}/{_settings.FallbackLoadAttempts}.");
			var loaded = await LoadGameAsync(gameType, cancellationToken);
			if (loaded != null)
			{
				return loaded;
			}
		}

		return null;
	}

	private IShortGame StartPreloadedAndGet(Type gameType)
	{
		if (!StartPreloadedGame(gameType))
		{
			return null;
		}

		return _loadedGames.TryGetValue(gameType, out var game) ? game : null;
	}

	private void TrimLoadedGames()
	{
		if (_settings.MaxLoadedGames <= 0)
		{
			return;
		}

		while (_loadedGames.Count > _settings.MaxLoadedGames)
		{
			var candidate = _loadedGames.Keys
				.Where(type => type != _activeGameType)
				.Select(type => new
				{
					Type = type,
					Distance = CalculateDistanceFromCursor(type)
				})
				.OrderByDescending(entry => entry.Distance)
				.ThenBy(entry => entry.Type.Name)
				.FirstOrDefault();

			if (candidate == null)
			{
				break;
			}

			_logger.Log($"Unloading {candidate.Type.Name} to satisfy MaxLoadedGames limit {_settings.MaxLoadedGames}");
			UnloadGame(candidate.Type);
		}
	}

	private void MarkGameActive(Type gameType)
	{
		if (gameType == null)
		{
			return;
		}

		if (_queueService.CurrentGameType != gameType && _pendingActiveGameType != gameType)
		{
			return;
		}

		_activeGameType = gameType;

		if (_pendingActiveGameType == gameType)
		{
			_pendingActiveGameType = null;
		}

		if (_loadedGames.ContainsKey(gameType))
		{
			TrimLoadedGames();
		}
	}

	private void TrimPreloadedGames(ICollection<Type> windowTypes)
	{
		var allowed = new HashSet<Type>(windowTypes);
		if (_activeGameType != null)
		{
			allowed.Add(_activeGameType);
		}
		if (_pendingActiveGameType != null)
		{
			allowed.Add(_pendingActiveGameType);
		}

		var toRemove = _preloadedGames.Keys
			.Where(type => !allowed.Contains(type))
			.ToList();

		foreach (var type in toRemove)
		{
			_logger.Log($"Evicting preloaded game outside window: {type.Name}");
			UnloadGame(type);
		}
	}

	private void SafeStopGame(Type gameType, IShortGame game, string reason)
	{
		if (game == null)
		{
			return;
		}

		try
		{
			game.StopGame();
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to stop game {gameType?.Name} ({reason}): {ex.Message}");
		}
	}

	private void SafeDisableGame(Type gameType, IShortGame game, string reason)
	{
		if (game == null)
		{
			return;
		}

		try
		{
			game.Disable();
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to disable game {gameType?.Name} ({reason}): {ex.Message}");
		}
	}

	private void SafeEnableGame(Type gameType, IShortGame game, string reason)
	{
		if (game == null)
		{
			return;
		}

		try
		{
			game.Enable();
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to enable game {gameType?.Name} ({reason}): {ex.Message}");
		}
	}

	private void SafeDispose(Type gameType, IShortGame game, string reason)
	{
		if (game == null)
		{
			return;
		}

		try
		{
			game.Dispose();
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to dispose game {gameType?.Name} ({reason}): {ex.Message}");
		}
	}

	private int CalculateDistanceFromCursor(Type type)
	{
		var cursor = _queueService.CurrentIndex;
		var targetIndex = GetIndexOfGameType(type);

		if (cursor < 0 || targetIndex < 0)
		{
			return int.MaxValue;
		}

		return Math.Abs(cursor - targetIndex);
	}

	private int GetIndexOfGameType(Type type)
	{
		for (var i = 0; i < _queueService.TotalGamesCount; i++)
		{
			if (_queueService.GetGameTypeAtIndex(i) == type)
			{
				return i;
			}
		}

		return -1;
	}

	private void StopGameIfPossible(Type gameType)
	{
		if (!_loadedGames.TryGetValue(gameType, out var game))
		{
			return;
		}

		try
		{
			_logger.Log($"Stopping previously active game {gameType.Name}");
			game.StopGame();
		}
		catch (Exception ex)
		{
			_logger.LogError($"Failed to stop game {gameType.Name}: {ex.Message}");
		}
	}
}
}