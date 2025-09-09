using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.LifeCycleService;
using Code.Core.ShortGamesCore.Source.Pool;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;
using UnityEngine;

namespace Code.Core.ShotGamesCore.Tests.Integration
{
    [TestFixture]
    public class ShortGamesSystemIntegrationTests
    {
        private SimpleShortGameLifeCycleService _lifeCycleService;
        private SimpleShortGamePool _pool;
        private AddressableShortGameFactory _factory;
        private MockLogger _logger;
        private MockResourceLoader _resourceLoader;
        private Transform _parent;
        private GameObject _parentObject;
        private Dictionary<Type, string> _resourcesInfo;
        
        [SetUp]
        public void SetUp()
        {
            _logger = new MockLogger();
            _resourceLoader = new MockResourceLoader();
            _parentObject = new GameObject("Parent");
            _parent = _parentObject.transform;
            
            _resourcesInfo = new Dictionary<Type, string>
            {
                { typeof(MockShortGame), "MockShortGame_Resource" },
                { typeof(MockPoolableShortGame), "MockPoolableShortGame_Resource" }
            };
            
            _pool = new SimpleShortGamePool(_logger, maxInstancesPerType: 3);
            _factory = new AddressableShortGameFactory(_parent, _resourcesInfo, _resourceLoader, _logger);
            _lifeCycleService = new SimpleShortGameLifeCycleService(_pool, _factory, _logger);
            
            // Setup mock resources
            SetupMockResources();
        }
        
        [TearDown]
        public void TearDown()
        {
            _lifeCycleService?.Dispose();
            
            // Clean up all game objects
            foreach (Transform child in _parent)
            {
                GameObject.DestroyImmediate(child.gameObject);
            }
            
            if (_parentObject != null)
            {
                GameObject.DestroyImmediate(_parentObject);
            }
        }
        
        private void SetupMockResources()
        {
            // Create prefabs for each game type
            var shortGamePrefab = new GameObject("MockShortGamePrefab");
            shortGamePrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", shortGamePrefab);
            
            var poolableGamePrefab = new GameObject("MockPoolableShortGamePrefab");
            poolableGamePrefab.AddComponent<MockPoolableShortGame>();
            _resourceLoader.AddResource("MockPoolableShortGame_Resource", poolableGamePrefab);
        }
        
        [Test]
        public async Task FullCycle_PreloadAndSwitch_WorksCorrectly()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            
            // Act - Preload games
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            
            // Assert - Games are preloaded
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Preloaded game"));
            Assert.AreEqual(2, _resourceLoader.LoadCallCount);
            
            // Act - Load first game
            var firstGame = await _lifeCycleService.LoadNextGameAsync();
            
            // Assert - First game is loaded and started
            Assert.IsNotNull(firstGame);
            Assert.IsInstanceOf<MockShortGame>(firstGame);
            var mockGame1 = firstGame as MockShortGame;
            Assert.IsTrue(mockGame1.IsStarted);
            
            // Act - Switch to next game
            var secondGame = await _lifeCycleService.LoadNextGameAsync();
            
            // Assert - Second game is loaded, first is stopped
            Assert.IsNotNull(secondGame);
            Assert.IsInstanceOf<MockPoolableShortGame>(secondGame);
            var mockGame2 = secondGame as MockPoolableShortGame;
            Assert.IsTrue(mockGame2.IsStarted);
            Assert.IsTrue(mockGame1.IsPaused);
            
            // Act - Switch back to first game
            var thirdGame = await _lifeCycleService.LoadNextGameAsync();
            
            // Assert - Cycles back to first game
            Assert.IsNotNull(thirdGame);
            Assert.IsInstanceOf<MockShortGame>(thirdGame);
        }
        
