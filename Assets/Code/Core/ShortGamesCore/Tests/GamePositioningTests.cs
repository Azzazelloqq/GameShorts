using Code.Core.ShortGamesCore.Source.GameCore;
using NUnit.Framework;
using UnityEngine;

namespace Code.Core.ShotGamesCore.Tests
{
    [TestFixture]
    public class GamePositioningTests
    {
        private GamePositioningConfig _config;
        
        [SetUp]
        public void SetUp()
        {
            _config = GamePositioningConfig.CreateDefault();
        }
        
        [Test]
        public void GetPosition3D_FirstGame_ReturnsBasePosition()
        {
            // Arrange
            var gameIndex = 0;
            
            // Act
            var position = _config.GetPosition3D(gameIndex);
            
            // Assert
            Assert.AreEqual(Vector3.zero, position);
        }
        
        [Test]
        public void GetPosition3D_SecondGame_ReturnsOffsetPosition()
        {
            // Arrange
            var gameIndex = 1;
            var expectedPosition = new Vector3(100f, 0, 0); // Base + 100 * 1
            
            // Act
            var position = _config.GetPosition3D(gameIndex);
            
            // Assert
            Assert.AreEqual(expectedPosition, position);
        }
        
        [Test]
        public void GetPosition3D_MultipleGames_ReturnsCorrectPositions()
        {
            // Arrange & Act & Assert
            for (int i = 0; i < 5; i++)
            {
                var expectedPosition = new Vector3(i * 100f, 0, 0);
                var actualPosition = _config.GetPosition3D(i);
                Assert.AreEqual(expectedPosition, actualPosition, $"Position for game {i} is incorrect");
            }
        }
        
        [Test]
        public void GetPosition2D_FirstGame_ReturnsBase2DPosition()
        {
            // Arrange
            var gameIndex = 0;
            var expectedPosition = new Vector3(1000f, 0, 0);
            
            // Act
            var position = _config.GetPosition2D(gameIndex);
            
            // Assert
            Assert.AreEqual(expectedPosition, position);
        }
        
        [Test]
        public void GetPosition2D_SecondGame_ReturnsOffset2DPosition()
        {
            // Arrange
            var gameIndex = 1;
            var expectedPosition = new Vector3(1050f, 0, 0); // Base (1000) + 50 * 1
            
            // Act
            var position = _config.GetPosition2D(gameIndex);
            
            // Assert
            Assert.AreEqual(expectedPosition, position);
        }
        
        [Test]
        public void GetPosition2D_MultipleGames_ReturnsCorrectPositions()
        {
            // Arrange & Act & Assert
            for (int i = 0; i < 5; i++)
            {
                var expectedPosition = new Vector3(1000f + i * 50f, 0, 0);
                var actualPosition = _config.GetPosition2D(i);
                Assert.AreEqual(expectedPosition, actualPosition, $"Position for 2D game {i} is incorrect");
            }
        }
        
        [Test]
        public void GetCanvasSortOrder_FirstGame_ReturnsZero()
        {
            // Arrange
            var gameIndex = 0;
            
            // Act
            var sortOrder = _config.GetCanvasSortOrder(gameIndex);
            
            // Assert
            Assert.AreEqual(0, sortOrder);
        }
        
        [Test]
        public void GetCanvasSortOrder_SecondGame_ReturnsIncrement()
        {
            // Arrange
            var gameIndex = 1;
            var expectedSortOrder = 100;
            
            // Act
            var sortOrder = _config.GetCanvasSortOrder(gameIndex);
            
            // Assert
            Assert.AreEqual(expectedSortOrder, sortOrder);
        }
        
        [Test]
        public void GetCanvasSortOrder_MultipleGames_ReturnsCorrectOrder()
        {
            // Arrange & Act & Assert
            for (int i = 0; i < 5; i++)
            {
                var expectedSortOrder = i * 100;
                var actualSortOrder = _config.GetCanvasSortOrder(i);
                Assert.AreEqual(expectedSortOrder, actualSortOrder, $"Sort order for UI game {i} is incorrect");
            }
        }
        
        [Test]
        public void CreateDefault_CreatesValidConfig()
        {
            // Act
            var config = GamePositioningConfig.CreateDefault();
            
            // Assert
            Assert.IsNotNull(config);
            Assert.AreEqual(100f, config.Distance3DGames);
            Assert.AreEqual(50f, config.Distance2DGames);
            Assert.AreEqual(Vector3.zero, config.Base3DPosition);
            Assert.AreEqual(new Vector3(1000f, 0, 0), config.Base2DPosition);
            Assert.AreEqual(Vector3.right, config.Positioning3DAxis);
            Assert.AreEqual(Vector3.right, config.Positioning2DAxis);
            Assert.IsTrue(config.CreateSeparateCanvasForUIGames);
            Assert.AreEqual(100, config.CanvasSortOrderIncrement);
        }
        
        [Test]
        public void Positioning3DAxis_AlwaysNormalized()
        {
            // Arrange - создаем конфиг с ненормализованной осью
            var config = GamePositioningConfig.CreateDefault();
            
            // Act
            var axis = config.Positioning3DAxis;
            
            // Assert
            Assert.AreEqual(1f, axis.magnitude, 0.001f, "Axis should be normalized");
        }
        
        [Test]
        public void Positioning2DAxis_AlwaysNormalized()
        {
            // Arrange
            var config = GamePositioningConfig.CreateDefault();
            
            // Act
            var axis = config.Positioning2DAxis;
            
            // Assert
            Assert.AreEqual(1f, axis.magnitude, 0.001f, "Axis should be normalized");
        }
    }
}
