using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.ShortGamesCore.Source.Factory;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;
using UnityEngine;

namespace Code.Core.ShotGamesCore.Tests.Factory
{
    [TestFixture]
    public class AddressableShortGameFactoryTests
    {
        private AddressableShortGameFactory _factory;
        private MockResourceLoader _resourceLoader;
        private MockLogger _logger;
        private Transform _parent;
        private Dictionary<Type, string> _resourcesInfo;
        private GameObject _parentObject;
        private GameObject _testPrefab;
        
        [SetUp]
        public void SetUp()
        {
            _resourceLoader = new MockResourceLoader();
            _logger = new MockLogger();
            _parentObject = new GameObject("Parent");
            _parent = _parentObject.transform;
            
            _resourcesInfo = new Dictionary<Type, string>
            {
                { typeof(MockShortGame), "MockShortGame_Resource" },
                { typeof(MockPoolableShortGame), "MockPoolableShortGame_Resource" }
            };
            
            _factory = new AddressableShortGameFactory(_parent, _resourcesInfo, _resourceLoader, _logger);
            
            // Create test prefab
            _testPrefab = new GameObject("TestPrefab");
        }
        
        [TearDown]
        public void TearDown()
        {
            // Dispose factory only if not already disposed
            if (_factory != null)
            {
                try
                {
                    _factory.Dispose();
                    _factory = null; // Prevent double dispose
                }
                catch
                {
                    // Ignore errors during cleanup
                }
            }
            
            if (_parentObject != null)
            {
                GameObject.DestroyImmediate(_parentObject);
            }
            if (_testPrefab != null)
            {
                GameObject.DestroyImmediate(_testPrefab);
            }
        }
        
        [Test]
        public async Task CreateShortGameAsync_Generic_CreatesInstance()
        {
            // Arrange
            _testPrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", _testPrefab);
            
            // Act
            var result = await _factory.CreateShortGameAsync<MockShortGame>(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<MockShortGame>(result);
            Assert.AreEqual(_parent, result.transform.parent);
            Assert.AreEqual(1, _resourceLoader.LoadCallCount);
            
            // Clean up
            GameObject.DestroyImmediate(result.gameObject);
        }
        
        [Test]
        public async Task CreateShortGameAsync_ByType_CreatesInstance()
        {
            // Arrange
            _testPrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", _testPrefab);
            
            // Act
            var result = await _factory.CreateShortGameAsync(typeof(MockShortGame), CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.IsInstanceOf<MockShortGame>(result);
            Assert.AreEqual(1, _resourceLoader.LoadCallCount);
            
            // Clean up
            GameObject.DestroyImmediate((result as Component).gameObject);
        }
        
        [Test]
        public async Task CreateShortGameAsync_UsesPreloadedPrefab()
        {
            // Arrange
            _testPrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", _testPrefab);
            
            // Preload first
            await _factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);
            Assert.AreEqual(1, _resourceLoader.LoadCallCount);
            
            // Act - Create using preloaded
            var result = await _factory.CreateShortGameAsync<MockShortGame>(CancellationToken.None);
            
            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(1, _resourceLoader.LoadCallCount); // Should not load again
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Using preloaded prefab"));
            
            // Clean up
            GameObject.DestroyImmediate(result.gameObject);
        }
        
        [Test]
        public async Task CreateShortGameAsync_NoResourceInfo_LogsError()
        {
            // Arrange
            var emptyResourcesInfo = new Dictionary<Type, string>();
            var factory = new AddressableShortGameFactory(_parent, emptyResourcesInfo, _resourceLoader, _logger);
            
            // Act
            var result = await factory.CreateShortGameAsync<MockShortGame>(CancellationToken.None);
            
            // Assert
            Assert.IsNull(result);
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("Can't find"));
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("resource id"));
            
            factory.Dispose();
        }
        
        [Test]
        public async Task CreateShortGameAsync_PrefabMissingComponent_LogsError()
        {
            // Arrange - Prefab without the required component
            var badPrefab = new GameObject("BadPrefab");
            _resourceLoader.AddResource("MockShortGame_Resource", badPrefab);
            
            // Act
            var result = await _factory.CreateShortGameAsync<MockShortGame>(CancellationToken.None);
            
            // Assert
            Assert.IsNull(result);
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("doesn't have component"));
            