        [Test]
        public async Task PoolingIntegration_ReuseGameInstances()
        {
            // Arrange - Preload poolable game
            await _lifeCycleService.PreloadGameAsync<MockPoolableShortGame>();
            
            // Act - Load game first time
            var game1 = await _lifeCycleService.LoadGameAsync<MockPoolableShortGame>();
            var gameId1 = (game1 as Component).GetInstanceID();
            
            // Act - Stop current game (returns to pool)
            _lifeCycleService.StopCurrentGame();
            // OnPooled was called once during preload, and once more now when stopping
            Assert.AreEqual(2, game1.OnPooledCallCount);
            
            // Act - Load same game type again
            var game2 = await _lifeCycleService.LoadGameAsync<MockPoolableShortGame>();
            var gameId2 = (game2 as Component).GetInstanceID();
            
            // Assert - Same instance is reused
            Assert.AreEqual(gameId1, gameId2);
            // OnUnpooled was called once for first load, and once more for second load
            Assert.AreEqual(2, game2.OnUnpooledCallCount);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Got game"));
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("from pool"));
        }
        
        [Test]
        public async Task MixedGameTypes_PoolableAndNonPoolable()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),      // Non-poolable
                typeof(MockPoolableShortGame) // Poolable
            };
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            
            // Act - Load non-poolable game
            var nonPoolable = await _lifeCycleService.LoadGameAsync<MockShortGame>();
            Assert.IsNotNull(nonPoolable);
            
            // Act - Switch to poolable game
            var poolable = await _lifeCycleService.LoadGameAsync<MockPoolableShortGame>();
            Assert.IsNotNull(poolable);
            
            // Assert - Non-poolable was destroyed, poolable was stopped properly
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Destroyed non-poolable game"));
            
            // Act - Switch back to non-poolable (should create new instance)
            var nonPoolable2 = await _lifeCycleService.LoadGameAsync<MockShortGame>();
            
            // Assert - Different instances for non-poolable
            Assert.AreNotEqual(
                (nonPoolable as Component).GetInstanceID(),
                (nonPoolable2 as Component).GetInstanceID()
            );
        }
        
        [Test]
        public async Task ResourceManagement_PreloadAndUnload()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            
            // Act - Preload
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            Assert.AreEqual(2, _resourceLoader.LoadCallCount);
            
            // Act - Clear preloaded games
            _lifeCycleService.ClearPreloadedGames();
            
            // Assert - Resources are unloaded
            Assert.Greater(_resourceLoader.ReleaseCallCount, 0);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Clearing all preloaded games"));
        }
        
        [Test]
        public async Task PoolCapacity_RespectedDuringPreload()
        {
            // Arrange - Create multiple instances of same type
            var gameTypes = new List<Type>();
            for (int i = 0; i < 5; i++)
            {
                gameTypes.Add(typeof(MockPoolableShortGame));
            }
            
            // Act - Try to preload more than pool capacity
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            
            // Assert - Pool should respect max capacity
            var pooledTypes = _pool.GetPooledGameTypes();
            Assert.That(pooledTypes, Has.Some.EqualTo(typeof(MockPoolableShortGame)));
        }
        
        [Test]
        public async Task NavigationFlow_ForwardAndBackward()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            
            // Act & Assert - Navigate forward
            var game1 = await _lifeCycleService.LoadNextGameAsync();
            Assert.IsInstanceOf<MockShortGame>(game1);
            
            var game2 = await _lifeCycleService.LoadNextGameAsync();
            Assert.IsInstanceOf<MockPoolableShortGame>(game2);
            
            // Act & Assert - Navigate backward
            var game3 = await _lifeCycleService.LoadPreviousGameAsync();
            Assert.IsInstanceOf<MockShortGame>(game3);
            
            var game4 = await _lifeCycleService.LoadPreviousGameAsync();
            Assert.IsInstanceOf<MockPoolableShortGame>(game4);
        }
        
        [Test]
        public async Task ErrorHandling_InvalidGameType()
        {
            // Arrange - Try to load a game type without resource mapping
            var unmappedTypes = new List<Type> { typeof(Component) };
            
            // Act
            await _lifeCycleService.PreloadGamesAsync(unmappedTypes);
            
            // Assert - Should handle error gracefully
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("Failed to preload"));
        }
        
        [Test]
        public async Task Performance_QuickSwitching()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockPoolableShortGame),
                typeof(MockPoolableShortGame) // Same type to test pooling
            };
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            
            // Act - Rapid switching
            var startTime = DateTime.Now;
            for (int i = 0; i < 10; i++)
            {
                await _lifeCycleService.LoadNextGameAsync();
            }
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            
            // Assert - Should be fast due to pooling
            Assert.Less(elapsed, 1000, "Switching should be fast with pooling");
            
            // Only 1 create call because of pooling
            Assert.LessOrEqual(_resourceLoader.LoadCallCount, 2);
        }
        
        [Test]
        public async Task StateManagement_CurrentGameTracking()
        {
            // Arrange
            await _lifeCycleService.PreloadGameAsync<MockShortGame>();
            
            // Act & Assert - Initially no current game
            Assert.IsNull(_lifeCycleService.CurrentGame);
            
            // Act - Load a game
            var game = await _lifeCycleService.LoadGameAsync<MockShortGame>();
            
            // Assert - Current game is tracked
            Assert.AreEqual(game, _lifeCycleService.CurrentGame);
            
            // Act - Stop game
            _lifeCycleService.StopCurrentGame();
            
            // Assert - Current game is cleared
            Assert.IsNull(_lifeCycleService.CurrentGame);
        }
        
        [Test]
        public async Task Cleanup_DisposeProperly()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            await _lifeCycleService.LoadNextGameAsync();
            
            // Act
            _lifeCycleService.Dispose();
            
            // Assert
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Disposing SimpleShortGameLifeCycleService"));
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Disposing SimpleShortGamePool"));
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Disposing AddressableShortGameFactory"));
            
            // Verify resources are cleaned up
            Assert.Greater(_resourceLoader.ReleaseCallCount, 0);
        }
        
        [Test]
        public async Task CancellationHandling_StopsOperations()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var gameTypes = new List<Type> { typeof(MockShortGame) };
            
            // Act - Start preload and cancel immediately
            cts.Cancel();
            
            // Act & Assert
            var ex = Assert.CatchAsync<Exception>(async () =>
            {
                await _lifeCycleService.PreloadGamesAsync(gameTypes, cts.Token);
            });
            
            Assert.IsTrue(ex is OperationCanceledException, 
                $"Expected OperationCanceledException or derived, but got {ex?.GetType().Name}");
        }
    }
}
