# GameSwiper Integration - MVC Architecture

## Overview

GameSwiper is a system for managing switching between mini-games, built using the MVC pattern. The system ensures clear separation of responsibilities between components.

**Key Features:**
- **TikTok-style swipe navigation** with real-time preview
- **Interactive gestures** - change direction mid-swipe or cancel
- **RenderTexture-based transitions** for smooth animations
- **Async initialization pattern** to avoid constructor deadlocks
- **Full MVC architecture** with clear separation of concerns

## MVC Architecture

### Model - GameSwiper
**Business logic** for game switching:
- Implements `ISwiperGame` interface
- Uses `IShortGameLifeCycleService` to manage games
- Handles async loading operations
- Logs all operations

### View - GameSwiperView
**Pure View** without business logic:
- Contains only UI elements and input handling
- Generates reactive events: `OnNextGameRequested`, `OnPreviousGameRequested`
- Supports various input methods (buttons, swipes, keyboard)
- Manages visual state (loading, button activity)

### Controller - GameSwiperController
**Bridge** between Model and View:
- Subscribes to View events
- Calls Model methods
- Manages View state (loading indicator)
- Handles errors and operation cancellations

## System Components

### 1. InteractiveSwipeHandler (New!)
**Advanced swipe gesture handling** with TikTok-like behavior:
- **Direction Change Support**: Can start swiping down, then change to up mid-gesture
- **Cancellation Support**: Release before threshold to cancel swipe
- **Visual Feedback**: Real-time preview of next/previous games during swipe
- **Rubber Band Effect**: Elastic feedback when swiping to unavailable game
- **Progress Tracking**: Events for swipe progress (0-100%)

Key features:
- `OnSwipeProgress` - Tracks swipe direction and progress in real-time
- `OnSwipeComplete` - Triggered when swipe passes threshold
- `OnSwipeCancelled` - Triggered when user releases before threshold
- Supports both up (previous) and down (next) swipe directions
- Configurable thresholds, deadzones, and visual feedback

### 2. ISwiperGame (Interface)
```csharp
public interface ISwiperGame
{
    Task<bool> SwipeToNextGameAsync(CancellationToken cancellationToken = default);
    Task<bool> SwipeToPreviousGameAsync(CancellationToken cancellationToken = default);
    (RenderTexture current, RenderTexture next, RenderTexture previous) GetRenderTextures();
    bool CanSwipeNext { get; }
    bool CanSwipePrevious { get; }
}
```

### 3. GameSwiper (Model)
- Core business logic
- Integration with `GameSwiperService`
- Wraps service functionality for interface implementation
- Manages async operations

### 4. GameSwiperView (View)
**Reactive events:**
- `OnNextGameRequested` - request next game
- `OnPreviousGameRequested` - request previous game
- `OnGameChanged` - game change notification

**State management methods:**
- `SetLoadingState(bool)` - show/hide loading indicator
- `SetPreviewTextures(next, previous)` - set textures for swipe preview
- `UpdateNavigationButtons(hasNext, hasPrevious)` - update button states
- **NEW**: Interactive swipe integration with `InteractiveSwipeHandler`

**Interactive Swipe Features:**
- Real-time preview during swipe
- Support for direction changes mid-gesture
- Cancellation when released before threshold
- Visual feedback with rubber band effect

### 5. GameSwiperController (Controller)
- Manages connection between Model and View
- Handles View events (buttons and swipe gestures)
- Updates preview textures for next/previous games
- Coordinates with GameProvider for game transitions
- Manages async initialization pattern

### 6. GameSwiperService
**Core service** for game transition orchestration:
- Manages transition states (Idle, Preparing, Animating, Completing)
- Handles RenderTexture coordination
- Synchronizes game lifecycle (pause, stop, start)
- Manages preloading of adjacent games
- Provides swipe availability checks

### 7. GameSwiperFactory
Factory for creating GameSwiper with dependencies:
```csharp
public static GameSwiper CreateGameSwiper(
    IShortGameLifeCycleService lifeCycleService, 
    IInGameLogger logger)
```

## Data Flow

```
User Input (View) 
    → Event (OnNextGameRequested)
    → Controller (HandleNextGameRequested)
    → Model (NextGameAsync)
    → Controller (HandleResponse)
    → View (SetLoadingState, OnGameChanged)
```

## GameEntryPoint Integration

### ⚠️ IMPORTANT: Async Initialization Pattern

**Never call async methods in constructors!** This can lead to deadlocks and unpredictable behavior.

### Correct initialization approach:

```csharp
// 1. Create components WITHOUT async calls
var gameProvider = new GameProvider(logger);
var controller = new GameSwiperController(ctx, logger, resourceLoader);

// 2. Async initialization through ValueTask methods
await gameProvider.InitializeAsync(registry, queue, loader, cancellationToken);
await controller.InitializeAsync(cancellationToken);

// 3. Now components are ready to use
```

