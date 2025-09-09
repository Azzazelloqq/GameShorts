using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.GamesLoader;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShortGamesCore.Source.LifeCycleService;
using Code.Core.ShortGamesCore.Source.Pool;
using InGameLogger;
using LightDI.Runtime;
using ResourceLoader;
using ResourceLoader.AddressableResourceLoader;
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
        
        private QueueShortGamesLoader _queueLoader;
        private IShortGameLifeCycleService _lifeCycleService;
        private CancellationTokenSource _cancellationTokenSource;
        private IDiContainer _globalGameDiContainer;

        /// <summary>
        /// Gets the queue loader
        /// </summary>
        public QueueShortGamesLoader QueueLoader => _queueLoader;
        
        /// <summary>
        /// Gets the current game
        /// </summary>
        public IShortGame CurrentGame => _queueLoader?.CurrentGame;
        
        private async void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            var exitGameCancellationToken = Application.exitCancellationToken;
            
            if (_gamesParent == null)
            {
                _gamesParent = transform;
            }

            _globalGameDiContainer = DiContainerFactory.CreateContainer();

            await InitializeAsync(exitGameCancellationToken);
        }
        
        private async Task InitializeAsync(CancellationToken cancellationToken)
        {
            var logger = new UnityInGameLogger();
            _globalGameDiContainer.RegisterAsSingleton<IInGameLogger>(logger);
            
            var pool = new SimpleShortGamePool(logger);
            _globalGameDiContainer.RegisterAsSingleton<IShortGamesPool>(pool);

            var resourceLoader = new AddressableResourceLoader();
            _globalGameDiContainer.RegisterAsSingleton<IResourceLoader>(resourceLoader);
            
            var resourceMapping = GetResourceMapping();
            var factory = AddressableShortGameFactoryFactory.CreateAddressableShortGameFactory(_gamesParent, resourceMapping);
            _globalGameDiContainer.RegisterAsSingleton<IShortGameFactory>(factory);
            
            _lifeCycleService = new SimpleShortGameLifeCycleService(pool, factory, logger);
            _globalGameDiContainer.RegisterAsSingleton(_lifeCycleService);
            
            _queueLoader = new QueueShortGamesLoader(_lifeCycleService, logger)
            {
                PreloadDepth = _preloadDepth
            };

            var games = GetGameTypes();
            await _queueLoader.InitializeAsync(games, cancellationToken);
            _globalGameDiContainer.RegisterAsSingleton<IGamesLoader>(_queueLoader);
        }

        private IReadOnlyList<Type> GetGameTypes()
        {
            var types = new[]
            {
                this.GetType(), 
            };

            return types;
        }
        
        /// <summary>
        /// Override this method to provide resource mapping for your games
        /// </summary>
        protected virtual Dictionary<Type, string> GetResourceMapping()
        {
            // Override this in your implementation
            // Example:
            // return new Dictionary<Type, string>
            // {
            //     { typeof(MyGame1), "Games/Game1" },
            //     { typeof(MyGame2), "Games/Game2" }
            // };
            return new Dictionary<Type, string>();
        }
        
        private void OnDestroy()
        {
            _cancellationTokenSource.Cancel();
            _queueLoader.Dispose();
            _lifeCycleService.Dispose();
            _cancellationTokenSource.Dispose();
            _globalGameDiContainer.Dispose();
        }
    }
}
