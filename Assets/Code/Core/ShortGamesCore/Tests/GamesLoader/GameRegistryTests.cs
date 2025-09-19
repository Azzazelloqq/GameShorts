using System;
using System.Collections.Generic;
using Code.Core.GamesLoader;
using Code.Core.GamesLoader.TestHelpers;
using Code.Core.ShotGamesCore.Tests.Mocks;
using InGameLogger;
using NUnit.Framework;

namespace Code.Core.ShortGamesCore.Tests.GamesLoader
{
    [TestFixture]
    public class GameRegistryTests
    {
        private GameRegistry _registry;
        private MockLogger _logger;
        
        [SetUp]
        public void SetUp()
        {
            _logger = new MockLogger();
            _registry = new GameRegistry(_logger);
        }
        
        [Test]
        public void RegisterGame_ValidGameType_ReturnsTrue()
        {
            // Act
            var result = _registry.RegisterGame(typeof(MockShortGame));
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(1, _registry.Count);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Registered game: MockShortGame"));
        }
        
        [Test]
        public void RegisterGame_DuplicateType_ReturnsFalse()
        {
            // Arrange
            _registry.RegisterGame(typeof(MockShortGame));
            
            // Act
            var result = _registry.RegisterGame(typeof(MockShortGame));
            
            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(1, _registry.Count);
            Assert.That(_logger.LoggedWarnings, Has.Some.Contains("already registered"));
        }
        
        [Test]
        public void RegisterGame_InvalidType_ReturnsFalse()
        {
            // Act
            var result = _registry.RegisterGame(typeof(string));
            
            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, _registry.Count);
            Assert.That(_logger.LoggedErrors, Has.Some.Contains("does not implement IShortGame"));
        }
        
        [Test]
        public void RegisterGame_NullType_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _registry.RegisterGame(null));
        }
        
        [Test]
        public void RegisterGames_MultipleTypes_RegistersAll()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            
            // Act
            _registry.RegisterGames(gameTypes);
            
            // Assert
            Assert.AreEqual(3, _registry.Count);
            Assert.IsTrue(_registry.IsGameRegistered(typeof(MockShortGame)));
            Assert.IsTrue(_registry.IsGameRegistered(typeof(MockPoolableShortGame)));
            Assert.IsTrue(_registry.IsGameRegistered(typeof(MockShortGame2D)));
        }
        
        [Test]
        public void UnregisterGame_ExistingGame_ReturnsTrue()
        {
            // Arrange
            _registry.RegisterGame(typeof(MockShortGame));
            
            // Act
            var result = _registry.UnregisterGame(typeof(MockShortGame));
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, _registry.Count);
            Assert.IsFalse(_registry.IsGameRegistered(typeof(MockShortGame)));
        }
        
        [Test]
        public void GetGameTypeByIndex_ValidIndex_ReturnsType()
        {
            // Arrange
            _registry.RegisterGame(typeof(MockShortGame));
            _registry.RegisterGame(typeof(MockPoolableShortGame));
            
            // Act
            var type = _registry.GetGameTypeByIndex(1);
            
            // Assert
            Assert.AreEqual(typeof(MockPoolableShortGame), type);
        }
        
        [Test]
        public void GetGameTypeByIndex_InvalidIndex_ReturnsNull()
        {
            // Arrange
            _registry.RegisterGame(typeof(MockShortGame));
            
            // Act
            var type1 = _registry.GetGameTypeByIndex(-1);
            var type2 = _registry.GetGameTypeByIndex(100);
            
            // Assert
            Assert.IsNull(type1);
            Assert.IsNull(type2);
        }
        
        [Test]
        public void GetIndexOfGameType_ExistingType_ReturnsIndex()
        {
            // Arrange
            _registry.RegisterGame(typeof(MockShortGame));
            _registry.RegisterGame(typeof(MockPoolableShortGame));
            
            // Act
            var index = _registry.GetIndexOfGameType(typeof(MockPoolableShortGame));
            
            // Assert
            Assert.AreEqual(1, index);
        }
        
        [Test]
        public void Clear_RemovesAllGames()
        {
            // Arrange
            _registry.RegisterGame(typeof(MockShortGame));
            _registry.RegisterGame(typeof(MockPoolableShortGame));
            
            // Act
            _registry.Clear();
            
            // Assert
            Assert.AreEqual(0, _registry.Count);
            Assert.That(_logger.LoggedMessages, Has.Some.Contains("Clearing game registry"));
        }
        
        [Test]
        public void OnGameRegistered_Event_FiresWhenGameRegistered()
        {
            // Arrange
            Type registeredType = null;
            _registry.OnGameRegistered += (type) => registeredType = type;
            
            // Act
            _registry.RegisterGame(typeof(MockShortGame));
            
            // Assert
            Assert.AreEqual(typeof(MockShortGame), registeredType);
        }
        
        [Test]
        public void OnGameUnregistered_Event_FiresWhenGameUnregistered()
        {
            // Arrange
            _registry.RegisterGame(typeof(MockShortGame));
            Type unregisteredType = null;
            _registry.OnGameUnregistered += (type) => unregisteredType = type;
            
            // Act
            _registry.UnregisterGame(typeof(MockShortGame));
            
            // Assert
            Assert.AreEqual(typeof(MockShortGame), unregisteredType);
        }
    }
}

