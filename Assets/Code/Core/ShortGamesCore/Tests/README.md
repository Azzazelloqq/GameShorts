# ShortGamesCore Tests

## Overview

This directory contains unit and integration tests for the ShortGamesCore system, including the new game loading architecture.

## Test Structure

### `/GamesLoader`
Tests for the new game loading architecture:
- **GameRegistryTests.cs** - Tests for game type registration
- **GameQueueServiceTests.cs** - Tests for queue management
- **QueueShortGamesLoaderTests.cs** - Tests for game loading and preloading
- **GameProviderTests.cs** - Tests for the game provider bridge

### `/Mocks`
Mock implementations for testing:
- **MockShortGame.cs** - Basic game implementation
- **MockPoolableShortGame.cs** - Poolable game implementation
- **MockShortGame2D.cs** - 2D game implementation
- **MockShortGame3D.cs** - 3D game implementation
- **MockShortGameUI.cs** - UI game implementation
- **MockLogger.cs** - Mock logger for testing
- **MockResourceLoader.cs** - Mock resource loader
- **MockDependencies.cs** - Other mock dependencies

### `/Factory`
Tests for game factory implementations:
- Factory creation tests
- Resource loading tests
- Game instantiation tests

### `/Performance`
Performance benchmarks and tests

## Running Tests

### In Unity Editor
1. Open Unity Test Runner: `Window > General > Test Runner`
2. Switch to "EditMode" tab
3. Click "Run All" or select specific tests

### Via Command Line
```bash
# Windows
.\RunTests.ps1

# Mac/Linux
./run-tests.sh
```

## New Architecture Tests

The test suite covers the new game loading architecture:

### GameRegistry
- Game type registration
- Validation of IShortGame implementation
- Event notifications
- Registry management

### GameQueueService
- Queue initialization
- Navigation (next, previous, by index)
- Queue state management
- Game preloading selection

### QueueShortGamesLoader
- Game loading and starting
- Game preloading without starting
- Batch preloading
- Game lifecycle management
- Resource management
- Event notifications

### GameProvider
- Service integration
- Game state management (pause, unpause, stop)
- Render texture access
- Preloaded games management

## Key Test Scenarios

### Basic Flow
```csharp
// 1. Register games
var registry = new GameRegistry(logger);
registry.RegisterGames(new[] { typeof(Game1), typeof(Game2) });

// 2. Initialize queue
var queueService = new GameQueueService(logger);

// 3. Create loader
var loader = new QueueShortGamesLoader(factory, queueService, logger);

// 4. Create provider
var provider = new GameProvider(logger);
await provider.InitializeAsync(registry, queueService, loader);

// 5. Navigate and control games
queueService.MoveNext();
await provider.UpdatePreloadedGamesAsync();
provider.StartCurrentGame();
```

### Preloading Test
```csharp
// Preload multiple games
var games = await loader.PreloadGamesAsync(gameTypes);

// Check preload status
Assert.IsTrue(loader.IsGameLoaded(gameType));
Assert.IsTrue(game.IsPreloaded);

// Start preloaded game
loader.StartPreloadedGame(gameType);
```

### Queue Navigation Test
```csharp
// Navigate forward
queueService.MoveNext();
Assert.IsTrue(queueService.HasNext);

// Navigate backward
queueService.MovePrevious();
Assert.IsTrue(queueService.HasPrevious);

// Jump to index
queueService.MoveToIndex(2);
```

## Mock Objects

### MockShortGame
Basic implementation with:
- Start/Stop/Pause functionality
- Preload support
- Render texture generation
- State tracking for assertions

### MockLogger
Captures all log messages for verification:
```csharp
Assert.That(logger.LoggedMessages, Has.Some.Contains("expected message"));
Assert.That(logger.LoggedErrors, Has.Count.EqualTo(0));
```

### MockResourceLoader
Simulates Unity's Addressables system:
```csharp
resourceLoader.AddResource("GamePrefab", prefab);
var loaded = await resourceLoader.LoadResourceAsync<GameObject>("GamePrefab");
```

## Writing New Tests

### Test Template
```csharp
[TestFixture]
public class MyComponentTests
{
    private MyComponent _component;
    private MockLogger _logger;
    
    [SetUp]
    public void SetUp()
    {
        _logger = new MockLogger();
        _component = new MyComponent(_logger);
    }
    
    [TearDown]
    public void TearDown()
    {
        _component?.Dispose();
    }
    
    [Test]
    public void MyMethod_ValidInput_ExpectedResult()
    {
        // Arrange
        var input = "test";
        
        // Act
        var result = _component.MyMethod(input);
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("expected", result);
    }
}
```

### Async Test Template
```csharp
[Test]
public async Task MyAsyncMethod_ValidInput_ExpectedResult()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = await _component.MyAsyncMethodAsync(input);
    
    // Assert
    Assert.IsNotNull(result);
}
```

## Test Coverage Goals

- **Unit Tests**: 80% code coverage minimum
- **Integration Tests**: Cover all major workflows
- **Performance Tests**: Benchmark critical paths

## Continuous Integration

Tests are automatically run on:
- Pull request creation
- Commits to main branch
- Nightly builds

## Troubleshooting

### Common Issues

1. **Tests not appearing in Test Runner**
   - Check assembly definition references
   - Ensure test class has `[TestFixture]` attribute
   - Methods must have `[Test]` attribute

2. **Mock objects not working**
   - Verify mock assembly is referenced
   - Check namespace imports

3. **Async tests timing out**
   - Use proper cancellation tokens
   - Add timeout attributes: `[Test, Timeout(5000)]`

## Contributing

When adding new features:
1. Write tests first (TDD approach)
2. Ensure all tests pass
3. Add integration tests for complex scenarios
4. Update this README if adding new test categories