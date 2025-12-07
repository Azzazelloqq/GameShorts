using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Core.GamesLoader;
using Code.Core.GamesLoader.TestHelpers;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Tests.GamesLoader
{
    /// <summary>
    /// Simple isolated tests for GameProvider functionality
    /// </summary>
    [TestFixture]
    public class GameProviderSimpleTest
    {
        [Test]
        public async Task GameProvider_BasicSetup_Works()
        {
            // Arrange
            var logger = new MockLogger();
            var parent = new GameObject("TestParent");
            
            try
            {
                // Create registry
                var registry = new GameRegistry(logger);
                registry.RegisterGame(typeof(MockShortGame));
                
                // Create queue service
                var queueService = new GameQueueService(logger);
                queueService.Initialize(registry.RegisteredGames);
                
                // Create prefabs
                var prefab = new GameObject("MockPrefab");
                prefab.AddComponent<MockShortGame>();
                
                // Create resource loader
                var resourceLoader = new MockResourceLoader();
                resourceLoader.AddResource("MockGame", prefab);
                
                // Create factory
                var resourceMapping = new Dictionary<Type, string>
                {
                    { typeof(MockShortGame), "MockGame" }
                };
                var factory = new AddressableShortGameFactory(parent.transform, resourceMapping, resourceLoader, logger);
                
                var settings = new ShortGameLoaderSettings();

                // Create loader
                var loader = new QueueShortGamesLoader(factory, queueService, logger, settings);
                
                // Create provider
                var provider = new TestableShortGameServiceProvider(logger, registry, queueService, loader);
                
                // Act
                await provider.InitializeAsync();
                
                // Assert
                Assert.IsNotNull(provider.TestGameRegistry, "Registry should be set");
                Assert.IsNotNull(provider.TestQueueService, "QueueService should be set");
                Assert.IsNotNull(provider.TestGamesLoader, "Loader should be set");
                
                // Check queue was initialized
                Assert.AreEqual(1, provider.TestQueueService.TotalGamesCount, "Should have 1 game in queue");
                
                // Move to first game
                provider.TestQueueService.MoveNext();
                Assert.AreEqual(typeof(MockShortGame), provider.TestQueueService.CurrentGameType, "Should be at MockShortGame");
                
                // Try to load the game
                var game = await loader.LoadGameAsync(typeof(MockShortGame));
                Assert.IsNotNull(game, "Should load game");
                
                // Check provider can access it
                var currentGame = provider.CurrentGame;
                Assert.IsNotNull(currentGame, "Provider should return current game");
                Assert.AreEqual(game, currentGame, "Should be same game");
                
                // Cleanup
                provider.Dispose();
                GameObject.DestroyImmediate(prefab);
            }
            finally
            {
                GameObject.DestroyImmediate(parent);
            }
        }
        
        [Test]
        public async Task GameProvider_UpdatePreloadedGames_Works()
        {
            // Arrange
            var logger = new MockLogger();
            var parent = new GameObject("TestParent");
            var prefabs = new List<GameObject>();
            
            try
            {
                // Create registry with 3 games
                var registry = new GameRegistry(logger);
                registry.RegisterGames(new[]
                {
                    typeof(MockShortGame),
                    typeof(MockPoolableShortGame),
                    typeof(MockShortGame2D)
                });
                
                // Create queue service
                var queueService = new GameQueueService(logger);
                queueService.Initialize(registry.RegisteredGames);
                
                // Create prefabs
                var prefab1 = new GameObject("MockPrefab1");
                prefab1.AddComponent<MockShortGame>();
                prefabs.Add(prefab1);
                
                var prefab2 = new GameObject("MockPrefab2");
                prefab2.AddComponent<MockPoolableShortGame>();
                prefabs.Add(prefab2);
                
                var prefab3 = new GameObject("MockPrefab3");
                prefab3.AddComponent<MockShortGame2D>();
                prefabs.Add(prefab3);
                
                // Create resource loader
                var resourceLoader = new MockResourceLoader();
                resourceLoader.AddResource("MockGame", prefab1);
                resourceLoader.AddResource("MockPoolableGame", prefab2);
                resourceLoader.AddResource("MockGame2D", prefab3);
                
                // Create factory
                var resourceMapping = new Dictionary<Type, string>
                {
                    { typeof(MockShortGame), "MockGame" },
                    { typeof(MockPoolableShortGame), "MockPoolableGame" },
                    { typeof(MockShortGame2D), "MockGame2D" }
                };
                var factory = new AddressableShortGameFactory(parent.transform, resourceMapping, resourceLoader, logger);
                
                var settings = new ShortGameLoaderSettings();

                // Create loader
                var loader = new QueueShortGamesLoader(factory, queueService, logger, settings);
                
                // Create provider
                var provider = new TestableShortGameServiceProvider(logger, registry, queueService, loader);
                
                // Act
                await provider.InitializeAsync();
                
                // Move to middle position
                queueService.MoveToIndex(1); // MockPoolableShortGame
                
                // Update preloaded games
                await provider.UpdatePreloadedGamesAsync();
                
                // Assert
                var current = provider.CurrentGame;
                var next = provider.NextGame;
                var previous = provider.PreviousGame;
                
                Assert.IsNotNull(current, "Should have current game");
                Assert.IsNotNull(next, "Should have next game");
                Assert.IsNotNull(previous, "Should have previous game");
                
                Assert.IsInstanceOf<MockPoolableShortGame>(current, "Current should be MockPoolableShortGame");
                Assert.IsInstanceOf<MockShortGame2D>(next, "Next should be MockShortGame2D");
                Assert.IsInstanceOf<MockShortGame>(previous, "Previous should be MockShortGame");
                
                // Check render textures
                var currentTexture = provider.CurrentGameRenderTexture;
                var nextTexture = provider.NextGameRenderTexture;
                var previousTexture = provider.PreviousGameRenderTexture;
                
                Assert.IsNotNull(currentTexture, "Should have current render texture");
                Assert.IsNotNull(nextTexture, "Should have next render texture");
                Assert.IsNotNull(previousTexture, "Should have previous render texture");
                
                // Cleanup
                provider.Dispose();
                foreach (var p in prefabs)
                {
                    if (p != null) GameObject.DestroyImmediate(p);
                }
            }
            finally
            {
                GameObject.DestroyImmediate(parent);
            }
        }
    }
}
