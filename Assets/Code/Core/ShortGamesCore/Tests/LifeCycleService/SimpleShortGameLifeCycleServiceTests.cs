using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.LifeCycleService;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;
using UnityEngine;

namespace Code.Core.ShotGamesCore.Tests.LifeCycleService
{
    [TestFixture]
    public class SimpleShortGameLifeCycleServiceTests
    {
        private SimpleShortGameLifeCycleService _service;
        private MockShortGamesPool _pool;
        private MockShortGameFactory _factory;
        private MockLogger _logger;
        private GameObject _testGameObject;
        
        [SetUp]
        public void SetUp()
        {
            _pool = new MockShortGamesPool();
            _factory = new MockShortGameFactory();
            _logger = new MockLogger();
            _service = new SimpleShortGameLifeCycleService(_pool, _factory, _logger);
            _testGameObject = new GameObject("TestGame");
        }
        
        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            if (_testGameObject != null)
            {
                GameObject.DestroyImmediate(_testGameObject);
            }
        }
        
        [Test]
        public async Task LoadGameAsync_NoPooledGame_CreatesNewInstance()
        {
            // Arrange
            _pool.ShouldReturnFromPool = false;
            var mockGame = _testGameObject.AddComponent<MockShortGame>();
            _factory.AddPrefab(typeof(MockShortGame), _testGameObject);
            
            // Act
            var result = await _service.LoadGameAsync<MockShortGame>();
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, _factory.CreateCallCount);
            Assert.Contains(typeof(MockShortGame), _factory.CreatedTypes);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Created new game instance"));
        }
        
        [Test]
        public async Task LoadGameAsync_WithPooledGame_ReturnsFromPool()
        {
            // Arrange
            var mockGame = _testGameObject.AddComponent<MockPoolableShortGame>();
            _pool.WarmUpPool(mockGame);
            _pool.ShouldReturnFromPool = true;
            
            // Act
            var result = await _service.LoadGameAsync<MockPoolableShortGame>();
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(mockGame, result);
            Assert.AreEqual(1, mockGame.OnUnpooledCallCount);
            Assert.AreEqual(0, _factory.CreateCallCount);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Got game"));
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("from pool"));
        }
        
        [Test]
        public async Task LoadGameAsync_StartsGame()
        {
            // Arrange
            _pool.ShouldReturnFromPool = false;
            var mockGame = _testGameObject.AddComponent<MockShortGame>();
            _factory.AddPrefab(typeof(MockShortGame), _testGameObject);
            
            // Act
            var result = await _service.LoadGameAsync<MockShortGame>();
            
            // Assert
            Assert.IsTrue(result.IsStarted);
            Assert.AreEqual(1, result.StartCallCount);
        }
        
        [Test]
        public async Task LoadGameAsync_StopsCurrentGame()
        {
            // Arrange
            _pool.ShouldReturnFromPool = false;
            var game1 = new GameObject("Game1").AddComponent<MockShortGame>();
            var game2 = new GameObject("Game2").AddComponent<MockShortGame>();
            _factory.AddPrefab(typeof(MockShortGame), game1.gameObject);
            
            // Load first game
            var firstGame = await _service.LoadGameAsync<MockShortGame>();
            Assert.IsTrue(firstGame.IsStarted);
            
            // Change factory to return second game
            _factory.Clear();
            _factory.AddPrefab(typeof(MockShortGame), game2.gameObject);
            
            // Act - Load second game
            var secondGame = await _service.LoadGameAsync<MockShortGame>();
            
            // Assert
            Assert.IsTrue(firstGame.IsPaused);
            Assert.AreEqual(1, firstGame.PauseCallCount);
            Assert.IsTrue(secondGame.IsStarted);
            
            // Clean up
            GameObject.DestroyImmediate(game1.gameObject);
            GameObject.DestroyImmediate(game2.gameObject);
        }
        
        [Test]
        public async Task PreloadGamesAsync_PreloadsMultipleGames()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            
            // Act
            await _service.PreloadGamesAsync(gameTypes);
            
            // Assert
            Assert.AreEqual(2, _factory.PreloadCallCount);
            Assert.Contains(typeof(MockShortGame), _factory.PreloadedTypes);
            Assert.Contains(typeof(MockPoolableShortGame), _factory.PreloadedTypes);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Preloaded game"));
        }
        
        [Test]
        public async Task PreloadGamesAsync_CreatesPoolableInstances()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockPoolableShortGame) };
            var mockGame = _testGameObject.AddComponent<MockPoolableShortGame>();
            _factory.AddPrefab(typeof(MockPoolableShortGame), _testGameObject);
            
            // Act
            await _service.PreloadGamesAsync(gameTypes);
            
            // Assert
            Assert.AreEqual(1, _factory.CreateCallCount);
            Assert.AreEqual(1, _pool.WarmUpCallCount);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Added"));
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("to pool for preloading"));
        }
        
        [Test]
        public void StopCurrentGame_NoCurrentGame_DoesNothing()
        {
            // Act
            _service.StopCurrentGame();
            
            // Assert
            Assert.AreEqual(0, _pool.ReleaseCallCount);
            Assert.That(_logger.LoggedMessages, Has.Count.EqualTo(0));
        }
        
        [Test]
        public async Task StopCurrentGame_WithPoolableGame_ReturnsToPool()
        {
            // Arrange
            var mockGame = _testGameObject.AddComponent<MockPoolableShortGame>();
            _factory.AddPrefab(typeof(MockPoolableShortGame), _testGameObject);
            _pool.ShouldReturnFromPool = false;
            
            var game = await _service.LoadGameAsync<MockPoolableShortGame>();
            
            // Act
            _service.StopCurrentGame();
            
            // Assert
            Assert.AreEqual(1, game.OnPooledCallCount);
            Assert.AreEqual(1, _pool.ReleaseCallCount);
            Assert.IsNull(_service.CurrentGame);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Returned game"));
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("to pool"));
        }
        
        [Test]
        public async Task StopCurrentGame_WithNonPoolableGame_DestroysGame()
        {
            // Arrange
            var mockGame = _testGameObject.AddComponent<MockShortGame>();
            _factory.AddPrefab(typeof(MockShortGame), _testGameObject);
            _pool.ShouldReturnFromPool = false;
            
            var game = await _service.LoadGameAsync<MockShortGame>();
            
            // Act
            _service.StopCurrentGame();
            
            // Assert
            Assert.AreEqual(0, _pool.ReleaseCallCount);
            Assert.IsNull(_service.CurrentGame);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Destroyed non-poolable game"));
        }
        
        [Test]
        public async Task StopCurrentGame_CallsStopMethodOnCurrentGame()
        {
            // Arrange
            var mockGame = _testGameObject.AddComponent<MockShortGame>();
            _factory.AddPrefab(typeof(MockShortGame), _testGameObject);
            _pool.ShouldReturnFromPool = false;
            
            var game = await _service.LoadGameAsync<MockShortGame>();
            
            // Act
            _service.StopCurrentGame();
            
            // Assert
            Assert.AreEqual(1, game.StopCallCount, "Stop method should be called once on current game");
            Assert.IsFalse(game.IsStarted, "Game should not be started after Stop is called");
            Assert.IsFalse(game.IsPaused, "Game should not be paused after Stop is called");
        }
        
        [Test]
        public async Task LoadNextGameAsync_NoPreloadedGames_ReturnsNull()
        {
            // Act
            var result = await _service.LoadNextGameAsync();
            
            // Assert
            Assert.IsNull(result);
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("No preloaded games available"));
        }
        
        [Test]
        public async Task LoadNextGameAsync_CyclesThroughGames()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            await _service.PreloadGamesAsync(gameTypes);
            
            // Act & Assert - First game
            var game1 = await _service.LoadNextGameAsync();
            Assert.IsNotNull(game1);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Loading next game"));
            
            // Act & Assert - Second game
            var game2 = await _service.LoadNextGameAsync();
            Assert.IsNotNull(game2);
            
            // Act & Assert - Should cycle back to first
            var game3 = await _service.LoadNextGameAsync();
            Assert.IsNotNull(game3);
        }
        
        [Test]
        public async Task LoadPreviousGameAsync_NoPreloadedGames_ReturnsNull()
        {
            // Act
            var result = await _service.LoadPreviousGameAsync();
            
            // Assert
            Assert.IsNull(result);
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("No preloaded games available"));
        }
        
        [Test]
        public async Task LoadPreviousGameAsync_CyclesThroughGamesBackward()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            await _service.PreloadGamesAsync(gameTypes);
            
            // Act & Assert
            var game1 = await _service.LoadPreviousGameAsync();
            Assert.IsNotNull(game1);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Loading previous game"));
        }
        
        [Test]
        public async Task ClearPreloadedGames_ClearsPoolAndUnloadsResources()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            await _service.PreloadGamesAsync(gameTypes);
            
            // Act
            _service.ClearPreloadedGames();
            
            // Assert
            Assert.AreEqual(2, _pool.ClearCallCount);
            Assert.AreEqual(2, _factory.UnloadCallCount);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Clearing all preloaded games"));
        }
        
        [Test]
        public async Task PreloadGameAsync_SingleGame_AddsToList()
        {
            // Act
            await _service.PreloadGameAsync<MockShortGame>();
            
            // Assert
            Assert.AreEqual(1, _factory.PreloadCallCount);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains($"Preloaded game: {nameof(MockShortGame)}"));
        }
        
        [Test]
        public async Task LoadGameAsync_ByType_WorksCorrectly()
        {
            // Arrange
            _pool.ShouldReturnFromPool = false;
            var mockGame = _testGameObject.AddComponent<MockShortGame>();
            _factory.AddPrefab(typeof(MockShortGame), _testGameObject);
            
            // Act
            var result = await _service.LoadGameAsync(typeof(MockShortGame));
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<MockShortGame>(result);
            Assert.AreEqual(1, _factory.CreateCallCount);
        }
        
        [Test]
        public void Dispose_CleansUpResources()
        {
            // Act
            _service.Dispose();
            
            // Assert
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Disposing SimpleShortGameLifeCycleService"));
        }
        
        [Test]
        public async Task PreloadGamesAsync_HandlesFailures()
        {
            // Arrange
            // Используем MockPoolableShortGame, потому что ошибка возникает только при создании для пула
            var gameTypes = new List<Type> { typeof(MockPoolableShortGame) };
            _factory.ShouldThrowOnCreate = true;
            
            // Act
            await _service.PreloadGamesAsync(gameTypes);
            
            // Assert
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("Failed to preload game"));
        }
        
        [Test]
        public async Task CurrentGame_ReturnsActiveGame()
        {
            // Arrange
            _pool.ShouldReturnFromPool = false;
            var mockGame = _testGameObject.AddComponent<MockShortGame>();
            _factory.AddPrefab(typeof(MockShortGame), _testGameObject);
            
            // Act
            var loadedGame = await _service.LoadGameAsync<MockShortGame>();
            
            // Assert
            Assert.AreEqual(loadedGame, _service.CurrentGame);
        }
        
        [Test]
        public async Task LoadGameAsync_UpdatesCurrentGameIndex()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            await _service.PreloadGamesAsync(gameTypes);
            
            // Act
            await _service.LoadGameAsync<MockPoolableShortGame>();
            
            // Assert
            // Index should be updated to 1 (second game in list)
            // We can verify this by loading next game
            var nextGame = await _service.LoadNextGameAsync();
            Assert.IsNotNull(nextGame);
            Assert.IsInstanceOf<MockShortGame>(nextGame);
        }
    }
}
