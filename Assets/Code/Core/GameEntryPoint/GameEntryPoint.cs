using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asteroids.Code.Games.Game1;
using Code.Core.GamesLoader;
using Code.Core.GameSwiper;
using Code.Core.GameStats;
using Code.Core.ShortGamesCore.EscapeFromDark;
using Code.Core.ShortGamesCore.Game2;
using Code.Core.ShortGamesCore.Lawnmower;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShortGamesCore.Source.Pool;
using Code.Core.Tools.Pool;
using Code.Games;
using Code.Games.AngryHumans;
using Code.Games.TestGames;
using Code.Generated.Addressables;
using GameShorts.CubeRunner;
using GameShorts.Gardener;
using InGameLogger;
using LightDI.Runtime;
using ResourceLoader;
using ResourceLoader.AddressableResourceLoader;
using TickHandler;
using TickHandler.UnityTickHandler;
using UnityEngine;

namespace Code.Core.GameEntryPoint
{
/// <summary>
/// Simple entry point for game system initialization
/// </summary>
public sealed class GameEntryPoint : MonoBehaviour
{
	[Header("Settings")]
	[SerializeField]
	private Transform _gamesParent;

	[SerializeField]
	private int _preloadDepth = 2;

	[Header("UI Settings")]
	[SerializeField]
	private Transform _uiParent;

	[SerializeField]
	private GamePositioningConfig _gamePositioningConfig;

	private IShortGameServiceProvider _shortGameServiceProvider;
	private GameSwiperController _gameSwiperController;
	private IGameStatsService _gameStatsService;
	private CancellationTokenSource _cancellationTokenSource;
	private IDiContainer _globalGameDiContainer;
	private UnityInGameLogger _logger;
	private SimpleShortGamePool _pool;
	private AddressableResourceLoader _resourceLoader;
	private UnityTickHandler _tickHandler;
	private PoolManager _poolObjects;

	private async void Start()
	{
		try
		{
			_cancellationTokenSource = new CancellationTokenSource();
			var exitGameCancellationToken = Application.exitCancellationToken;

			if (_gamesParent == null)
			{
				_gamesParent = transform;
			}

			if (_uiParent == null)
			{
				var uiRoot = new GameObject("UI Root");
				_uiParent = uiRoot.transform;
				_uiParent.SetParent(transform);
			}

			_globalGameDiContainer = DiContainerFactory.CreateContainer();

			await InitializeAsync(exitGameCancellationToken);
		}
		catch (Exception e)
		{
			_logger.LogException(e);
		}
	}

	private async Task InitializeAsync(CancellationToken cancellationToken)
	{
		_logger = new UnityInGameLogger();
		_globalGameDiContainer.RegisterAsSingleton<IInGameLogger>(_logger);

		_poolObjects = new PoolManager();
		_globalGameDiContainer.RegisterAsSingleton<IPoolManager>(_poolObjects);

		_resourceLoader = new AddressableResourceLoader();
		_globalGameDiContainer.RegisterAsSingleton<IResourceLoader>(_resourceLoader);

		var dispatcher = gameObject.AddComponent<UnityDispatcherBehaviour>();
		_tickHandler = new UnityTickHandler(dispatcher);
		_globalGameDiContainer.RegisterAsSingleton<ITickHandler>(_tickHandler);

		_pool = new SimpleShortGamePool(_logger);
		_globalGameDiContainer.RegisterAsSingleton<IShortGamesPool>(_pool);

		var resourceMapping = GetResourceMapping();
		var factory =
			AddressableShortGameFactoryFactory.CreateAddressableShortGameFactory(_gamesParent, resourceMapping,
				_gamePositioningConfig);
		_globalGameDiContainer.RegisterAsSingleton<IShortGameFactory>(factory);

		var gameLoaderSettings = new ShortGameLoaderSettings();
		var games = GetPlayableGames();
		var registry = GameRegistryFactory.CreateGameRegistry();
		_globalGameDiContainer.RegisterAsSingleton<IGameRegistry>(registry);
		
		registry.RegisterGames(games);

		var queueService = GameQueueServiceFactory.CreateGameQueueService();
		queueService.Initialize(registry.RegisteredGames);
		_globalGameDiContainer.RegisterAsSingleton<IGameQueueService>(queueService);
		
		var loader = QueueShortGamesLoaderFactory.CreateQueueShortGamesLoader(gameLoaderSettings);
		_globalGameDiContainer.RegisterAsSingleton<IGamesLoader>(loader);
		
		_shortGameServiceProvider = ShortGameServiceProviderFactory.CreateShortGameServiceProvider(gameLoaderSettings);
		await _shortGameServiceProvider.InitializeAsync(cancellationToken);

		_gameStatsService = new LocalRandomGameStatsService(_logger);

		_gameSwiperController = new GameSwiperController(
			_uiParent,
			_shortGameServiceProvider,
			_logger,
			_resourceLoader,
			_gameStatsService
		);

		await _gameSwiperController.InitializeAsync(cancellationToken);
	}

	/// <summary>
	/// Override this method to provide resource mapping for your games
	/// </summary>
	private Dictionary<Type, string> GetResourceMapping()
	{
		return new Dictionary<Type, string>
		{
			{ typeof(GardenerGame), ResourceIdsContainer.GameGardneer.GardenerGame },
			{ typeof(AngryHumansShortGame), ResourceIdsContainer.GameAngryHumans.MainGame },
			{ typeof(BoxTower), ResourceIdsContainer.GameBoxTower.BoxTower },
			{ typeof(LawnmowerGame), ResourceIdsContainer.GameLawnmover.GameLawnmower },
			{ typeof(EscapeFromDarkGame), ResourceIdsContainer.GameEscapeFromDark.EscapeFromDarkMain },
			{ typeof(Game2048), ResourceIdsContainer.GroupGame2048.Id2048Main },
			{ typeof(AsteroidsGame), ResourceIdsContainer.GameAsteroids.AsteroidGame },
			{ typeof(CubeRunnerGame), ResourceIdsContainer.GameCubeRunner.CubeRunnerGame },
		};
	}

	private IEnumerable<Type> GetPlayableGames()
	{
		return new[]
		{
			typeof(CubeRunnerGame),
			typeof(GardenerGame),
			typeof(BoxTower),
			typeof(LawnmowerGame),
			typeof(EscapeFromDarkGame),
			typeof(Game2048),
			typeof(AngryHumansShortGame),
			typeof(AsteroidsGame),
		};
	}

	private void OnDestroy()
	{
		_logger?.Log("GameEntryPoint OnDestroy - starting cleanup");

		_cancellationTokenSource?.Cancel();

		// Dispose in correct order to prevent errors
		try
		{
			_gameSwiperController?.Dispose();
		}
		catch (Exception ex)
		{
			_logger?.LogError($"Error disposing GameSwiperController: {ex.Message}");
		}

		try
		{
			_globalGameDiContainer?.Dispose();
		}
		catch (Exception ex)
		{
			_logger?.LogError($"Error disposing GlobalGameDiContainer: {ex.Message}");
		}

		_logger?.Log("GameEntryPoint OnDestroy - cleanup completed");
	}
}
}