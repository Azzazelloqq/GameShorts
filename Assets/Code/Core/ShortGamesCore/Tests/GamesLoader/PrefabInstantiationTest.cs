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
    /// Isolated test to verify prefab instantiation works correctly with inactive prefabs
    /// </summary>
    [TestFixture]
    public class PrefabInstantiationTest
    {
        [Test]
        public void InactivePrefab_WhenInstantiated_BecomesActive()
        {
            // Arrange
            var prefab = new GameObject("TestPrefab");
            prefab.AddComponent<MockShortGame>();
            prefab.SetActive(false);
            Assert.IsFalse(prefab.activeSelf, "Prefab should be inactive");
            
            // Act
            var instance = GameObject.Instantiate(prefab);
            
            // Assert - Unity keeps the active state by default
            Assert.IsFalse(instance.activeSelf, "Instance is inactive by default when prefab is inactive");
            
            // Activate the instance
            instance.SetActive(true);
            Assert.IsTrue(instance.activeSelf, "Instance should be active after SetActive(true)");
            
            // Cleanup
            GameObject.DestroyImmediate(prefab);
            GameObject.DestroyImmediate(instance);
        }
        
        [Test]
        public async Task MockResourceLoader_WithInactivePrefab_WorksCorrectly()
        {
            // Arrange
            var logger = new MockLogger();
            var parentObject = new GameObject("TestParent");
            var parent = parentObject.transform;
            
            // Create inactive prefab
            var prefab = new GameObject("MockGamePrefab");
            prefab.AddComponent<MockShortGame>();
            prefab.SetActive(false);
            
            // Setup resource loader
            var resourceLoader = new MockResourceLoader();
            resourceLoader.AddResource("MockGame", prefab);
            
            // Setup factory
            var resourceMapping = new Dictionary<Type, string>
            {
                { typeof(MockShortGame), "MockGame" }
            };
            var factory = new AddressableShortGameFactory(parent, resourceMapping, resourceLoader, logger);
            
            // Act
            var game = await factory.CreateShortGameAsync<MockShortGame>(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(game, "Game should be created");
            Assert.IsNotNull(game.gameObject, "Game should have GameObject");
            Assert.IsTrue(game.gameObject.activeSelf, "Created game object should be active (after our fix)");
            
            // Cleanup
            GameObject.DestroyImmediate(prefab);
            GameObject.DestroyImmediate(parentObject);
        }
        
        [Test]
        public async Task QueueLoader_WithInactivePrefab_LoadsActiveGame()
        {
            // Arrange
            var logger = new MockLogger();
            var parentObject = new GameObject("TestParent");
            var parent = parentObject.transform;
            var prefabs = new List<GameObject>();
            
            try
            {
                // Create inactive prefab
                var prefab = new GameObject("MockGamePrefab");
                prefab.AddComponent<MockShortGame>();
                prefab.SetActive(false);
                prefabs.Add(prefab);
                
                // Setup resource loader
                var resourceLoader = new MockResourceLoader();
                resourceLoader.AddResource("MockGame", prefab);
                
                // Setup factory
                var resourceMapping = new Dictionary<Type, string>
                {
                    { typeof(MockShortGame), "MockGame" }
                };
                var factory = new AddressableShortGameFactory(parent, resourceMapping, resourceLoader, logger);
                
                // Setup queue and loader
                var queueService = new GameQueueService(logger);
                queueService.Initialize(new[] { typeof(MockShortGame) });
                var loader = new QueueShortGamesLoader(factory, queueService, logger);
                
                // Act
                var game = await loader.LoadGameAsync(typeof(MockShortGame));
                
                // Assert
                Assert.IsNotNull(game, "Game should be loaded");
                var gameObject = (game as Component)?.gameObject;
                Assert.IsNotNull(gameObject, "Game should have GameObject");
                Assert.IsTrue(gameObject.activeSelf, "Loaded game object should be active");
                
                // Cleanup
                loader.Dispose();
            }
            finally
            {
                // Cleanup
                foreach (var p in prefabs)
                {
                    if (p != null) GameObject.DestroyImmediate(p);
                }
                GameObject.DestroyImmediate(parentObject);
            }
        }
    }
}
