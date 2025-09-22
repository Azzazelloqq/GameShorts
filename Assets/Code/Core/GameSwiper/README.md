# GameSwiper - UI Module with Modular Input System

## Architecture

GameSwiper is a pure UI component with a modular input handling system. 
It separates visual presentation from input processing through pluggable handlers.

```
GameSwiper (UI Component)
├── Handles visual carousel (3 RawImages)
├── Delegates input to handlers (modular)
├── Fires events when user wants to switch games
└── Updates display when told by controller

Input Handlers (Pluggable)
├── GameSwiperInputHandler (base contract)
├── SwipeInputHandler (touch/mouse swipes)
└── ButtonInputHandler (UI buttons)

GameSwiperController (Mediator)
├── Creates and manages GameSwiper
├── Listens to GameSwiper events
├── Calls IGameProvider methods
└── Updates GameSwiper display
```

## Input Handler System

### Base Contract (GameSwiperInputHandler)
Abstract base class that all input handlers must extend:
```csharp
public abstract class GameSwiperInputHandler : MonoBehaviour
{
    // Events for game navigation
    public event Action OnNextGameRequested;
    public event Action OnPreviousGameRequested;
    
    // Event for real-time interaction progress
    public event Action<float> OnDragProgress;  // -1 to 1, negative=previous, positive=next
    
    // Enable/disable the handler
    public abstract bool IsEnabled { get; set; }
    
    // Update navigation availability
    public abstract void SetNavigationAvailability(bool canGoNext, bool canGoPrevious);
    
    // Reset any ongoing input state
    public abstract void ResetInputState();
}
```

### SwipeInputHandler
Handles vertical swipe gestures with visual feedback:
- **Swipe Up**: Request next game (like TikTok/YouTube Shorts)
- **Swipe Down**: Request previous game
- **Drag Preview**: Real-time visual feedback during drag
- **Rubber Band**: Elastic effect at boundaries
- **Configurable**: Threshold, sensitivity, invert option

### ButtonInputHandler  
Manages UI button navigation:
- **Next Button**: Navigate to next game
- **Previous Button**: Navigate to previous game
- **Auto-disable**: Grays out unavailable directions
- **Auto-hide**: Optionally hides unavailable buttons
- **Quick Progress**: Sends instant OnDragProgress (1 or -1) on button click

## Separation of Concerns

### GameSwiper (Pure UI)
- **Responsibility**: Visual presentation and input coordination
- **Does**: Animation, delegates to input handlers, fires events
- **Does NOT**: Game logic, queue management, loading, direct input handling
- **Communication**: Events only (OnNextGameRequested, OnPreviousGameRequested)

### Input Handlers (Modular Input)
- **Responsibility**: Process specific input types
- **Does**: Detect user input, validate actions, fire navigation events
- **Does NOT**: Animate UI, manage games, know about business logic
- **Communication**: Events to GameSwiper

### GameSwiperController (Mediator)
- **Responsibility**: Connect UI with business logic
- **Does**: Subscribe to UI events, call provider methods, update UI state, manage loading
- **Loading Management**: Shows loading indicator while waiting for games to be ready
- **Does NOT**: Direct game management or input processing
- **Communication**: Uses IGameProvider interface

### IGameProvider (Business Logic)
- **Responsibility**: Game management and queue logic
- **Does**: Switch games, manage queue, handle loading
- **Does NOT**: Know about UI, swipes, or visual presentation
- **Communication**: Provides game state and textures

## Data Flow

```
User interacts (swipe/button/keyboard/etc)
    ↓
InputHandler detects input
    ↓
InputHandler.OnDragProgress event (visual feedback)
    ↓
GameSwiper updates visual preview
    ↓
InputHandler.OnNextGameRequested/OnPreviousGameRequested event
    ↓
GameSwiper receives event from handler
    ↓
GameSwiper.OnNextGameRequested/OnPreviousGameRequested event
    ↓
GameSwiperController handles event
    ↓
Controller checks if game is ready (IsNextGameReady/IsPreviousGameReady)
    ↓
If not ready: Show loading indicator and wait
    ↓
Once ready: Animate transition
    ↓
Controller calls gameProvider.SwipeToNextGameAsync()
    ↓
Provider switches game (business logic)
    ↓
Controller updates GameSwiper textures
    ↓
GameSwiper displays new state
```

