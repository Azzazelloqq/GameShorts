# ShortGames Core - Test Suite

## Overview
Comprehensive test suite for the ShortGames Core system, covering unit tests, integration tests, and performance benchmarks.

## Test Categories

### 1. Unit Tests
Individual component testing in isolation:
- **Factory Tests**: Game creation and resource management
- **Pool Tests**: Object pooling operations
- **LifeCycleService Tests**: Game lifecycle management

### 2. Integration Tests
System-wide interaction testing:
- **Full cycle preload and switch**
- **Pooling with lifecycle management**
- **Resource management flows**
- **Error handling scenarios**

### 3. Performance Tests
Benchmarking critical operations:
- **Pool efficiency measurements**
- **Preloading performance**
- **Memory usage tracking**
- **Rapid switching benchmarks**

## Running Tests

### Unity Test Runner
1. Open Unity Editor
2. Navigate to `Window > General > Test Runner`
3. Select tests to run or click "Run All"

### Command Line (Windows)
```powershell
# Run all tests
.\RunTests.ps1

# Run specific category
.\RunTests.ps1 -TestFilter "Code.Core.ShotGamesCore.Tests.Factory"
.\RunTests.ps1 -TestFilter "Code.Core.ShotGamesCore.Tests.Pool"
.\RunTests.ps1 -TestFilter "Code.Core.ShotGamesCore.Tests.Integration"
```

### Command Line (Linux/Mac)
```bash
# Run all tests
./run-tests.sh

# Run specific category
./run-tests.sh --filter "Code.Core.ShotGamesCore.Tests.Factory"
```

## Test Architecture

### Assembly Structure
```
Core.ShortGame.Tests (Editor-only)
├── Factory/
│   ├── AddressableShortGameFactoryTests.cs
│   └── AddressableShortGameFactoryDebugTest.cs
├── LifeCycleService/
│   └── SimpleShortGameLifeCycleServiceTests.cs
├── Pool/
│   └── SimpleShortGamePoolTests.cs
├── Integration/
│   └── ShortGamesSystemIntegrationTests.cs
├── Performance/
│   └── PerformanceTests.cs
└── Mocks/ → Moved to separate assembly

Core.ShortGame.Tests.Mocks (Runtime)
├── MockShortGame.cs
├── MockDependencies.cs
└── Core.ShortGame.Tests.Mocks.asmdef
```

### Mock Implementations

#### MockShortGame
Basic implementation of IShortGame for testing:
```csharp
public class MockShortGame : MonoBehaviour, IShortGame
{
    public bool IsStarted { get; private set; }
    public bool IsPaused { get; private set; }
    public int StartCallCount { get; private set; }
    // ... tracking properties for testing
}
```

#### MockPoolableShortGame
Poolable version with lifecycle callbacks:
```csharp
public class MockPoolableShortGame : MonoBehaviour, IPoolableShortGame
{
    public int OnPooledCallCount { get; private set; }
    public int OnUnpooledCallCount { get; private set; }
    // ... additional pooling tracking
}
```

#### Mock Dependencies
- **MockLogger**: Tracks all logging calls
- **MockResourceLoader**: Simulates resource loading
- **MockShortGameFactory**: Controls game creation
- **MockShortGamesPool**: Manages pooling behavior

## Test Coverage

### Factory Tests
- ✅ Create game instance (generic and by type)
- ✅ Preload resources
- ✅ Use preloaded prefabs
- ✅ Handle missing resources
- ✅ Cancellation support
- ✅ Reference counting
- ✅ Disposal cleanup

### Pool Tests
- ✅ Get/release games
- ✅ Pool capacity limits
- ✅ Warm up pool
- ✅ Clear pool by type
- ✅ Active game tracking
- ✅ Disposal

### LifeCycleService Tests
- ✅ Load games (pooled and new)
- ✅ Stop current game
- ✅ Preload game lists
- ✅ Navigate next/previous
- ✅ Clear preloaded games
- ✅ Error handling
- ✅ Disposal

### Integration Tests
- ✅ Full preload and switch cycle
- ✅ Pool reuse across lifecycle
- ✅ Mixed poolable/non-poolable games
- ✅ Resource management
- ✅ Navigation flow
- ✅ Error scenarios
- ✅ Cleanup and disposal

### Performance Tests
- ✅ Pool vs new instance creation
- ✅ Preloading efficiency
- ✅ Memory usage
- ✅ Rapid sequential operations
- ✅ Large scale pooling

## Common Test Patterns

### Setup and Teardown
```csharp
[SetUp]
public void SetUp()
{
    _logger = new MockLogger();
    _pool = new SimpleShortGamePool(_logger);
    _testGameObject = new GameObject("TestGame");
}

[TearDown]
public void TearDown()
{
    // Clean up in correct order
    if (_testGameObject != null)
        GameObject.DestroyImmediate(_testGameObject);
    
    _pool?.Dispose();
}
```

### Async Testing
```csharp
[Test]
public async Task LoadGameAsync_CreatesNewInstance()
{
    // Arrange
    var factory = new MockShortGameFactory();
    
    // Act
    var game = await service.LoadGameAsync<MockShortGame>();
    
    // Assert
    Assert.IsNotNull(game);
    Assert.IsTrue(game.IsStarted);
}
```

### Performance Benchmarking
```csharp
[Test]
public void MeasurePoolPerformance()
{
    var sw = Stopwatch.StartNew();
    
    // Perform operations
    for (int i = 0; i < 100; i++)
    {
        _pool.TryGetShortGame<MockPoolableShortGame>(out var game);
        _pool.ReleaseShortGame(game);
    }
    
    sw.Stop();
    Assert.Less(sw.ElapsedMilliseconds, 100);
}
```

## Unity-Specific Considerations

### GameObject Creation
- All GameObject operations must happen on the main thread
- Use `DestroyImmediate` in Editor mode tests
- Check for null before destroying

### Async Operations
- Use `async Task` instead of `async void`
- Handle cancellation tokens properly
- Avoid `Task.Run` for GameObject operations

### Test Attributes
- `[Test]` - Standard synchronous test
- `[UnityTest]` - Tests that yield or use coroutines
- `[TestFixture]` - Group related tests
- `[SetUp]`/`[TearDown]` - Test lifecycle hooks

## Troubleshooting

### Common Issues

1. **"Can't add script behaviour because it is an editor script"**
   - Solution: Mocks are in separate runtime assembly

2. **"Destroy may not be called from edit mode"**
   - Solution: Use `DestroyImmediate` in editor tests

3. **"Internal_CreateGameObject can only be called from the main thread"**
   - Solution: Don't use `Task.Run` for GameObject operations

4. **"The object has been destroyed but you are still trying to access it"**
   - Solution: Check for null before operations

## Contributing

When adding new tests:
1. Follow existing patterns and naming conventions
2. Add appropriate test categories
3. Include setup and teardown
4. Handle Unity-specific requirements
5. Document complex test scenarios
6. Ensure thread safety for Unity operations