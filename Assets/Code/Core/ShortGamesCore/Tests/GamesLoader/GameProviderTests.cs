using System;
using System.Collections.Generic;
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
        private List<GameObject> _prefabs;
        
        [SetUp]
        public async Task SetUp()
        {
            _logger = new MockLogger();
            _parentObject = new GameObject("TestParent");
            _parent = _parentObject.transform;
            _prefabs = new List<GameObject>();
            
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
            var resourceMapping = new Dictionary<Type, string>
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
            
            // Clean up prefabs
            if (_prefabs != null)
            {
                foreach (var prefab in _prefabs)
                {
                    if (prefab != null)
                    {
                        GameObject.DestroyImmediate(prefab);
                    }
                }
                _prefabs.Clear();
            }
            
            if (_parentObject != null)
            {
                GameObject.DestroyImmediate(_parentObject);
            }
        }
        
        private void SetupMockResources(MockResourceLoader resourceLoader)
        {
            var mockGamePrefab = new GameObject("MockGamePrefab");
            var mockComponent = mockGamePrefab.AddComponent<MockShortGame>();
            Assert.IsNotNull(mockComponent, "Failed to add MockShortGame component");
            _prefabs.Add(mockGamePrefab);
            resourceLoader.AddResource("MockGame", mockGamePrefab);
            
            var poolableGamePrefab = new GameObject("MockPoolableGamePrefab");
            var poolableComponent = poolableGamePrefab.AddComponent<MockPoolableShortGame>();
            Assert.IsNotNull(poolableComponent, "Failed to add MockPoolableShortGame component");
            _prefabs.Add(poolableGamePrefab);
            resourceLoader.AddResource("MockPoolableGame", poolableGamePrefab);
            
            var game2DPrefab = new GameObject("MockGame2DPrefab");
            var game2DComponent = game2DPrefab.AddComponent<MockShortGame2D>();
            Assert.IsNotNull(game2DComponent, "Failed to add MockShortGame2D component");
            _prefabs.Add(game2DPrefab);
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
            _queueService.MoveNext(); // Move to first game (index 0)
            var loadedGame = await _loader.LoadGameAsync(typeof(MockShortGame));
            Assert.IsNotNull(loadedGame, "Failed to load MockShortGame");
            
            // Act
            var currentGame = _provider.CurrentGame;
            
            // Assert
            Assert.IsNotNull(currentGame, "CurrentGame should not be null after loading");
            Assert.IsInstanceOf<MockShortGame>(currentGame);
        }
        
        [Test]
        public async Task NextGame_WhenPreloaded_ReturnsCorrectGame()
        {
            // Arrange
            _queueService.MoveNext(); // Move to first game (index 0)
            await _provider.UpdatePreloadedGamesAsync();
            
            // Verify queue state
            Assert.IsTrue(_queueService.HasNext, "Should have next game");
            Assert.AreEqual(typeof(MockPoolableShortGame), _queueService.NextGameType);
            
            // Act
            var nextGame = _provider.NextGame;
            
            // Assert
            Assert.IsNotNull(nextGame, "NextGame should not be null after preloading");
            Assert.IsInstanceOf<MockPoolableShortGame>(nextGame);
        }
        
        [Test]
        public async Task PreviousGame_WhenPreloaded_ReturnsCorrectGame()
        {
            // Arrange
            _queueService.MoveToIndex(1); // Move to middle (index 1)
            await _provider.UpdatePreloadedGamesAsync();
            
            // Verify queue state
            Assert.IsTrue(_queueService.HasPrevious, "Should have previous game");
            Assert.AreEqual(typeof(MockShortGame), _queueService.PreviousGameType);
            
            // Act
            var previousGame = _provider.PreviousGame;
            
            // Assert
            Assert.IsNotNull(previousGame, "PreviousGame should not be null after preloading");
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
            var loadedGame = await _loader.LoadGameAsync(typeof(MockShortGame));
            Assert.IsNotNull(loadedGame, "Failed to load game");
            
            // Act
            _provider.PauseCurrentGame();
            
            // Assert
            var game = _provider.CurrentGame as MockShortGame;
            Assert.IsNotNull(game, "CurrentGame should not be null");
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
            
            // Verify games are loaded
            Assert.IsNotNull(_provider.CurrentGame, "Current game should be loaded");
            Assert.IsNotNull(_provider.NextGame, "Next game should be loaded");
            Assert.IsNotNull(_provider.PreviousGame, "Previous game should be loaded");
            
            _provider.StartCurrentGame();
            _provider.StartNextGame();
            _provider.StartPreviousGame();
            
            // Act
            _provider.PauseAllGames();
            
            // Assert
            var current = _provider.CurrentGame as MockPoolableShortGame;
            var next = _provider.NextGame as MockShortGame2D;
            var previous = _provider.PreviousGame as MockShortGame;
            
            Assert.IsNotNull(current, "Current game cast failed");
            Assert.IsNotNull(next, "Next game cast failed");
            Assert.IsNotNull(previous, "Previous game cast failed");
            
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
        
        [Test]
        public async Task BasicLoadingTest_VerifySetup()
        {
            // This test verifies the basic setup works
            
            // Verify queue was initialized by GameProvider
            Assert.AreEqual(3, _queueService.TotalGamesCount, "Queue should have 3 games");
            Assert.AreEqual(-1, _queueService.CurrentIndex, "Initial index should be -1");
            
            // Arrange & Act
            var moveResult = _queueService.MoveNext(); // Move to index 0
            Assert.IsTrue(moveResult, "MoveNext should return true");
            Assert.AreEqual(0, _queueService.CurrentIndex, "Index should be 0 after MoveNext");
            
            var gameType = _queueService.CurrentGameType;
            Assert.IsNotNull(gameType, "CurrentGameType should not be null after MoveNext");
            Assert.AreEqual(typeof(MockShortGame), gameType);
            
            // Try to load the game directly
            var game = await _loader.LoadGameAsync(gameType);
            
            // Assert
            Assert.IsNotNull(game, "Game should be loaded successfully");
            Assert.IsInstanceOf<MockShortGame>(game);
            
            // Check if we can get it back
            var retrievedGame = _loader.GetGame(gameType);
            Assert.IsNotNull(retrievedGame, "Should be able to retrieve loaded game");
            Assert.AreEqual(game, retrievedGame);
        }
        
        [Test]
        public async Task BasicPreloadingTest_VerifySetup()
        {
            // This test verifies preloading works
            
            // Arrange & Act
            var gameType = typeof(MockShortGame);
            
            // Log initial state
            Assert.IsNotNull(_loader, "Loader should not be null");
            Assert.IsNotNull(_queueService, "Queue service should not be null");
            
            // Check queue is initialized
            if (_queueService.TotalGamesCount == 0)
            {
                _queueService.Initialize(new[] { gameType });
            }
            
            var game = await _loader.PreloadGameAsync(gameType);
            
            // Assert
            Assert.IsNotNull(game, "Game should be preloaded successfully");
            
            // Cast to check it's the right type
            var mockGame = game as MockShortGame;
            Assert.IsNotNull(mockGame, "Game should be MockShortGame type");
            
            // Check preloaded flag
            Assert.IsTrue(game.IsPreloaded, "Game should be marked as preloaded");
            
            // Check if we can get it back
            var retrievedGame = _loader.GetGame(gameType);
            Assert.IsNotNull(retrievedGame, "Should be able to retrieve preloaded game");
            Assert.AreEqual(game, retrievedGame);
        }
    }
}

