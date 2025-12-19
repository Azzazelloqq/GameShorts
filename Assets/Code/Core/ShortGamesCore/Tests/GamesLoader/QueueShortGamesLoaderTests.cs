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
    [TestFixture]
    public class QueueShortGamesLoaderTests
    {
        private QueueShortGamesLoader _loader;
        private IShortGameFactory _mockFactory;
        private IGameQueueService _queueService;
        private MockLogger _logger;
        private Transform _parent;
        private GameObject _parentObject;
        private List<GameObject> _prefabs;
        
        [SetUp]
        public void SetUp()
        {
            _logger = new MockLogger();
            _parentObject = new GameObject("TestParent");
            _parent = _parentObject.transform;
            _prefabs = new List<GameObject>();
            
            // Create mock factory
            var resourceMapping = new Dictionary<Type, string>
            {
                { typeof(MockShortGame), "MockGame" },
                { typeof(MockPoolableShortGame), "MockPoolableGame" },
                { typeof(MockShortGame2D), "MockGame2D" }
            };
            
            var resourceLoader = new MockResourceLoader();
            SetupMockResources(resourceLoader);
            
            _mockFactory = new AddressableShortGameFactory(_parent, resourceMapping, resourceLoader, _logger);
            _queueService = new GameQueueService(_logger);
            var settings = new ShortGameLoaderSettings();
            _loader = new QueueShortGamesLoader(_mockFactory, _queueService, _logger, settings);
        }
        
        [TearDown]
        public void TearDown()
        {
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
        public async Task LoadGameAsync_ValidType_LoadsAndStartsGame()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            
            // Act
            var game = await _loader.LoadGameAsync(typeof(MockShortGame));
            
            // Assert
            Assert.IsNotNull(game);
            Assert.IsTrue(game.IsPreloaded);
            Assert.IsInstanceOf<MockShortGame>(game);
            var mockGame = game as MockShortGame;
            Assert.IsTrue(mockGame.IsStarted);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Successfully loaded and started game"));
        }
        
        [Test]
        public async Task PreloadGameAsync_ValidType_PreloadsWithoutStarting()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            
            // Act
            var game = await _loader.PreloadGameAsync(typeof(MockShortGame));
            
            // Assert
            Assert.IsNotNull(game);
            Assert.IsTrue(game.IsPreloaded);
            Assert.IsInstanceOf<MockShortGame>(game);
            var mockGame = game as MockShortGame;
            Assert.IsFalse(mockGame.IsStarted);
            Assert.That(_loader.PreloadedGames, Has.Count.EqualTo(1));
        }
        
        [Test]
        public async Task PreloadGamesAsync_MultipleTypes_PreloadsAll()
        {
            // Arrange
            var gameTypes = new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            _queueService.Initialize(gameTypes);
            
            // Act
            var preloaded = await _loader.PreloadGamesAsync(gameTypes);
            
            // Assert
            Assert.AreEqual(3, preloaded.Count);
            Assert.That(preloaded.Keys, Has.Member(typeof(MockShortGame)));
            Assert.That(preloaded.Keys, Has.Member(typeof(MockPoolableShortGame)));
            Assert.That(preloaded.Keys, Has.Member(typeof(MockShortGame2D)));
        }
        
        [Test]
        public async Task StartPreloadedGame_PreloadedGame_StartsSuccessfully()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            var game = await _loader.PreloadGameAsync(typeof(MockShortGame));
            
            // Act
            var result = _loader.StartPreloadedGame(typeof(MockShortGame));
            
            // Assert
            Assert.IsTrue(result);
            Assert.That(_loader.LoadedGames, Has.Count.EqualTo(1));
            Assert.That(_loader.PreloadedGames, Has.Count.EqualTo(0));
            var mockGame = game as MockShortGame;
            Assert.IsTrue(mockGame.IsStarted);
        }
        
        [Test]
        public void StartPreloadedGame_NotPreloaded_ReturnsFalse()
        {
            // Act
            var result = _loader.StartPreloadedGame(typeof(MockShortGame));
            
            // Assert
            Assert.IsFalse(result);
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("is not preloaded"));
        }
        
        [Test]
        public async Task LoadNextGameAsync_WithQueue_LoadsNextInQueue()
        {
            // Arrange
            var gameTypes = new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext(); // Move to first game
            
            // Act
            var game = await _loader.LoadNextGameAsync();
            
            // Assert
            Assert.IsNotNull(game);
            Assert.IsInstanceOf<MockPoolableShortGame>(game);
            Assert.AreEqual(1, _queueService.CurrentIndex);
        }
        
        [Test]
        public async Task LoadPreviousGameAsync_WithQueue_LoadsPreviousInQueue()
        {
            // Arrange
            var gameTypes = new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext(); // Move to index 0
            _queueService.MoveNext(); // Move to index 1
            
            // Act
            var game = await _loader.LoadPreviousGameAsync();
            
            // Assert
            Assert.IsNotNull(game);
            Assert.IsInstanceOf<MockShortGame>(game);
            Assert.AreEqual(0, _queueService.CurrentIndex);
        }
        
        [Test]
        public async Task LoadGameByIndexAsync_ValidIndex_LoadsCorrectGame()
        {
            // Arrange
            var gameTypes = new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            _queueService.Initialize(gameTypes);
            
            // Act
            var game = await _loader.LoadGameByIndexAsync(2);
            
            // Assert
            Assert.IsNotNull(game);
            Assert.IsInstanceOf<MockShortGame2D>(game);
            Assert.AreEqual(2, _queueService.CurrentIndex);
        }
        
        [Test]
        public async Task UnloadGame_LoadedGame_RemovesFromLoaded()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            var game = await _loader.LoadGameAsync(typeof(MockShortGame));
            
            // Act
            _loader.UnloadGame(typeof(MockShortGame));
            
            // Assert
            Assert.That(_loader.LoadedGames, Has.Count.EqualTo(0));
            Assert.IsFalse(_loader.IsGameLoaded(typeof(MockShortGame)));
        }
        
        [Test]
        public async Task UnloadGame_PreloadedGame_RemovesFromPreloaded()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            await _loader.PreloadGameAsync(typeof(MockShortGame));
            
            // Act
            _loader.UnloadGame(typeof(MockShortGame));
            
            // Assert
            Assert.That(_loader.PreloadedGames, Has.Count.EqualTo(0));
            Assert.IsFalse(_loader.IsGameLoaded(typeof(MockShortGame)));
        }
        
        [Test]
        public async Task UnloadAllGames_RemovesAllGames()
        {
            // Arrange
            var gameTypes = new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            _queueService.Initialize(gameTypes);
            await _loader.LoadGameAsync(typeof(MockShortGame));
            await _loader.PreloadGameAsync(typeof(MockPoolableShortGame));
            
            // Act
            _loader.UnloadAllGames();
            
            // Assert
            Assert.That(_loader.LoadedGames, Has.Count.EqualTo(0));
            Assert.That(_loader.PreloadedGames, Has.Count.EqualTo(0));
        }
        
        [Test]
        public async Task GetGame_LoadedGame_ReturnsGame()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            var loadedGame = await _loader.LoadGameAsync(typeof(MockShortGame));
            
            // Act
            var game = _loader.GetGame(typeof(MockShortGame));
            
            // Assert
            Assert.AreEqual(loadedGame, game);
        }
        
        [Test]
        public async Task GetGame_PreloadedGame_ReturnsGame()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            var preloadedGame = await _loader.PreloadGameAsync(typeof(MockShortGame));
            
            // Act
            var game = _loader.GetGame(typeof(MockShortGame));
            
            // Assert
            Assert.AreEqual(preloadedGame, game);
        }
        
        [Test]
        public async Task IsGameLoaded_LoadedGame_ReturnsTrue()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            await _loader.LoadGameAsync(typeof(MockShortGame));
            
            // Act & Assert
            Assert.IsTrue(_loader.IsGameLoaded(typeof(MockShortGame)));
        }
        
        [Test]
        public async Task IsGameLoaded_PreloadedGame_ReturnsTrue()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            await _loader.PreloadGameAsync(typeof(MockShortGame));
            
            // Act & Assert
            Assert.IsTrue(_loader.IsGameLoaded(typeof(MockShortGame)));
        }

        [Test]
        public async Task ActivateNextGameAsync_WhenNotPreloaded_LoadsViaFallback()
        {
            // Arrange
            var settings = new ShortGameLoaderSettings(
                readinessTimeout: TimeSpan.FromMilliseconds(50),
                preloadRadius: 0,
                maxLoadedGames: 2,
                fallbackLoadAttempts: 2);

            _loader.Dispose();
            _loader = new QueueShortGamesLoader(_mockFactory, _queueService, _logger, settings);

            var gameTypes = new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            _queueService.Initialize(gameTypes);

            Assert.IsTrue(await _loader.ActivateNextGameAsync(), "Should activate first game");

            // Act
            var activatedNext = await _loader.ActivateNextGameAsync();

            // Assert
            Assert.IsTrue(activatedNext, "Fallback should load next game");
            Assert.AreEqual(typeof(MockPoolableShortGame), _loader.ActiveGameType);
            Assert.IsTrue(_loader.IsGameLoaded(typeof(MockPoolableShortGame)));
        }

        [Test]
        public async Task ActivateNextGameAsync_RespectsMaxLoadedGames()
        {
            // Arrange
            var settings = new ShortGameLoaderSettings(
                readinessTimeout: TimeSpan.FromMilliseconds(50),
                preloadRadius: 0,
                maxLoadedGames: 1,
                fallbackLoadAttempts: 1);

            _loader.Dispose();
            _loader = new QueueShortGamesLoader(_mockFactory, _queueService, _logger, settings);

            var gameTypes = new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame)
            };
            _queueService.Initialize(gameTypes);

            Assert.IsTrue(await _loader.ActivateNextGameAsync(), "Should activate first game");

            // Act
            Assert.IsTrue(await _loader.ActivateNextGameAsync(), "Should activate next game");

            // Assert
            Assert.IsFalse(_loader.IsGameLoaded(typeof(MockShortGame)), "Previous game should be unloaded");
            Assert.IsTrue(_loader.IsGameLoaded(typeof(MockPoolableShortGame)), "Active game should stay loaded");
        }

        [Test]
        public async Task ActivateNextGameAsync_UnloadsGamesOutsideVisibleWindow()
        {
            // Arrange
            var gameTypes = new[]
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            _queueService.Initialize(gameTypes);

            Assert.IsTrue(await _loader.ActivateNextGameAsync(), "Should activate first game");
            await _loader.PreloadWindowAsync();
            StartNextGameIfAvailable();

            Assert.IsTrue(await _loader.ActivateNextGameAsync(), "Should activate second game");
            await _loader.PreloadWindowAsync();
            StartNextGameIfAvailable();

            Assert.IsTrue(_loader.IsGameLoaded(typeof(MockShortGame)), "First game should remain loaded");
            Assert.IsTrue(_loader.IsGameLoaded(typeof(MockPoolableShortGame)), "Second game should remain loaded");
            Assert.IsTrue(_loader.IsGameLoaded(typeof(MockShortGame2D)), "Third game should be loaded");

            // Act
            Assert.IsTrue(await _loader.ActivateNextGameAsync(), "Should activate last game");

            // Assert
            Assert.IsFalse(_loader.IsGameLoaded(typeof(MockShortGame)), "Game outside the window should be unloaded");
            Assert.IsTrue(_loader.IsGameLoaded(typeof(MockPoolableShortGame)), "Previous game should stay loaded");
            Assert.IsTrue(_loader.IsGameLoaded(typeof(MockShortGame2D)), "Current game should stay loaded");
        }
        
        [Test]
        public async Task Reset_ClearsQueueAndGames()
        {
            // Arrange
            var gameTypes = new[] { typeof(MockShortGame), typeof(MockPoolableShortGame) };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext();
            await _loader.LoadGameAsync(typeof(MockShortGame));
            
            // Act
            _loader.Reset();
            
            // Assert
            Assert.AreEqual(-1, _queueService.CurrentIndex);
            Assert.That(_loader.LoadedGames, Has.Count.EqualTo(0));
            Assert.That(_loader.PreloadedGames, Has.Count.EqualTo(0));
        }
        
        [Test]
        public async Task LoadGameAsync_InvalidType_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () => 
                await _loader.LoadGameAsync(typeof(string)));
        }
        
        [Test]
        public void LoadGameAsync_NullType_ThrowsException()
        {
            // Act & Assert
            Assert.ThrowsAsync<ArgumentNullException>(async () => 
                await _loader.LoadGameAsync(null));
        }
        
        [Test]
        public async Task Events_LoadingSuccessful_FiresCorrectEvents()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            Type loadingStartedType = null;
            Type loadingCompletedType = null;
            
            _loader.OnGameLoadingStarted += (type) => loadingStartedType = type;
            _loader.OnGameLoadingCompleted += (type, game) => loadingCompletedType = type;
            
            // Act
            await _loader.LoadGameAsync(typeof(MockShortGame));
            
            // Assert
            Assert.AreEqual(typeof(MockShortGame), loadingStartedType);
            Assert.AreEqual(typeof(MockShortGame), loadingCompletedType);
        }
        
        [Test]
        public async Task Cancellation_StopsOperation()
        {
            // Arrange
            _queueService.Initialize(new[] { typeof(MockShortGame) });
            var cts = new CancellationTokenSource();
            cts.Cancel();
            
            // Act & Assert - TaskCanceledException derives from OperationCanceledException
            // but Unity's test framework doesn't handle inheritance properly
            Assert.ThrowsAsync<TaskCanceledException>(async () =>
                await _loader.LoadGameAsync(typeof(MockShortGame), cts.Token));
        }
        private void StartNextGameIfAvailable()
        {
            var nextType = _queueService.NextGameType;
            if (nextType == null)
            {
                return;
            }

            var started = _loader.StartPreloadedGame(nextType);
            if (!started)
            {
                Assert.Fail($"Failed to start preloaded game {nextType.Name}");
            }
        }
    }
}

