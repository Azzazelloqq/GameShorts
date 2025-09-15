using System;
using System.Threading.Tasks;
using Code.Core.GamesLoader;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Tests.GamesLoader
{
    [TestFixture]
    public class GameProviderTests
    {
        private GameProvider _provider;
        private IGameRegistry _registry;
        private IGameQueueService _queueService;
        private IGamesLoader _loader;
        private MockLogger _logger;
        private Transform _parent;
        private GameObject _parentObject;
        
        [SetUp]
        public async Task SetUp()
        {
            _logger = new MockLogger();
            _parentObject = new GameObject("TestParent");
            _parent = _parentObject.transform;
            
            // Setup registry
            _registry = new GameRegistry(_logger);
            _registry.RegisterGames(new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            });
            
            // Setup queue service
            _queueService = new GameQueueService(_logger);
            
            // Setup loader
            var resourceMapping = new System.Collections.Generic.Dictionary<Type, string>
            {
                { typeof(MockShortGame), "MockGame" },
                { typeof(MockPoolableShortGame), "MockPoolableGame" },
                { typeof(MockShortGame2D), "MockGame2D" }
            };
            
            var resourceLoader = new MockResourceLoader();
            SetupMockResources(resourceLoader);
            
            var factory = new AddressableShortGameFactory(_parent, resourceMapping, resourceLoader, _logger);
            _loader = new QueueShortGamesLoader(factory, _queueService, _logger);
            
            // Create and initialize provider
            _provider = new GameProvider(_logger);
            await _provider.InitializeAsync(_registry, _queueService, _loader);
        }
        
        [TearDown]
        public void TearDown()
        {
            _provider?.Dispose();
            _loader?.Dispose();
            
            if (_parentObject != null)
            {
                GameObject.DestroyImmediate(_parentObject);
            }
        }
        
        private void SetupMockResources(MockResourceLoader resourceLoader)
        {
            var mockGamePrefab = new GameObject("MockGamePrefab");
            mockGamePrefab.AddComponent<MockShortGame>();
            resourceLoader.AddResource("MockGame", mockGamePrefab);
            
            var poolableGamePrefab = new GameObject("MockPoolableGamePrefab");
            poolableGamePrefab.AddComponent<MockPoolableShortGame>();
            resourceLoader.AddResource("MockPoolableGame", poolableGamePrefab);
            
            var game2DPrefab = new GameObject("MockGame2DPrefab");
            game2DPrefab.AddComponent<MockShortGame2D>();
            resourceLoader.AddResource("MockGame2D", game2DPrefab);
        }
        
        [Test]
        public void InitializeAsync_SetsUpServices()
        {
            // Assert
            Assert.IsNotNull(_provider.GameRegistry);
            Assert.IsNotNull(_provider.QueueService);
            Assert.IsNotNull(_provider.GamesLoader);
            Assert.AreEqual(_registry, _provider.GameRegistry);
            Assert.AreEqual(_queueService, _provider.QueueService);
            Assert.AreEqual(_loader, _provider.GamesLoader);
        }
        
        [Test]
        public void InitializeAsync_InitializesQueueWithRegisteredGames()
        {
            // Assert
            Assert.AreEqual(3, _queueService.TotalGamesCount);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Initializing queue with 3 registered games"));
        }
        
        [Test]
        public async Task CurrentGame_AfterLoading_ReturnsCorrectGame()
        {
            // Arrange
            _queueService.MoveNext();
            await _loader.LoadGameAsync(typeof(MockShortGame));
            
            // Act
            var currentGame = _provider.CurrentGame;
            
            // Assert
            Assert.IsNotNull(currentGame);
            Assert.IsInstanceOf<MockShortGame>(currentGame);
        }
        
        [Test]
        public async Task NextGame_WhenPreloaded_ReturnsCorrectGame()
        {
            // Arrange
            _queueService.MoveNext(); // Move to first game
            await _provider.UpdatePreloadedGamesAsync();
            
            // Act
            var nextGame = _provider.NextGame;
            
            // Assert
            Assert.IsNotNull(nextGame);
            Assert.IsInstanceOf<MockPoolableShortGame>(nextGame);
        }
        
        [Test]
        public async Task PreviousGame_WhenPreloaded_ReturnsCorrectGame()
        {
            // Arrange
            _queueService.MoveToIndex(1); // Move to middle
            await _provider.UpdatePreloadedGamesAsync();
            
            // Act
            var previousGame = _provider.PreviousGame;
            
            // Assert
            Assert.IsNotNull(previousGame);
            Assert.IsInstanceOf<MockShortGame>(previousGame);
        }
        
        [Test]
        public async Task StartCurrentGame_StartsTheGame()
        {
            // Arrange
            _queueService.MoveNext();
            await _loader.PreloadGameAsync(typeof(MockShortGame));
            
            // Act
            _provider.StartCurrentGame();
            
            // Assert
            var game = _provider.CurrentGame as MockShortGame;
            Assert.IsTrue(game.IsStarted);
        }
        
        [Test]
        public async Task PauseCurrentGame_PausesTheGame()
        {
            // Arrange
            _queueService.MoveNext();
            await _loader.LoadGameAsync(typeof(MockShortGame));
            
            // Act
            _provider.PauseCurrentGame();
            
            // Assert
            var game = _provider.CurrentGame as MockShortGame;
            Assert.IsTrue(game.IsPaused);
        }
        
        [Test]
        public async Task UnpauseCurrentGame_UnpausesTheGame()
        {
            // Arrange
            _queueService.MoveNext();
            await _loader.LoadGameAsync(typeof(MockShortGame));
            _provider.PauseCurrentGame();
            
            // Act
            _provider.UnpauseCurrentGame();
            
            // Assert
            var game = _provider.CurrentGame as MockShortGame;
            Assert.IsFalse(game.IsPaused);
        }
        
        [Test]
        public async Task PauseAllGames_PausesAllLoadedGames()
        {
            // Arrange
            _queueService.MoveToIndex(1);
            await _provider.UpdatePreloadedGamesAsync();
            _provider.StartCurrentGame();
            _provider.StartNextGame();
            _provider.StartPreviousGame();
            
            // Act
            _provider.PauseAllGames();
            
            // Assert
            var current = _provider.CurrentGame as MockPoolableShortGame;
            var next = _provider.NextGame as MockShortGame2D;
            var previous = _provider.PreviousGame as MockShortGame;
            
            Assert.IsTrue(current.IsPaused);
            Assert.IsTrue(next.IsPaused);
            Assert.IsTrue(previous.IsPaused);
        }
        
        [Test]
        public async Task StopCurrentGame_StopsTheGame()
        {
            // Arrange
            _queueService.MoveNext();
            await _loader.LoadGameAsync(typeof(MockShortGame));
            
            // Act
            _provider.StopCurrentGame();
            
            // Assert
            var game = _provider.CurrentGame as MockShortGame;
            Assert.IsFalse(game.IsStarted);
        }
        
        [Test]
        public async Task StopAllGames_StopsAllLoadedGames()
        {
            // Arrange
            _queueService.MoveToIndex(1);
            await _provider.UpdatePreloadedGamesAsync();
            _provider.StartCurrentGame();
            _provider.StartNextGame();
            _provider.StartPreviousGame();
            
            // Act
            _provider.StopAllGames();
            
            // Assert
            var current = _provider.CurrentGame as MockPoolableShortGame;
            var next = _provider.NextGame as MockShortGame2D;
            var previous = _provider.PreviousGame as MockShortGame;
            
            Assert.IsFalse(current.IsStarted);
            Assert.IsFalse(next.IsStarted);
            Assert.IsFalse(previous.IsStarted);
        }
        
        [Test]
        public async Task GetRenderTextures_ReturnsCorrectTextures()
        {
            // Arrange
            _queueService.MoveToIndex(1);
            await _provider.UpdatePreloadedGamesAsync();
            
            // Act
            var currentTexture = _provider.CurrentGameRenderTexture;
            var nextTexture = _provider.NextGameRenderTexture;
            var previousTexture = _provider.PreviousGameRenderTexture;
            
            // Assert
            Assert.IsNotNull(currentTexture);
            Assert.IsNotNull(nextTexture);
            Assert.IsNotNull(previousTexture);
        }
        
        [Test]
        public async Task HasGame_Properties_ReturnCorrectValues()
        {
            // Arrange - No games loaded initially
            Assert.IsFalse(_provider.HasCurrentGame);
            Assert.IsFalse(_provider.HasNextGame);
            Assert.IsFalse(_provider.HasPreviousGame);
            
            // Act - Load games
            _queueService.MoveToIndex(1);
            await _provider.UpdatePreloadedGamesAsync();
            
            // Assert
            Assert.IsTrue(_provider.HasCurrentGame);
            Assert.IsTrue(_provider.HasNextGame);
            Assert.IsTrue(_provider.HasPreviousGame);
        }
        
        [Test]
        public async Task IsGameReady_Properties_ReturnCorrectValues()
        {
            // Arrange
            _queueService.MoveToIndex(1);
            await _provider.UpdatePreloadedGamesAsync();
            
            // Assert - All games should be preloaded and ready
            Assert.IsTrue(_provider.IsCurrentGameReady);
            Assert.IsTrue(_provider.IsNextGameReady);
            Assert.IsTrue(_provider.IsPreviousGameReady);
        }
        
        [Test]
        public async Task UpdatePreloadedGamesAsync_PreloadsCorrectGames()
        {
            // Arrange
            _queueService.MoveToIndex(1);
            
            // Act
            await _provider.UpdatePreloadedGamesAsync();
            
            // Assert
            var gamesToPreload = _queueService.GetGamesToPreload();
            foreach (var gameType in gamesToPreload)
            {
                Assert.IsTrue(_loader.IsGameLoaded(gameType));
            }
        }
        
        [Test]
        public void Dispose_CleansUpResources()
        {
            // Act
            _provider.Dispose();
            
            // Assert
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Disposing GameProvider"));
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Stopping all games"));
        }
        
        [Test]
        public async Task InitializeAsync_NullRegistry_ThrowsException()
        {
            // Arrange
            var newProvider = new GameProvider(_logger);
            
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await newProvider.InitializeAsync(null, _queueService, _loader));
        }
        
        [Test]
        public async Task InitializeAsync_NullQueueService_ThrowsException()
        {
            // Arrange
            var newProvider = new GameProvider(_logger);
            
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await newProvider.InitializeAsync(_registry, null, _loader));
        }
        
        [Test]
        public async Task InitializeAsync_NullLoader_ThrowsException()
        {
            // Arrange
            var newProvider = new GameProvider(_logger);
            
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () =>
                await newProvider.InitializeAsync(_registry, _queueService, null));
        }
    }
}
