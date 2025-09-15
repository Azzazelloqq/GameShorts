# Game System Usage Example

## Architecture Overview

The game system has a clear architecture where each component has a specific responsibility:

1. **IGameRegistry** - Stores all available game types in the system (provided explicitly)
2. **IGameQueueService** - Manages the order of games (current, next, previous)
3. **IGamesLoader** - Handles loading and preloading of game instances via IShortGameFactory
4. **IGameProvider** - Simple bridge that provides access to all services without business logic

> Note: `IShortGameLifeCycleService` is no longer used. The system now works directly with `IShortGameFactory`.

## Complete Initialization Example

```csharp
// 1. Create and populate the registry with your games
var gameRegistry = new GameRegistry(logger);

// Register specific games explicitly
var gameTypes = new List<Type> 
{ 
    typeof(Game1), 
    typeof(Game2), 
    typeof(Game3),
    typeof(Game4)
};
gameRegistry.RegisterGames(gameTypes);

// Or register one by one
gameRegistry.RegisterGame(typeof(Game1));
gameRegistry.RegisterGame(typeof(Game2));

// 2. Create the queue service
var queueService = new GameQueueService(logger);

// 3. Create the games loader with factory
var gamesLoader = new QueueShortGamesLoader(gameFactory, queueService, logger);

// 4. Create the provider (bridge)
var gameProvider = new GameProvider(logger);

// 5. Initialize the provider with all services
await gameProvider.InitializeAsync(
    gameRegistry,
    queueService, 
    gamesLoader,
    cancellationToken);

// The queue is automatically initialized with games from the registry
// Now you can control games through the provider
```

## Using the Game Provider

```csharp
// The provider is a simple bridge - all operations are delegated

// Access services directly
var registry = gameProvider.GameRegistry;     // All registered games
var queue = gameProvider.QueueService;        // Queue management
var loader = gameProvider.GamesLoader;        // Loading/preloading

// Get current games
var currentGame = gameProvider.CurrentGame;
var nextGame = gameProvider.NextGame;
var previousGame = gameProvider.PreviousGame;

// Get render textures
var currentTexture = gameProvider.CurrentGameRenderTexture;
var nextTexture = gameProvider.NextGameRenderTexture;
var previousTexture = gameProvider.PreviousGameRenderTexture;

// Control games (externally driven)
gameProvider.StartCurrentGame();
gameProvider.PauseCurrentGame();
gameProvider.UnpauseCurrentGame();

gameProvider.StartNextGame();
gameProvider.PauseNextGame();

gameProvider.PauseAllGames();
gameProvider.UnpauseAllGames();
gameProvider.StopAllGames();

// Update preloaded games when queue changes
await gameProvider.UpdatePreloadedGamesAsync();
```

## Queue Navigation

```csharp
// Navigate through the queue
if (gameProvider.QueueService.HasNext)
{
    gameProvider.QueueService.MoveNext();
    await gameProvider.UpdatePreloadedGamesAsync();
    gameProvider.StartCurrentGame();
}

if (gameProvider.QueueService.HasPrevious)
{
    gameProvider.QueueService.MovePrevious();
    await gameProvider.UpdatePreloadedGamesAsync();
    gameProvider.StartCurrentGame();
}

// Jump to specific index
gameProvider.QueueService.MoveToIndex(5);
await gameProvider.UpdatePreloadedGamesAsync();
gameProvider.StartCurrentGame();
```

## Working with the Registry

```csharp
// Create registry
var registry = new GameRegistry(logger);

// Register games explicitly
registry.RegisterGame(typeof(MyGame));
registry.RegisterGames(new[] { typeof(Game1), typeof(Game2) });

// Check registration
bool isRegistered = registry.IsGameRegistered(typeof(MyGame));

// Get by index
var gameType = registry.GetGameTypeByIndex(0);

// Get index of type
int index = registry.GetIndexOfGameType(typeof(MyGame));

// Access all registered games
IReadOnlyList<Type> allGames = registry.RegisteredGames;
int gameCount = registry.Count;

// Events
registry.OnGameRegistered += (type) => Debug.Log($"Game registered: {type.Name}");
registry.OnGameUnregistered += (type) => Debug.Log($"Game unregistered: {type.Name}");

// Unregister
registry.UnregisterGame(typeof(MyGame));

// Clear all
registry.Clear();
```

## Component Responsibilities

### IGameRegistry
- Stores explicitly provided game types
- Validates that types implement IShortGame
- Provides events for registration changes

### IGameQueueService  
- Manages game order (linear queue in first iteration)
- Tracks current, next, previous positions
- Will be replaced with dynamic queue later

### IGamesLoader
- Creates game instances via IShortGameFactory (not IShortGameLifeCycleService)
- Manages loaded and preloaded game instances
- Handles preloading for smooth transitions

### IGameProvider
- Simple bridge/facade over other services
- No business logic, just delegates calls
- External code controls when to start/pause/stop games

## Key Design Principles

1. **Explicit Configuration** - Games must be explicitly registered, no auto-discovery
2. **Separation of Concerns** - Each component has one responsibility
3. **External Control** - The provider doesn't decide when to pause/start, external code does
4. **Preloading** - Next and previous games are preloaded for instant switching
5. **Clean Architecture** - Interfaces allow easy replacement of implementations