## Unity Setup

### GameSwiper Prefab Structure:
```
GameSwiperPrefab
├── GameSwiper (Component)
│   ├── Input Handlers (List)
│   │   ├── SwipeInputHandler
│   │   └── ButtonInputHandler
│   └── Settings
├── Visual Elements
│   ├── TopImage (RawImage, Y: 1080)
│   ├── CenterImage (RawImage, Y: 0)
│   └── BottomImage (RawImage, Y: -1080)
├── Input Components
│   ├── SwipeInputHandler (Component)
│   └── ButtonInputHandler (Component)
│       ├── NextButton (Button reference)
│       └── PreviousButton (Button reference)
└── UI Elements
    ├── NextButton (Button)
    ├── PreviousButton (Button)
    └── LoadingIndicator (GameObject)
```

### Configuration

**GameSwiper Settings:**
- Animation Duration: 0.3s
- Animation Ease: OutQuad
- Image Spacing: 1080
- Input Handlers: [List of handlers]

**SwipeInputHandler Settings:**
- Swipe Threshold: 100
- Drag Sensitivity: 1.0
- Invert Swipe: false
- Max Rubber Band: 100
- Rubber Band Resistance: 0.5

**ButtonInputHandler Settings:**
- Next Button: (Button reference)
- Previous Button: (Button reference)
- Auto Hide Unavailable: true
- Auto Disable Unavailable: true

## Adding Custom Input Handlers

Create a new handler by extending the base class:

```csharp
public class KeyboardInputHandler : GameSwiperInputHandler
{
    [SerializeField] private KeyCode _nextKey = KeyCode.N;
    [SerializeField] private KeyCode _previousKey = KeyCode.P;
    
    private void Update()
    {
        if (!IsEnabled) return;
        
        if (Input.GetKeyDown(_nextKey))
        {
            ReportDragProgress(1f);  // Instant full progress
            RequestNextGame();
            ReportDragProgress(0f);  // Reset
        }
            
        if (Input.GetKeyDown(_previousKey))
        {
            ReportDragProgress(-1f); // Instant full progress
            RequestPreviousGame();
            ReportDragProgress(0f);  // Reset
        }
    }
    
    // Implement abstract members...
}
```

Then add it to GameSwiper's input handlers list in the Inspector.

## API Reference

### GameSwiper Public Methods:
- `UpdateTextures(prev, curr, next)` - Update all displays
- `UpdateNavigationStates(canNext, canPrev)` - Update all input handlers
- `SetLoadingState(bool)` - Show/hide loading
- `AnimateToNext()` - Play next animation
- `AnimateToPrevious()` - Play previous animation
- `AddInputHandler(handler)` - Add handler at runtime
- `RemoveInputHandler(handler)` - Remove handler at runtime

### GameSwiper Events:
- `OnNextGameRequested` - User wants next game
- `OnPreviousGameRequested` - User wants previous game

## Loading Management

The system automatically handles game loading states:

- **Initial Load**: Waits for current game to be ready on startup
- **Pre-transition Check**: Checks if next/previous game is ready before switching
- **Loading Indicator**: Shows loading UI while waiting for games
- **Timeout Protection**: 10-second timeout prevents infinite waiting
- **Automatic Updates**: Loading state updates when game readiness changes

## Benefits

- **Unified API**: All input handlers share the same interface
- **Consistent Behavior**: Visual feedback works the same for all input types
- **Smart Loading**: Automatic loading management with visual feedback
- **Modular Input**: Easy to add/remove input methods
- **Testable**: Each handler can be tested independently
- **Flexible**: Mix and match input methods per platform
- **Maintainable**: Clear separation of concerns
- **Extensible**: Add custom input handlers without modifying core
- **Reusable**: Input handlers can be reused in other projects

## Example: Multiple Input Methods

```csharp
// Setup GameSwiper with multiple input handlers
gameSwiper.AddInputHandler(swipeHandler);    // Touch/mouse swipes
gameSwiper.AddInputHandler(buttonHandler);   // UI buttons
gameSwiper.AddInputHandler(keyboardHandler); // Keyboard shortcuts
gameSwiper.AddInputHandler(gamepadHandler);  // Gamepad support

// Enable/disable specific input methods
swipeHandler.IsEnabled = Application.isMobilePlatform;
keyboardHandler.IsEnabled = Application.isEditor;
```