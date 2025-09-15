using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.GamesLoader;
using Code.Core.GameSwiper;
using Code.Core.ShortGamesCore.Game1;
using Code.Core.ShortGamesCore.Game2;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShortGamesCore.Source.Pool;
using Code.Core.Tools.Pool;
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
        [SerializeField] private Transform _gamesParent;
        [SerializeField] private int _preloadDepth = 2;
        
        [Header("UI Settings")]
        [SerializeField] private Transform _uiParent;
        
        [SerializeField]
        private GamePositioningConfig  _gamePositioningConfig;
        
        private IGameRegistry _gameRegistry;
        private IGameQueueService _queueService;
        private IGamesLoader _gamesLoader;
        private IGameProvider _gameProvider;
        private GameSwiper.GameSwiper _gameSwiper;
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
            var factory = AddressableShortGameFactoryFactory.CreateAddressableShortGameFactory(_gamesParent, resourceMapping, _gamePositioningConfig);
            _globalGameDiContainer.RegisterAsSingleton<IShortGameFactory>(factory);
            
            // Create new game system components
            _gameRegistry = new GameRegistry(_logger);
            var games = GetGameTypes();
            _gameRegistry.RegisterGames(games);
            _globalGameDiContainer.RegisterAsSingleton(_gameRegistry);
            
            _queueService = new GameQueueService(_logger);
            _globalGameDiContainer.RegisterAsSingleton(_queueService);
            
            _gamesLoader = new QueueShortGamesLoader(factory, _queueService, _logger);
            _globalGameDiContainer.RegisterAsSingleton(_gamesLoader);
            
            _gameProvider = new GameProvider(_logger);
            await _gameProvider.InitializeAsync(_gameRegistry, _queueService, _gamesLoader, cancellationToken);
            _globalGameDiContainer.RegisterAsSingleton(_gameProvider);
            
            // Создаем контроллер
            
            _gameSwiperController = GameSwiperControllerFactory.CreateGameSwiperController(new Ctx()
            {
                PlaceForAllUi = _uiParent,
            });
            _globalGameDiContainer.RegisterAsSingleton(_gameSwiperController);

        }

        private IReadOnlyList<Type> GetGameTypes()
        {
            var types = new[]
            {
                typeof(Game1), 
                typeof(Game2)
            };

            return types;
        }

        /// <summary>
        /// Override this method to provide resource mapping for your games
        /// </summary>
        protected virtual Dictionary<Type, string> GetResourceMapping()
        {
            return new Dictionary<Type, string>
            {
                { typeof(Game1), ResourceIdsContainer.GameAsteroids.Game1MAIN },
                { typeof(Game2), ResourceIdsContainer.DefaultLocalGroup.Game2 }
            };
           // return new Dictionary<Type, string>();

        }
        
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            
            _gameSwiperController?.Dispose();
            _gameProvider?.Dispose();
            _gamesLoader?.Dispose();
            _queueService?.Clear();
            _gameRegistry?.Clear();
            _cancellationTokenSource?.Dispose();
            _poolObjects?.Dispose();
            _globalGameDiContainer?.Dispose();
        }
    }
}
