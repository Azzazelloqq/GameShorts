using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Asteroids.Code.Games.Game1;
using Code.Core.GamesLoader;
using Code.Core.GameSwiper;
using Code.Core.ShortGamesCore.Game1;
using Code.Core.ShortGamesCore.Game2;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShortGamesCore.Source.LifeCycleService;
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
using static Code.Generated.Addressables.ResourceIdsContainer;

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
        
        private IGamesLoader _queueLoader;
        private IShortGameLifeCycleService _lifeCycleService;
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
            
            _pool = new SimpleShortGamePool(_logger);
            _globalGameDiContainer.RegisterAsSingleton<IShortGamesPool>(_pool);

            _poolObjects = new PoolManager();
            _globalGameDiContainer.RegisterAsSingleton<IPoolManager>(_poolObjects);

            _resourceLoader = new AddressableResourceLoader();
            _globalGameDiContainer.RegisterAsSingleton<IResourceLoader>(_resourceLoader);

            var dispatcher = gameObject.AddComponent<UnityDispatcherBehaviour>();
            _tickHandler = new UnityTickHandler(dispatcher);
            _globalGameDiContainer.RegisterAsSingleton<ITickHandler>(_tickHandler);
            
            var resourceMapping = GetResourceMapping();
            var factory = AddressableShortGameFactoryFactory.CreateAddressableShortGameFactory(_gamesParent, resourceMapping);
            _globalGameDiContainer.RegisterAsSingleton<IShortGameFactory>(factory);
            
            _lifeCycleService = new SimpleShortGameLifeCycleService(_pool, factory, _logger);
            _globalGameDiContainer.RegisterAsSingleton(_lifeCycleService);
            
            _queueLoader = new QueueShortGamesLoader(_lifeCycleService, _logger)
            {
                PreloadDepth = _preloadDepth
            };

            var games = GetGameTypes();
            await _queueLoader.InitializeAsync(games, cancellationToken);
            _globalGameDiContainer.RegisterAsSingleton(_queueLoader);
            
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
                typeof(AsteroidsGame), 
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
                { typeof(AsteroidsGame), ResourceIdsContainer.GameAsteroids.Game1MAIN },
                { typeof(Game2), ResourceIdsContainer.GameBoxTower.Game2Main }
            };
           // return new Dictionary<Type, string>();

        }
        
        private void OnDestroy()
        {
            _cancellationTokenSource?.Cancel();
            
            _gameSwiperController?.Dispose();
            _queueLoader?.Dispose();
            _lifeCycleService?.Dispose();
            _cancellationTokenSource?.Dispose();
            _poolObjects?.Dispose();
            _globalGameDiContainer?.Dispose();
        }
    }
}
