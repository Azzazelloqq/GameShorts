using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Code.Core.GamesLoader;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShortGamesCore.Source.Pool;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Code.Core.ShotGamesCore.Tests.Performance
{
    /// <summary>
    /// Performance tests for the ShortGames system with new architecture
    /// </summary>
    [TestFixture]
    public class PerformanceTests
    {
        private QueueShortGamesLoader _loader;
        private IGameQueueService _queueService;
        private IGameRegistry _registry;
        private SimpleShortGamePool _pool;
        private IShortGameFactory _factory;
        private MockLogger _logger;
        private GameObject _parentObject;
        private Transform _parent;
        private List<GameObject> _prefabs;
        
        [SetUp]
        public void SetUp()
        {
            _logger = new MockLogger();
            _pool = new SimpleShortGamePool(_logger, maxInstancesPerType: 10);
            _parentObject = new GameObject("Parent");
            _parent = _parentObject.transform;
            _prefabs = new List<GameObject>();
            
            // Setup mock factory with resources
            var resourceMapping = new Dictionary<Type, string>
            {
                { typeof(MockShortGame), "MockGame" },
                { typeof(MockPoolableShortGame), "MockPoolableGame" },
                { typeof(MockShortGame2D), "MockGame2D" }
            };
            
            var resourceLoader = new MockResourceLoader();
            SetupMockResources(resourceLoader);
            _factory = new AddressableShortGameFactory(_parent, resourceMapping, resourceLoader, _logger);
            
            // Setup new architecture components
            _registry = new GameRegistry(_logger);
            _queueService = new GameQueueService(_logger);
            var settings = new ShortGameLoaderSettings();
            
            _loader = new QueueShortGamesLoader(_factory, _queueService, _logger, settings);
        }
        
        [TearDown]
        public void TearDown()
        {
            _loader?.Dispose();
            _pool?.Dispose();
            
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
            mockGamePrefab.AddComponent<MockShortGame>();
            _prefabs.Add(mockGamePrefab);
            resourceLoader.AddResource("MockGame", mockGamePrefab);
            
            var poolableGamePrefab = new GameObject("MockPoolableGamePrefab");
            poolableGamePrefab.AddComponent<MockPoolableShortGame>();
            _prefabs.Add(poolableGamePrefab);
            resourceLoader.AddResource("MockPoolableGame", poolableGamePrefab);
            
            var game2DPrefab = new GameObject("MockGame2DPrefab");
            game2DPrefab.AddComponent<MockShortGame2D>();
            _prefabs.Add(game2DPrefab);
            resourceLoader.AddResource("MockGame2D", game2DPrefab);
        }
        
        [Test]
        public void Pool_GetAndRelease_Performance()
        {
            // Arrange
            var games = new List<MockPoolableShortGame>();
            for (int i = 0; i < 10; i++)
            {
                var go = new GameObject($"Game_{i}");
                var game = go.AddComponent<MockPoolableShortGame>();
                games.Add(game);
                _pool.ReleaseShortGame(game);
            }
            
            // Act - Measure get and release performance
            var sw = Stopwatch.StartNew();
            const int iterations = 100;
            
            for (int i = 0; i < iterations; i++)
            {
                _pool.TryGetShortGame<MockPoolableShortGame>(out var game);
                _pool.ReleaseShortGame(game);
            }
            
            sw.Stop();
            
            // Assert
            var avgTime = sw.ElapsedMilliseconds / (float)iterations;
            Debug.Log($"Pool Get/Release avg time: {avgTime:F2}ms");
            Assert.Less(avgTime, 1.0f, "Pool operations should be very fast");
            
            // Cleanup
            foreach (var game in games)
            {
                GameObject.DestroyImmediate(game.gameObject);
            }
        }
        
        [Test]
        public async Task GameLoading_Performance()
        {
            // Arrange - Register and preload games
            var gameTypes = new List<Type>();
            for (int i = 0; i < 5; i++)
            {
                gameTypes.Add(typeof(MockPoolableShortGame));
            }
            _registry.RegisterGames(gameTypes);
            _queueService.Initialize(_registry.RegisteredGames);
            
            // Preload all games
            await _loader.PreloadGamesAsync(gameTypes);
            
            // Act - Measure loading performance
            var sw = Stopwatch.StartNew();
            const int iterations = 20;
            
            for (int i = 0; i < iterations; i++)
            {
                var gameType = gameTypes[i % gameTypes.Count];
                await _loader.LoadGameAsync(gameType);
            }
            
            sw.Stop();
            
            // Assert
            var avgTime = sw.ElapsedMilliseconds / (float)iterations;
            Debug.Log($"Game loading avg time: {avgTime:F2}ms");
            // In Editor mode with logging, operations are slower than in builds
            var threshold = Application.isEditor ? 100.0f : 10.0f;
            Assert.Less(avgTime, threshold, $"Loading should be fast (< {threshold}ms)");
        }
        
        [Test]
        public async Task GameSwitching_WithQueue_Performance()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            _registry.RegisterGames(gameTypes);
            _queueService.Initialize(_registry.RegisteredGames);
            await _loader.PreloadGamesAsync(gameTypes);
            
            // Move to first game
            _queueService.MoveNext();
            
            // Act - Measure rapid switching through queue
            var sw = Stopwatch.StartNew();
            const int switches = 50;
            
            for (int i = 0; i < switches; i++)
            {
                if (_queueService.HasNext)
                {
                    await _loader.LoadNextGameAsync();
                }
                else
                {
                    // Reset to beginning
                    _queueService.Reset();
                    _queueService.MoveNext();
                    await _loader.LoadGameAsync(_queueService.CurrentGameType);
                }
            }
            
            sw.Stop();
            
            // Assert
            var avgTime = sw.ElapsedMilliseconds / (float)switches;
            Debug.Log($"Game switching avg time: {avgTime:F2}ms");
            // In Editor mode with logging, operations are slower than in builds
            var threshold = Application.isEditor ? 150.0f : 50.0f;
            Assert.Less(avgTime, threshold, $"Game switching should be reasonably fast (< {threshold}ms)");
        }
        
        [Test]
        public async Task Preloading_BatchPerformance()
        {
            // Arrange
            var gameTypes = new List<Type>();
            for (int i = 0; i < 10; i++)
            {
                gameTypes.Add(i % 2 == 0 ? typeof(MockShortGame) : typeof(MockPoolableShortGame));
            }
            _registry.RegisterGames(gameTypes);
            _queueService.Initialize(_registry.RegisteredGames);
            
            // Act - Measure batch preloading time
            var sw = Stopwatch.StartNew();
            var preloaded = await _loader.PreloadGamesAsync(gameTypes);
            sw.Stop();
            
            // Assert
            var avgTime = sw.ElapsedMilliseconds / (float)gameTypes.Count;
            Debug.Log($"Preloading avg time per game: {avgTime:F2}ms");
            Debug.Log($"Total preload time for {gameTypes.Count} games: {sw.ElapsedMilliseconds}ms");
            Debug.Log($"Successfully preloaded: {preloaded.Count} games");
            Assert.Less(avgTime, 200.0f, "Preloading should be reasonably fast per game");
        }
        
        [Test]
        public async Task MemoryUsage_LoadedGames()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            _registry.RegisterGames(gameTypes);
            _queueService.Initialize(_registry.RegisteredGames);
            
            // Act - Load multiple games
            var loadTasks = new List<Task>();
            foreach (var gameType in gameTypes)
            {
                loadTasks.Add(_loader.PreloadGameAsync(gameType).AsTask());
            }
            await Task.WhenAll(loadTasks.ToArray());
            
            var afterLoadMemory = GC.GetTotalMemory(false);
            var memoryUsed = (afterLoadMemory - initialMemory) / 1024f / 1024f;
            
            // Report
            Debug.Log($"Memory used by {gameTypes.Count} loaded games: {memoryUsed:F2} MB");
            Debug.Log($"Average memory per game: {memoryUsed / gameTypes.Count * 1024f:F2} KB");
            
            // Assert - reasonable memory usage
            Assert.Less(memoryUsed, 10f, "Games should not use excessive memory");
        }
        
        [Test]
        public async Task QueueNavigation_Performance()
        {
            // Arrange
            var gameTypes = new List<Type>();
            for (int i = 0; i < 20; i++)
            {
                gameTypes.Add(typeof(MockPoolableShortGame));
            }
            _registry.RegisterGames(gameTypes);
            _queueService.Initialize(_registry.RegisteredGames);
            
            // Act - Measure queue navigation performance
            var sw = Stopwatch.StartNew();
            const int navigationOps = 100;
            
            for (int i = 0; i < navigationOps; i++)
            {
                if (i % 3 == 0 && _queueService.HasNext)
                {
                    _queueService.MoveNext();
                }
                else if (i % 3 == 1 && _queueService.HasPrevious)
                {
                    _queueService.MovePrevious();
                }
                else
                {
                    _queueService.MoveToIndex(i % gameTypes.Count);
                }
            }
            
            sw.Stop();
            
            // Report
            var avgTime = sw.ElapsedMilliseconds / (float)navigationOps;
            Debug.Log($"Queue navigation avg time: {avgTime:F2}ms");
            
            // Assert
            Assert.Less(avgTime, 1.0f, "Queue navigation should be very fast");
        }
        
        [Test]
        public async Task StartPreloadedGame_Performance()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            _registry.RegisterGames(gameTypes);
            _queueService.Initialize(_registry.RegisteredGames);
            
            // Preload all games
            await _loader.PreloadGamesAsync(gameTypes);
            
            // Act - Measure starting preloaded games
            var sw = Stopwatch.StartNew();
            const int iterations = 30;
            
            for (int i = 0; i < iterations; i++)
            {
                var gameType = gameTypes[i % gameTypes.Count];
                _loader.StartPreloadedGame(gameType);
                // Re-preload for next iteration
                await _loader.PreloadGameAsync(gameType);
            }
            
            sw.Stop();
            
            // Report
            var avgTime = sw.ElapsedMilliseconds / (float)iterations;
            Debug.Log($"Starting preloaded game avg time: {avgTime:F2}ms");
            
            // Assert
            Assert.Less(avgTime, 50.0f, "Starting preloaded games should be fast");
        }
        
        [Test]
        public void LargeRegistry_Performance()
        {
            // Test registry performance with different sizes
            var registrySizes = new[] { 10, 50, 100, 500 };
            var results = new List<string>();
            
            foreach (var size in registrySizes)
            {
                var testRegistry = new GameRegistry(_logger);
                var gameTypes = new List<Type>();
                
                // Fill registry
                for (int i = 0; i < size; i++)
                {
                    // Alternate between different game types
                    var gameType = i % 3 == 0 ? typeof(MockShortGame) :
                                  i % 3 == 1 ? typeof(MockPoolableShortGame) :
                                              typeof(MockShortGame2D);
                    gameTypes.Add(gameType);
                }
                
                // Measure registration time
                var sw = Stopwatch.StartNew();
                testRegistry.RegisterGames(gameTypes);
                sw.Stop();
                
                var avgTime = sw.ElapsedMilliseconds / (float)size;
                results.Add($"Registry size {size}: {avgTime:F3}ms per registration");
            }
            
            // Report
            Debug.Log("Registry Performance at Different Scales:");
            foreach (var result in results)
            {
                Debug.Log($"  {result}");
            }
        }
        
        [Test]
        public async Task ResourceCleanup_Performance()
        {
            // Arrange - Create and preload many games
            var gameTypes = new List<Type>();
            for (int i = 0; i < 20; i++)
            {
                gameTypes.Add(typeof(MockPoolableShortGame));
            }
            _registry.RegisterGames(gameTypes);
            _queueService.Initialize(_registry.RegisteredGames);
            await _loader.PreloadGamesAsync(gameTypes);
            
            // Act - Measure cleanup time
            var sw = Stopwatch.StartNew();
            _loader.UnloadAllGames();
            sw.Stop();
            
            // Report
            Debug.Log($"Cleanup time for {gameTypes.Count} preloaded games: {sw.ElapsedMilliseconds}ms");
            
            // Assert
            Assert.Less(sw.ElapsedMilliseconds, 1000, "Cleanup should be reasonably fast");
        }
        
        [Test]
        public async Task ThreeGamesSimultaneous_Performance()
        {
            // Test the main use case: 3 games loaded simultaneously (previous, current, next)
            
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),       // Previous
                typeof(MockPoolableShortGame), // Current  
                typeof(MockShortGame2D)       // Next
            };
            _registry.RegisterGames(gameTypes);
            _queueService.Initialize(_registry.RegisteredGames);
            _queueService.MoveToIndex(1); // Set current to middle game
            
            // Act - Measure loading 3 games simultaneously
            var sw = Stopwatch.StartNew();
            var gamesToPreload = _queueService.GetGamesToPreload();
            await _loader.PreloadGamesAsync(gamesToPreload);
            sw.Stop();
            
            // Report
            Debug.Log($"Loading 3 simultaneous games took: {sw.ElapsedMilliseconds}ms");
            
            // Measure switching between them
            sw.Restart();
            const int switches = 30;
            
            for (int i = 0; i < switches; i++)
            {
                if (i % 2 == 0 && _queueService.HasNext)
                {
                    await _loader.LoadNextGameAsync();
                }
                else if (_queueService.HasPrevious)
                {
                    await _loader.LoadPreviousGameAsync();
                }
            }
            
            sw.Stop();
            
            var avgSwitchTime = sw.ElapsedMilliseconds / (float)switches;
            Debug.Log($"Switching between 3 preloaded games avg: {avgSwitchTime:F2}ms");
            
            // Assert
            Assert.Less(avgSwitchTime, 30.0f, "Switching between preloaded games should be very fast");
        }
        
        /// <summary>
        /// Generates a performance report for the new architecture
        /// </summary>
        [Test]
        public void GeneratePerformanceReport()
        {
            Debug.Log("\n=== PERFORMANCE REPORT ===");
            Debug.Log("ShortGames Core System Performance Metrics (New Architecture)\n");
            
            var metrics = new Dictionary<string, string>
            {
                ["Target FPS"] = "60 FPS",
                ["Max frame time"] = "16.67ms",
                ["Registry operation"] = "< 0.1ms",
                ["Queue navigation"] = "< 1ms",
                ["Preloaded game start"] = "< 5ms",
                ["Game switch (preloaded)"] = "< 30ms",
                ["Game preload"] = "< 200ms per game",
                ["3 games simultaneous"] = "< 600ms total",
                ["Memory per game"] = "< 1MB"
            };
            
            Debug.Log("Performance Targets:");
            foreach (var metric in metrics)
            {
                Debug.Log($"  {metric.Key}: {metric.Value}");
            }
            
            Debug.Log("\nArchitectural Improvements:");
            Debug.Log("  • Separation of concerns (Registry, Queue, Loader, Provider)");
            Debug.Log("  • Support for 3 simultaneous games (previous, current, next)");
            Debug.Log("  • Clean bridge pattern with IGameProvider");
            Debug.Log("  • Explicit game registration (no auto-discovery overhead)");
            
            Debug.Log("\nRecommendations:");
            Debug.Log("  • Always preload next and previous games");
            Debug.Log("  • Use render textures for smooth transitions");
            Debug.Log("  • Register only needed games to minimize memory");
            Debug.Log("  • Use pause instead of stop for quick resume");
            Debug.Log("\n=== END REPORT ===");
        }
    }
}