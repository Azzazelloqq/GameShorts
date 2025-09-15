using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.LifeCycleService;
using Code.Core.ShortGamesCore.Source.Pool;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Code.Core.ShotGamesCore.Tests.Performance
{
    /// <summary>
    /// Тесты производительности системы ShortGames
    /// </summary>
    [TestFixture]
    public class PerformanceTests
    {
        private SimpleShortGameLifeCycleService _lifeCycleService;
        private SimpleShortGamePool _pool;
        private MockShortGameFactory _factory;
        private MockLogger _logger;
        private GameObject _parentObject;
        
        [SetUp]
        public void SetUp()
        {
            _logger = new MockLogger();
            _pool = new SimpleShortGamePool(_logger, maxInstancesPerType: 10);
            _factory = new MockShortGameFactory();
            _lifeCycleService = new SimpleShortGameLifeCycleService(_pool, _factory, _logger);
            _parentObject = new GameObject("Parent");
        }
        
        [TearDown]
        public void TearDown()
        {
            _lifeCycleService?.Dispose();
            if (_parentObject != null)
            {
                GameObject.DestroyImmediate(_parentObject);
            }
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
        public async Task GameLoading_FromPool_Performance()
        {
            // Arrange - Preload games
            var gameTypes = new List<Type>();
            for (int i = 0; i < 5; i++)
            {
                gameTypes.Add(typeof(MockPoolableShortGame));
            }
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            
            // Act - Measure loading from pool
            var sw = Stopwatch.StartNew();
            const int iterations = 20;
            
            for (int i = 0; i < iterations; i++)
            {
                await _lifeCycleService.LoadNextGameAsync();
            }
            
            sw.Stop();
            
            // Assert
            var avgTime = sw.ElapsedMilliseconds / (float)iterations;
            Debug.Log($"Game loading from pool avg time: {avgTime:F2}ms");
            Assert.Less(avgTime, 10.0f, "Loading from pool should be fast");
        }
        
        [Test]
        public async Task GameSwitching_Performance()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            
            // Act - Measure rapid switching
            var sw = Stopwatch.StartNew();
            const int switches = 50;
            
            for (int i = 0; i < switches; i++)
            {
                await _lifeCycleService.LoadNextGameAsync();
            }
            
            sw.Stop();
            
            // Assert
            var avgTime = sw.ElapsedMilliseconds / (float)switches;
            Debug.Log($"Game switching avg time: {avgTime:F2}ms");
            Assert.Less(avgTime, 50.0f, "Game switching should be reasonably fast");
        }
        
        [Test]
        public async Task Preloading_Performance()
        {
            // Arrange
            var gameTypes = new List<Type>();
            for (int i = 0; i < 10; i++)
            {
                gameTypes.Add(i % 2 == 0 ? typeof(MockShortGame) : typeof(MockPoolableShortGame));
            }
            
            // Act - Measure preloading time
            var sw = Stopwatch.StartNew();
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            sw.Stop();
            
            // Assert
            var avgTime = sw.ElapsedMilliseconds / (float)gameTypes.Count;
            Debug.Log($"Preloading avg time per game: {avgTime:F2}ms");
            Debug.Log($"Total preload time for {gameTypes.Count} games: {sw.ElapsedMilliseconds}ms");
            Assert.Less(avgTime, 200.0f, "Preloading should be reasonably fast per game");
        }
        
        [Test]
        public void MemoryUsage_PoolSize()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var games = new List<GameObject>();
            
            // Act - Create many games and add to pool
            for (int i = 0; i < 100; i++)
            {
                var go = new GameObject($"Game_{i}");
                var game = go.AddComponent<MockPoolableShortGame>();
                games.Add(go);
                _pool.WarmUpPool(game);
            }
            
            var afterPoolMemory = GC.GetTotalMemory(false);
            var memoryUsed = (afterPoolMemory - initialMemory) / 1024f / 1024f;
            
            // Report
            Debug.Log($"Memory used by 100 pooled games: {memoryUsed:F2} MB");
            Debug.Log($"Average memory per game: {memoryUsed / 100f * 1024f:F2} KB");
            
            // Cleanup
            foreach (var go in games)
            {
                GameObject.DestroyImmediate(go);
            }
            
            // Assert - reasonable memory usage
            Assert.Less(memoryUsed, 10f, "Pool should not use excessive memory");
        }
        
        [Test]
        public async Task ConcurrentOperations_Performance()
        {
            // Arrange
            await _lifeCycleService.PreloadGameAsync<MockPoolableShortGame>();
            
            // Act - Simulate rapid sequential operations (Unity doesn't support true multi-threading for GameObjects)
            // We test performance of quick successive operations instead of true concurrent operations
            var sw = Stopwatch.StartNew();
            
            // Perform 50 rapid load/stop operations
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    await _lifeCycleService.LoadGameAsync<MockPoolableShortGame>();
                    _lifeCycleService.StopCurrentGame();
                }
            }
            
            sw.Stop();
            
            // Report
            Debug.Log($"Rapid sequential operations (50 total) completed in: {sw.ElapsedMilliseconds}ms");
            var avgTime = sw.ElapsedMilliseconds / 50f;
            Debug.Log($"Average time per operation: {avgTime:F2}ms");
            
            // Assert
            Assert.Less(sw.ElapsedMilliseconds, 2000, "Sequential operations should complete quickly");
            Assert.Less(avgTime, 40, "Each operation should be fast when using pool");
        }
        
        [Test]
        public void LargeScalePooling_Performance()
        {
            // Test pool performance with different sizes
            var poolSizes = new[] { 10, 50, 100, 500 };
            var results = new List<string>();
            
            foreach (var size in poolSizes)
            {
                // Create pool with specific size
                var testPool = new SimpleShortGamePool(_logger, maxInstancesPerType: size);
                var games = new List<MockPoolableShortGame>();
                
                // Fill pool
                for (int i = 0; i < size; i++)
                {
                    var go = new GameObject($"Game_{i}");
                    var game = go.AddComponent<MockPoolableShortGame>();
                    games.Add(game);
                    testPool.WarmUpPool(game);
                }
                
                // Measure retrieval time
                var sw = Stopwatch.StartNew();
                for (int i = 0; i < size; i++)
                {
                    testPool.TryGetShortGame<MockPoolableShortGame>(out var game);
                }
                sw.Stop();
                
                var avgTime = sw.ElapsedMilliseconds / (float)size;
                results.Add($"Pool size {size}: {avgTime:F3}ms per retrieval");
                
                // Cleanup
                foreach (var game in games)
                {
                    GameObject.DestroyImmediate(game.gameObject);
                }
                testPool.Dispose();
            }
            
            // Report
            Debug.Log("Pool Performance at Different Scales:");
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
            await _lifeCycleService.PreloadGamesAsync(gameTypes);
            
            // Act - Measure cleanup time
            var sw = Stopwatch.StartNew();
            _lifeCycleService.ClearPreloadedGames();
            sw.Stop();
            
            // Report
            Debug.Log($"Cleanup time for {gameTypes.Count} preloaded games: {sw.ElapsedMilliseconds}ms");
            
            // Assert
            Assert.Less(sw.ElapsedMilliseconds, 1000, "Cleanup should be reasonably fast");
        }
        
        /// <summary>
        /// Генерирует отчёт о производительности
        /// </summary>
        [Test]
        public void GeneratePerformanceReport()
        {
            Debug.Log("\n=== PERFORMANCE REPORT ===");
            Debug.Log("ShortGames Core System Performance Metrics\n");
            
            var metrics = new Dictionary<string, string>
            {
                ["Target FPS"] = "60 FPS",
                ["Max frame time"] = "16.67ms",
                ["Pool operation target"] = "< 1ms",
                ["Game switch target"] = "< 50ms",
                ["Preload target per game"] = "< 200ms",
                ["Memory per pooled game"] = "< 100KB"
            };
            
            Debug.Log("Performance Targets:");
            foreach (var metric in metrics)
            {
                Debug.Log($"  {metric.Key}: {metric.Value}");
            }
            
            Debug.Log("\nRecommendations:");
            Debug.Log("  • Preload 3-5 games for optimal performance");
            Debug.Log("  • Use pooling for frequently switched games");
            Debug.Log("  • Limit pool size to 3-5 instances per type");
            Debug.Log("  • Clear unused games from pool periodically");
            Debug.Log("\n=== END REPORT ===");
        }
    }
}
