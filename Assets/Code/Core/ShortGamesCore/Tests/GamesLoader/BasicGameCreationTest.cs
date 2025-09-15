using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.GamesLoader;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;
using UnityEngine;

namespace Code.Core.ShortGamesCore.Tests.GamesLoader
{
    /// <summary>
    /// Basic tests to verify game creation and loading works
    /// </summary>
    [TestFixture]
    public class BasicGameCreationTest
    {
        [Test]
        public void CanCreateMockShortGameComponent()
        {
            // Test basic component creation
            var go = new GameObject("TestGame");
            var component = go.AddComponent<MockShortGame>();
            
            Assert.IsNotNull(component, "Component should be created");
            Assert.IsNotNull(component.gameObject, "Component should have GameObject");
            Assert.IsTrue(component.gameObject.activeSelf, "GameObject should be active");
            
            // Cleanup
            GameObject.DestroyImmediate(go);
        }
        
        [Test]
        public async Task BasicFactoryCreation_Works()
        {
            // Arrange
            var logger = new MockLogger();
            var parent = new GameObject("Parent");
            
            // Create prefab
            var prefab = new GameObject("Prefab");
            var prefabComponent = prefab.AddComponent<MockShortGame>();
            Assert.IsNotNull(prefabComponent, "Prefab component should exist");
            
            // Setup loader
            var loader = new MockResourceLoader();
            loader.AddResource("MockGame", prefab);
            
            var mapping = new Dictionary<Type, string>
            {
                { typeof(MockShortGame), "MockGame" }
            };
            
            var factory = new AddressableShortGameFactory(parent.transform, mapping, loader, logger);
            
            // Act
            var game = await factory.CreateShortGameAsync<MockShortGame>(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(game, "Factory should create game");
            Assert.IsNotNull(game.gameObject, "Game should have GameObject");
            Assert.IsTrue(game.gameObject.activeSelf, "Game GameObject should be active");
            Assert.AreNotEqual(prefab, game.gameObject, "Should be instance, not prefab");
            
            // Cleanup
            GameObject.DestroyImmediate(parent); // Destroy parent first (will destroy child instances)
            GameObject.DestroyImmediate(prefab); // Then destroy prefab
        }
        
        [Test]
        public async Task QueueServiceInitialization_Works()
        {
            // Arrange
            var logger = new MockLogger();
            var registry = new GameRegistry(logger);
            registry.RegisterGames(new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            });
            
            var queueService = new GameQueueService(logger);
            
            // Act
            queueService.Initialize(registry.RegisteredGames);
            
            // Assert
            Assert.AreEqual(3, queueService.TotalGamesCount, "Should have 3 games");
            Assert.AreEqual(-1, queueService.CurrentIndex, "Initial index should be -1");
            Assert.IsNull(queueService.CurrentGameType, "No current game initially");
            
            // Move to first
            var moved = queueService.MoveNext();
            Assert.IsTrue(moved, "Should move to first game");
            Assert.AreEqual(0, queueService.CurrentIndex, "Should be at index 0");
            Assert.AreEqual(typeof(MockShortGame), queueService.CurrentGameType, "Should be MockShortGame");
        }
        
        [Test]
        public async Task BasicLoaderTest_CanLoadGame()
        {
            // Arrange
            var logger = new MockLogger();
            var parent = new GameObject("Parent");
            
            try
            {
                // Create prefab
                var prefab = new GameObject("Prefab");
                prefab.AddComponent<MockShortGame>();
                
                // Setup loader
                var resourceLoader = new MockResourceLoader();
                resourceLoader.AddResource("MockGame", prefab);
                
                var mapping = new Dictionary<Type, string>
                {
                    { typeof(MockShortGame), "MockGame" }
                };
                
                var factory = new AddressableShortGameFactory(parent.transform, mapping, resourceLoader, logger);
                
                var queueService = new GameQueueService(logger);
                queueService.Initialize(new[] { typeof(MockShortGame) });
                
                var loader = new QueueShortGamesLoader(factory, queueService, logger);
                
                // Act
                var game = await loader.LoadGameAsync(typeof(MockShortGame));
                
                // Assert
                Assert.IsNotNull(game, "Should load game");
                Assert.IsInstanceOf<MockShortGame>(game, "Should be MockShortGame");
                
                var gameObject = (game as Component)?.gameObject;
                Assert.IsNotNull(gameObject, "Should have GameObject");
                Assert.IsTrue(gameObject.activeSelf, "GameObject should be active");
                
                // Check retrieval
                var retrieved = loader.GetGame(typeof(MockShortGame));
                Assert.AreEqual(game, retrieved, "Should retrieve same game");
                
                // Cleanup
                loader.Dispose();
                GameObject.DestroyImmediate(prefab);
            }
            finally
            {
                GameObject.DestroyImmediate(parent);
            }
        }
    }
}
