using System;
using Code.Core.ShortGamesCore.Source.GameCore;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;

namespace Code.Core.ShotGamesCore.Tests
{
    [TestFixture]
    public class GameTypeDetectorTests
    {
        [Test]
        public void GetGameType_3DGame_ReturnsThreeD()
        {
            // Arrange
            var gameType = typeof(MockShortGame3D);
            
            // Act
            var result = GameTypeDetector.GetGameType(gameType);
            
            // Assert
            Assert.AreEqual(GameType.ThreeD, result);
        }
        
        [Test]
        public void GetGameType_2DGame_ReturnsTwoD()
        {
            // Arrange
            var gameType = typeof(MockShortGame2D);
            
            // Act
            var result = GameTypeDetector.GetGameType(gameType);
            
            // Assert
            Assert.AreEqual(GameType.TwoD, result);
        }
        
        [Test]
        public void GetGameType_UIGame_ReturnsUI()
        {
            // Arrange
            var gameType = typeof(MockShortGameUI);
            
            // Act
            var result = GameTypeDetector.GetGameType(gameType);
            
            // Assert
            Assert.AreEqual(GameType.UI, result);
        }
        
        [Test]
        public void GetGameType_BaseShortGame_ReturnsThreeD()
        {
            // Arrange - игра реализует только IShortGame
            var gameType = typeof(MockShortGame);
            
            // Act
            var result = GameTypeDetector.GetGameType(gameType);
            
            // Assert - должна вернуть 3D по умолчанию для обратной совместимости
            Assert.AreEqual(GameType.ThreeD, result);
        }
        
        [Test]
        public void GetGameType_NotShortGame_ThrowsException()
        {
            // Arrange
            var nonGameType = typeof(string);
            
            // Act & Assert
            Assert.Throws<ArgumentException>(() => GameTypeDetector.GetGameType(nonGameType));
        }
        
        [Test]
        public void IsPoolable_PoolableGame_ReturnsTrue()
        {
            // Arrange
            var poolableGameType = typeof(MockPoolableShortGame);
            
            // Act
            var result = GameTypeDetector.IsPoolable(poolableGameType);
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void IsPoolable_NonPoolableGame_ReturnsFalse()
        {
            // Arrange
            var nonPoolableGameType = typeof(MockShortGame);
            
            // Act
            var result = GameTypeDetector.IsPoolable(nonPoolableGameType);
            
            // Assert
            Assert.IsFalse(result);
        }
        
        [Test]
        public void IsPoolable_Poolable3DGame_ReturnsTrue()
        {
            // Arrange
            var poolableGameType = typeof(MockPoolableShortGame3D);
            
            // Act
            var result = GameTypeDetector.IsPoolable(poolableGameType);
            
            // Assert
            Assert.IsTrue(result);
        }
        
        [Test]
        public void GetGameType_Poolable3DGame_ReturnsThreeD()
        {
            // Arrange
            var gameType = typeof(MockPoolableShortGame3D);
            
            // Act
            var result = GameTypeDetector.GetGameType(gameType);
            
            // Assert
            Assert.AreEqual(GameType.ThreeD, result);
        }
    }
}
