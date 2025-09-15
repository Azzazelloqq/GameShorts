# GamesLoader Module

## IGameProvider - The Core Interface

`IGameProvider` is the main interface for game management. It handles business logic only and knows nothing about UI.

## Key Methods

### Game Switching (Business Logic Only)

```csharp
// Switch to next game in queue
// Called by UI controller when user swipes
// Provider handles: pause current, move queue, load new, update preloads
Task<bool> SwipeToNextGameAsync(CancellationToken ct);

// Switch to previous game in queue  
Task<bool> SwipeToPreviousGameAsync(CancellationToken ct);
```

These methods:
- ✅ Pause current game
- ✅ Move queue position
- ✅ Load new game
- ✅ Stop old game
- ✅ Update preloaded games
- ❌ DO NOT handle animation
- ❌ DO NOT know about UI
- ❌ DO NOT manage swipe gestures

### Proper Implementation Example

```csharp
public class GameProvider : IGameProvider
{
    public async Task<bool> SwipeToNextGameAsync(CancellationToken ct)
    {
        // 1. Check if can switch
        if (!QueueService.HasNext) 
            return false;
        
        // 2. Pause current game
        CurrentGame?.PauseGame();
        
        // 3. Move queue
        QueueService.MoveNext();
        
        // 4. Load new current game
        var newGame = await GamesLoader.LoadGameAsync(
            QueueService.CurrentGameType, ct);
        
        // 5. Stop old game
        CurrentGame?.StopGame();
        
        // 6. Update references
        CurrentGame = newGame;
        CurrentGame?.StartGame();
        
        // 7. Update preloaded games
        await UpdatePreloadedGamesAsync(ct);
        
        return true;
    }
}
```

## Architecture Separation

```
UI Layer (GameSwiper):
└── User swipes/clicks
    └── Fires event

Controller Layer:
└── Handles UI event
    └── Calls provider.SwipeToNextGameAsync()
    └── Updates UI with new textures

Business Layer (IGameProvider):
└── Switches game logic
    └── Returns success/failure
```

## Important Notes

1. **IGameProvider is UI-agnostic** - It doesn't know if switch was from swipe, button, or code
2. **Methods are async** - Game loading may take time
3. **Returns bool** - Indicates if switch was successful
4. **Manages state** - Updates CurrentGame, NextGame, PreviousGame
5. **Handles lifecycle** - Pauses, stops, starts games appropriately

## Common Mistakes to Avoid

❌ **DON'T** put UI logic in provider:
```csharp
// WRONG - Provider shouldn't know about UI
public async Task SwipeToNextGameAsync()
{
    await AnimateTransition(); // NO!
    ShowLoadingSpinner(); // NO!
}
```

❌ **DON'T** put business logic in UI:
```csharp
// WRONG - UI shouldn't manage games
class GameSwiper
{
    void OnSwipe()
    {
        QueueService.MoveNext(); // NO!
        CurrentGame.StopGame(); // NO!
    }
}
```

✅ **DO** keep clean separation:
```csharp
// RIGHT - UI fires event
class GameSwiper
{
    void OnSwipe() => OnNextGameRequested?.Invoke();
}

// RIGHT - Controller connects them
class Controller
{
    void HandleNextGameRequested()
    {
        await provider.SwipeToNextGameAsync();
        swiper.UpdateTextures(provider.CurrentGameRenderTexture);
    }
}

// RIGHT - Provider manages games
class Provider
{
    Task<bool> SwipeToNextGameAsync()
    {
        // Only game logic here
    }
}
```