### Integration steps:

1. **Create GameProvider** - `new GameProvider(logger)`
2. **Initialize Provider** - `await gameProvider.InitializeAsync(...)`
3. **Create Controller** - `new GameSwiperController(ctx, logger, resourceLoader)`  
4. **Initialize Controller** - `await controller.InitializeAsync(cancellationToken)`
5. **View loads automatically** - in controller's `InitializeAsync` method

## Configuration

### GameSwiperView settings:
- `_swipeThreshold` - swipe sensitivity (50px)
- `_enableSwipeGestures` - enable swipe gestures
- `_enableKeyboardInput` - enable keyboard input
- `_loadingIndicator` - loading indicator

### GameEntryPoint settings:
- `_uiParent` - parent object for UI
- `_gameSwiperViewPrefabPath` - path to prefab

## Rapid Consecutive Swipes Support

### Current Limitation (Default Behavior)
⚠️ **By default, rapid consecutive swipes are BLOCKED**:
- Only one transition can happen at a time
- Additional swipes during transition are ignored
- User must wait for current transition to complete

### SwipeQueueManager (Optional Enhancement)
To support TikTok-style rapid swipes with queuing:

```csharp
// Create queue manager with the swiper service
var queueManager = new SwipeQueueManager(_swiperService, _logger, maxQueueSize: 3);

// Enqueue swipes (they will be processed sequentially)
await queueManager.EnqueueSwipeAsync(SwipeQueueManager.SwipeType.Next);
await queueManager.EnqueueSwipeAsync(SwipeQueueManager.SwipeType.Next);
await queueManager.EnqueueSwipeAsync(SwipeQueueManager.SwipeType.Previous);

// Monitor queue
queueManager.OnQueueSizeChanged += (size) => {
    _logger.Log($"Queue size: {size}");
};

// Get queue info
string info = queueManager.GetQueueInfo(); // "Queue (3): Next -> Next -> Previous"
```

**Features:**
- **Queue Size Limit**: Max 3 swipes queued (configurable)
- **Automatic Processing**: Swipes execute sequentially
- **Timeout Protection**: Old requests expire after 10s
- **Visual Feedback**: Shows queue size indicator
- **Cancellable**: Can clear queue anytime

**Benefits:**
- ✅ User can swipe multiple times rapidly
- ✅ All swipes are processed in order
- ✅ No swipes are lost
- ✅ Prevents memory issues with queue limit

## Interaction Methods

### 1. UI Buttons
- "Next Game" / "Previous Game"
- Automatically disabled during loading

### 2. Swipe Gestures (Enhanced with InteractiveSwipeHandler)
- **Swipe up** → previous game
- **Swipe down** → next game  
- **Interactive Features:**
  - Start swiping, see preview of target game
  - Change direction mid-swipe (e.g., start down, go back up)
  - Release before threshold to cancel
  - Rubber band effect at boundaries
  - Real-time visual feedback
- **Configurable:**
  - Swipe threshold (default 50px)
  - Dead zone (default 10px)
  - Maximum swipe time
  - Preview scale and animations

### 3. Keyboard Input (Editor)
- ↑ / ↓ - navigate between games
- Unity editor only

### 4. Programmatic Control
```csharp
var gameSwiper = container.GetInstance<ISwiperGame>();
await gameSwiper.NextGameAsync();
```

## Testing

**GameSwiperTester** for system verification:
- N/P keys - navigation testing
- C key - controller verification
- Automatic connection on initialization

## Architecture Advantages

1. **Separation of Concerns** - clear boundaries between components
2. **Testability** - each component can be tested in isolation
3. **Extensibility** - easy to add new input methods or logic
4. **Reusability** - View can be used with different controllers
5. **Clean Code** - View contains only UI logic, Model contains only business logic

## File Structure

```
Assets/Code/Core/GameSwiper/
├── ISwiperGame.cs              # Interface
├── GameSwiper.cs               # Model (business logic)
├── GameSwiperView.cs           # View (pure UI)
├── GameSwiperController.cs     # Controller (bridge)
├── GameSwiperService.cs        # Service (transition orchestration)
├── InteractiveSwipeHandler.cs  # Interactive swipe gestures (TikTok-style)
├── SwipeQueueManager.cs        # Queue system for rapid consecutive swipes (NEW!)
└── README.md                   # Documentation
```

## Lifecycle Management

1. **Creation**: GameEntryPoint creates components (constructors)
2. **Initialization**: Call `InitializeAsync()` methods for async operations
3. **Binding**: Controller connects to View (in `InitializeAsync`)
4. **Operation**: Events View → Controller → Model → Controller → View
5. **Cleanup**: Controller unsubscribes from View, releases resources

### Async Best Practices:

- **Use ValueTask** for initialization methods (more efficient for sync operations)
- **Check initialization state** via `_isInitialized` flag
- **Support CancellationToken** in all async operations
- **Prevent double initialization** - check flag at method start
