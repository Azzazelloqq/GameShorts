using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asteroids.Code.Games.Game1;
using Code.Core.GamesLoader;
using Code.Core.GameSwiper;
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
public class GameEntryPoint : MonoBehaviour
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
	private CancellationTokenSource _cancellationTokenSource;
	private IDiContainer _globalGameDiContainer;
	private UnityInGameLogger _logger;
	private SimpleShortGamePool _pool;
	private AddressableResourceLoader _resourceLoader;
	private UnityTickHandler _tickHandler;
	private PoolManager _poolObjects;

	private async void Start()
	{
		_cancellationTokenSource = new CancellationTokenSource();
		var exitGameCancellationToken = Application.exitCancellationToken;

		if (_gamesParent == null)
		{
			_gamesParent = transform;
		}

		if (_uiParent == null)
		{
			// Создаем отдельный родительский объект для UI, если не задан
			var uiRoot = new GameObject("UI Root");
			_uiParent = uiRoot.transform;
			_uiParent.SetParent(transform);
		}

		_globalGameDiContainer = DiContainerFactory.CreateContainer();

		await InitializeAsync(exitGameCancellationToken);
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

		var playableGames = GetPlayableGames();
		_shortGameServiceProvider = ShortGameServiceProviderFactory.CreateShortGameServiceProvider(playableGames, factory);
		await _shortGameServiceProvider.InitializeAsync(cancellationToken);

		_gameSwiperController = new GameSwiperController(
			_uiParent,
			_shortGameServiceProvider,
			_logger,
			_resourceLoader
		);

		await _gameSwiperController.InitializeAsync(cancellationToken);
	}

	/// <summary>
	/// Override this method to provide resource mapping for your games
	/// </summary>
	protected virtual Dictionary<Type, string> GetResourceMapping()
	{
		return new Dictionary<Type, string>
		{
			{ typeof(AngryHumansShortGame), ResourceIdsContainer.GameAngryHumans.MainGame },
			{ typeof(BoxTower), ResourceIdsContainer.GameBoxTower.BoxTower },
			{ typeof(LawnmowerGame), ResourceIdsContainer.GameLawnmover.GameLawnmower },
			{ typeof(EscapeFromDarkGame), ResourceIdsContainer.GameEscapeFromDark.EscapeFromDarkMain },
			{ typeof(Game2048), ResourceIdsContainer.GroupGame2048.Id2048Main},
			{ typeof(AsteroidsGame), ResourceIdsContainer.GameAsteroids.AsteroidGame },
		};
	}

	private IEnumerable<Type> GetPlayableGames()
	{
		return new[]
		{
			typeof(BoxTower),
			typeof(LawnmowerGame),
			typeof(EscapeFromDarkGame),
			typeof(Game2048),
			typeof(AsteroidsGame),
		};
	}

	private void OnDestroy()
	{
		_cancellationTokenSource?.Cancel();

		_gameSwiperController?.Dispose();
		_shortGameServiceProvider?.Dispose();
		_cancellationTokenSource?.Dispose();
		_poolObjects?.Dispose();
		_globalGameDiContainer?.Dispose();
	}
}
}