            // Clean up
            GameObject.DestroyImmediate(badPrefab);
        }
        
        [Test]
        public async Task PreloadGameResourcesAsync_LoadsAndCachesResource()
        {
            // Arrange
            _testPrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", _testPrefab);
            
            // Act
            await _factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);
            
            // Assert
            Assert.AreEqual(1, _resourceLoader.LoadCallCount);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Preloaded prefab"));
        }
        
        [Test]
        public async Task PreloadGameResourcesAsync_AlreadyPreloaded_IncreasesRefCount()
        {
            // Arrange
            _testPrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", _testPrefab);
            
            // Act - Preload twice
            await _factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);
            await _factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);
            
            // Assert
            Assert.AreEqual(1, _resourceLoader.LoadCallCount); // Should only load once
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("already preloaded"));
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("ref count: 2"));
        }
        
        [Test]
        public async Task PreloadGameResourcesAsync_ByType_Works()
        {
            // Arrange
            _testPrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", _testPrefab);
            
            // Act
            await _factory.PreloadGameResourcesAsync(typeof(MockShortGame), CancellationToken.None);
            
            // Assert
            Assert.AreEqual(1, _resourceLoader.LoadCallCount);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Preloaded prefab"));
        }
        
        [Test]
        public async Task UnloadGameResources_RemovesFromCache()
        {
            // Arrange
            _testPrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", _testPrefab);
            await _factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);
            
            // Act
            _factory.UnloadGameResources<MockShortGame>();
            
            // Assert
            Assert.AreEqual(1, _resourceLoader.ReleaseCallCount);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Unloaded resources"));
        }
        
        [Test]
        public async Task UnloadGameResources_WithRefCount_DecreasesCount()
        {
            // Arrange
            _testPrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", _testPrefab);
            
            // Preload twice
            await _factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);
            await _factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);
            
            // Act - Unload once
            _factory.UnloadGameResources<MockShortGame>();
            
            // Assert
            Assert.AreEqual(0, _resourceLoader.ReleaseCallCount); // Should not release yet
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Decreased ref count"));
            
            // Act - Unload again
            _factory.UnloadGameResources<MockShortGame>();
            
            // Assert
            Assert.AreEqual(1, _resourceLoader.ReleaseCallCount); // Now should release
        }
        
        [Test]
        public void UnloadGameResources_NotPreloaded_LogsWarning()
        {
            // Act
            _factory.UnloadGameResources<MockShortGame>();
            
            // Assert
            Assert.That(_logger.LoggedWarnings, Has.Some.Contains("No preloaded resources"));
        }
        
        [Test]
        public async Task Dispose_UnloadsAllResources()
        {
            // Arrange
            _testPrefab.AddComponent<MockShortGame>();
            _resourceLoader.AddResource("MockShortGame_Resource", _testPrefab);
            
            var prefab2 = new GameObject("Prefab2");
            prefab2.AddComponent<MockPoolableShortGame>();
            _resourceLoader.AddResource("MockPoolableShortGame_Resource", prefab2);
            
            await _factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);
            await _factory.PreloadGameResourcesAsync<MockPoolableShortGame>(CancellationToken.None);
            
            // Act
            _factory.Dispose();
            _factory = null; // Prevent double dispose in TearDown
            
            // Assert
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Disposing AddressableShortGameFactory - START"));
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Disposing AddressableShortGameFactory - COMPLETED"));
            Assert.AreEqual(2, _resourceLoader.ReleaseCallCount);
            
            // Clean up
            GameObject.DestroyImmediate(prefab2);
        }
        
        [Test]
        public async Task CreateShortGameAsync_InvalidType_LogsError()
        {
            // Act
            var result = await _factory.CreateShortGameAsync(typeof(string), CancellationToken.None);
            
            // Assert
            Assert.IsNull(result);
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("must be Component and implement IShortGame"));
        }
        
        [Test]
        public async Task CreateShortGameAsync_LoadFails_ReturnsNull()
        {
            // Arrange
            _resourceLoader.ShouldThrowOnLoad = true;
            
            // Act
            var result = await _factory.CreateShortGameAsync<MockShortGame>(CancellationToken.None);
            
            // Assert
            Assert.IsNull(result);
        }
        
        [Test]
        public async Task PreloadGameResourcesAsync_LoadFails_ThrowsException()
        {
            // Arrange
            _resourceLoader.ShouldThrowOnLoad = true;
            
            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () =>
            {
                await _factory.PreloadGameResourcesAsync<MockShortGame>(CancellationToken.None);
            });
            
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("Failed to preload"));
        }
        
        [Test]
        public async Task CreateShortGameAsync_Cancellation_ThrowsException()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            cts.Cancel();
            
            // Act & Assert
            // TaskCanceledException inherits from OperationCanceledException
            // But NUnit's ThrowsAsync checks exact type, so we use Catch instead
            var ex = Assert.CatchAsync<Exception>(async () =>
            {
                await _factory.CreateShortGameAsync<MockShortGame>(cts.Token);
            });
            
            // Verify it's a cancellation exception (TaskCanceledException or OperationCanceledException)
            Assert.IsTrue(ex is OperationCanceledException, 
                $"Expected OperationCanceledException or derived, but got {ex?.GetType().Name}");
        }
    }
}
