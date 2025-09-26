# ShortGames Core System

## Overview
A high-performance mini-game management system inspired by TikTok/YouTube Shorts, designed for Unity. The system provides efficient game switching, resource preloading, and object pooling for seamless transitions between mini-games.

## Key Features
- **Fast Game Switching**: Instant transitions between mini-games
- **Resource Preloading**: Pre-cache game resources for immediate access  
- **Object Pooling**: Reuse game instances to minimize allocations
- **Async Operations**: Non-blocking loading with cancellation support
- **Memory Efficient**: Smart resource management and cleanup

## Architecture

### Core Components

#### 1. IShortGame & IPoolableShortGame
Base interfaces for all mini-games:
```csharp
public interface IShortGame
{
    void Start();
    void Pause();
    void Resume();
    void Restart();
    void Stop();
}

public interface IPoolableShortGame : IShortGame
{
    void OnPooled();    // Called when returned to pool
    void OnUnpooled();  // Called when taken from pool
}
```

#### 2. IShortGameLifeCycleService
Manages game lifecycle and navigation:
- Load/stop games
- Preload game lists
- Navigate next/previous
- Clear preloaded resources

#### 3. IShortGameFactory
Creates game instances and manages resources:
- Create game instances
- Preload game prefabs
- Unload unused resources

#### 4. IShortGamesPool
Object pool for reusable games:
- Get/release pooled games
- Warm up pool with instances
- Clear pool by type

## Usage Examples

### Basic Setup
```csharp
// Configure dependencies
var logger = new InGameLogger();
var resourceLoader = new AddressableResourceLoader();
var pool = new SimpleShortGamePool(logger, maxInstancesPerType: 3);
var factory = new AddressableShortGameFactory(parent, resourcesInfo, resourceLoader, logger);
var lifeCycleService = new SimpleShortGameLifeCycleService(pool, factory, logger);
```

### Preload Games
```csharp
// Preload a list of games for quick switching
var gameTypes = new List<Type> 
{
    typeof(PuzzleGame),
    typeof(RunnerGame),
    typeof(ShooterGame)
};

await lifeCycleService.PreloadGamesAsync(gameTypes);
```

### Navigate Between Games
```csharp
// Load first game
var game = await lifeCycleService.LoadNextGameAsync();

// Switch to next game (cycles through preloaded list)
game = await lifeCycleService.LoadNextGameAsync();

// Go back to previous game
game = await lifeCycleService.LoadPreviousGameAsync();
```

### Direct Game Loading
```csharp
// Load specific game type
var puzzleGame = await lifeCycleService.LoadGameAsync<PuzzleGame>();

// Stop current game (returns to pool if poolable)
lifeCycleService.StopCurrentGame();
```

## Configuration

### ShortGameSystemConfig
ScriptableObject for system configuration:
```csharp
[CreateAssetMenu(fileName = "ShortGameSystemConfig", menuName = "ShortGames/Config")]
public class ShortGameSystemConfig : ScriptableObject
{
    public Dictionary<Type, string> GameResourceMapping;
    public Transform DefaultParent;
    public int MaxPoolSizePerType = 3;
}
```

## Performance Optimization

### Object Pooling
- Poolable games are reused instead of destroyed
- Reduces garbage collection pressure
- Instant game switching for pooled instances

### Resource Management
- Preload frequently used games
- Reference counting for shared resources
- Automatic cleanup of unused resources

### Best Practices
1. **Use Poolable Games** when possible for better performance
2. **Preload games** that will be accessed frequently
3. **Clear unused resources** when switching game sets
4. **Handle cancellation** properly in async operations

## Integration with Input System

### Example: Scroll Navigation
```csharp
public class ShortGamesScrollController : MonoBehaviour
{
    private IShortGameLifeCycleService _lifeCycleService;
    
    void OnScroll(InputValue value)
    {
        var scrollDelta = value.Get<Vector2>().y;
        
        if (scrollDelta > 0)
            _ = _lifeCycleService.LoadPreviousGameAsync();
        else if (scrollDelta < 0)
            _ = _lifeCycleService.LoadNextGameAsync();
    }
}
```

## Testing

The system includes comprehensive test coverage:
- Unit tests for each component
- Integration tests for system interactions
- Performance benchmarks
- Mock implementations for isolated testing

Run tests using:
```bash
# Unity Test Runner
Window > General > Test Runner > Run All

# Command line
./RunTests.ps1 -TestFilter "Code.Core.ShotGamesCore.Tests"
```

## Assembly Structure

```
Core.ShortGame (Runtime)
├── Source/
│   ├── Factory/
│   ├── GameCore/
│   ├── LifeCycleService/
│   └── Pool/

Core.ShortGame.Tests.Mocks (Runtime)
├── Mock implementations for testing

Core.ShortGame.Tests (Editor)
├── Unit, Integration, and Performance tests
```

## Requirements
- Unity 2021.3+
- Addressables package
- LightDI for dependency injection

## License
[Your License Here]