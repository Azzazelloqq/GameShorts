using System;
using System.Collections.Generic;
using System.Linq;
using Code.Core.GamesLoader;
using Code.Core.ShotGamesCore.Tests.Mocks;
using NUnit.Framework;

namespace Code.Core.ShortGamesCore.Tests.GamesLoader
{
    [TestFixture]
    public class GameQueueServiceTests
    {
        private GameQueueService _queueService;
        private MockLogger _logger;
        
        [SetUp]
        public void SetUp()
        {
            _logger = new MockLogger();
            _queueService = new GameQueueService(_logger);
        }
        
        [Test]
        public void Initialize_WithValidTypes_SetsUpQueue()
        {
            // Arrange
            var gameTypes = new List<Type>
            {
                typeof(MockShortGame),
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            
            // Act
            _queueService.Initialize(gameTypes);
            
            // Assert
            Assert.AreEqual(3, _queueService.TotalGamesCount);
            Assert.AreEqual(-1, _queueService.CurrentIndex);
            Assert.IsNull(_queueService.CurrentGameType);
        }
        
        [Test]
        public void Initialize_EmptyList_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentException>(() => _queueService.Initialize(new List<Type>()));
        }
        
        [Test]
        public void Initialize_Null_ThrowsException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _queueService.Initialize(null));
        }
        
        [Test]
        public void MoveNext_FromStart_MovesToFirstGame()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame), typeof(MockPoolableShortGame) };
            _queueService.Initialize(gameTypes);
            
            // Act
            var result = _queueService.MoveNext();
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, _queueService.CurrentIndex);
            Assert.AreEqual(typeof(MockShortGame), _queueService.CurrentGameType);
            Assert.AreEqual(typeof(MockPoolableShortGame), _queueService.NextGameType);
            Assert.IsNull(_queueService.PreviousGameType);
        }
        
        [Test]
        public void MoveNext_AtEnd_ReturnsFalse()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame) };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext();
            
            // Act
            var result = _queueService.MoveNext();
            
            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, _queueService.CurrentIndex);
        }
        
        [Test]
        public void MovePrevious_FromMiddle_MovesToPreviousGame()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame), typeof(MockPoolableShortGame) };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext(); // Move to index 0
            _queueService.MoveNext(); // Move to index 1
            
            // Act
            var result = _queueService.MovePrevious();
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(0, _queueService.CurrentIndex);
            Assert.AreEqual(typeof(MockShortGame), _queueService.CurrentGameType);
        }
        
        [Test]
        public void MovePrevious_AtStart_ReturnsFalse()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame) };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext();
            
            // Act
            var result = _queueService.MovePrevious();
            
            // Assert
            Assert.IsFalse(result);
            Assert.AreEqual(0, _queueService.CurrentIndex);
        }
        
        [Test]
        public void MoveToIndex_ValidIndex_MovesToSpecificGame()
        {
            // Arrange
            var gameTypes = new List<Type> 
            { 
                typeof(MockShortGame), 
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            _queueService.Initialize(gameTypes);
            
            // Act
            var result = _queueService.MoveToIndex(2);
            
            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(2, _queueService.CurrentIndex);
            Assert.AreEqual(typeof(MockShortGame2D), _queueService.CurrentGameType);
        }
        
        [Test]
        public void MoveToIndex_InvalidIndex_ReturnsFalse()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame) };
            _queueService.Initialize(gameTypes);
            
            // Act
            var result1 = _queueService.MoveToIndex(-1);
            var result2 = _queueService.MoveToIndex(100);
            
            // Assert
            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
        }
        
        [Test]
        public void HasNext_WhenNextExists_ReturnsTrue()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame), typeof(MockPoolableShortGame) };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext();
            
            // Act & Assert
            Assert.IsTrue(_queueService.HasNext);
        }
        
        [Test]
        public void HasNext_AtEnd_ReturnsFalse()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame) };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext();
            
            // Act & Assert
            Assert.IsFalse(_queueService.HasNext);
        }
        
        [Test]
        public void HasPrevious_WhenPreviousExists_ReturnsTrue()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame), typeof(MockPoolableShortGame) };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext();
            _queueService.MoveNext();
            
            // Act & Assert
            Assert.IsTrue(_queueService.HasPrevious);
        }
        
        [Test]
        public void GetGamesToPreload_ReturnsCurrentNextPrevious()
        {
            // Arrange
            var gameTypes = new List<Type> 
            { 
                typeof(MockShortGame), 
                typeof(MockPoolableShortGame),
                typeof(MockShortGame2D)
            };
            _queueService.Initialize(gameTypes);
            _queueService.MoveToIndex(1); // Middle position
            
            // Act
            var toPreload = _queueService.GetGamesToPreload().ToList();
            
            // Assert
            Assert.AreEqual(3, toPreload.Count);
            Assert.Contains(typeof(MockShortGame), toPreload);       // Previous
            Assert.Contains(typeof(MockPoolableShortGame), toPreload); // Current
            Assert.Contains(typeof(MockShortGame2D), toPreload);      // Next
        }
        
        [Test]
        public void GetGamesToPreload_AtStart_ReturnsCurrentAndNext()
        {
            // Arrange
            var gameTypes = new List<Type> 
            { 
                typeof(MockShortGame), 
                typeof(MockPoolableShortGame)
            };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext();
            
            // Act
            var toPreload = _queueService.GetGamesToPreload().ToList();
            
            // Assert
            Assert.AreEqual(2, toPreload.Count);
            Assert.Contains(typeof(MockShortGame), toPreload);        // Current
            Assert.Contains(typeof(MockPoolableShortGame), toPreload); // Next
        }
        
        [Test]
        public void GetGameTypeAtIndex_ValidIndex_ReturnsType()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame), typeof(MockPoolableShortGame) };
            _queueService.Initialize(gameTypes);
            
            // Act
            var type = _queueService.GetGameTypeAtIndex(1);
            
            // Assert
            Assert.AreEqual(typeof(MockPoolableShortGame), type);
        }
        
        [Test]
        public void Reset_ReturnsToBeginning()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame), typeof(MockPoolableShortGame) };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext();
            _queueService.MoveNext();
            
            // Act
            _queueService.Reset();
            
            // Assert
            Assert.AreEqual(-1, _queueService.CurrentIndex);
            Assert.IsNull(_queueService.CurrentGameType);
            Assert.AreEqual(2, _queueService.TotalGamesCount);
        }
        
        [Test]
        public void Clear_RemovesAllGames()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame), typeof(MockPoolableShortGame) };
            _queueService.Initialize(gameTypes);
            _queueService.MoveNext();
            
            // Act
            _queueService.Clear();
            
            // Assert
            Assert.AreEqual(0, _queueService.TotalGamesCount);
            Assert.AreEqual(-1, _queueService.CurrentIndex);
            Assert.IsNull(_queueService.CurrentGameType);
        }
        
        [Test]
        public void OnQueueUpdated_Event_FiresOnChanges()
        {
            // Arrange
            var gameTypes = new List<Type> { typeof(MockShortGame), typeof(MockPoolableShortGame) };
            _queueService.Initialize(gameTypes);
            int eventCount = 0;
            _queueService.OnQueueUpdated += () => eventCount++;
            
            // Act & Assert each action
            _queueService.MoveNext(); // Move to index 0 - should fire event
            Assert.AreEqual(1, eventCount, "Event should fire after MoveNext");
            
            // MovePrevious from index 0 won't work (no previous), so no event
            bool movedBack = _queueService.MovePrevious();
            Assert.IsFalse(movedBack, "Should not be able to move previous from index 0");
            Assert.AreEqual(1, eventCount, "Event count should not change after failed MovePrevious");
            
            _queueService.MoveToIndex(1); // Move to index 1 - should fire event
            Assert.AreEqual(2, eventCount, "Event should fire after MoveToIndex");
            
            _queueService.Reset(); // Reset to -1 - should fire event
            Assert.AreEqual(3, eventCount, "Event should fire after Reset");
            
            _queueService.Clear(); // Clear queue - should fire event
            Assert.AreEqual(4, eventCount, "Event should fire after Clear");
        }
    }
}

