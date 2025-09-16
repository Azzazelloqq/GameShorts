using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.GamesLoader;
using Code.Core.Tools;
using Code.Generated.Addressables;
using InGameLogger;
using R3;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.GameSwiper
{
    /// <summary>
    /// Enhanced GameSwiperController with queue support for rapid consecutive swipes
    /// </summary>
    public class GameSwiperControllerWithQueue : BaseDisposable
    {
        private readonly IGameProvider _gameProvider;
        private readonly GameSwiperService _swiperService;
        private readonly SwipeQueueManager _queueManager; // NEW: Queue manager
        private readonly IInGameLogger _logger;
        private readonly IResourceLoader _resourceLoader;
        private readonly Ctx _ctx;
        private GameSwiperView _gameSwiperView;
        private readonly ReactiveTrigger _onPreviewGame;
        private readonly ReactiveTrigger _onNextGame;
        private bool _isInitialized;
        private CancellationTokenSource _cancellationTokenSource;

        public struct Ctx
        {
            public Transform PlaceForAllUi;
            public IGameProvider GameProvider;
            public bool EnableQueuedSwipes; // NEW: Option to enable queue
        }

        public GameSwiperControllerWithQueue(
            Ctx ctx, 
            IInGameLogger logger, 
            IResourceLoader resourceLoader)
        {
            _ctx = ctx;
            _gameProvider = ctx.GameProvider ?? throw new ArgumentNullException(nameof(ctx.GameProvider));
            _cancellationTokenSource = new CancellationTokenSource();
            _logger = logger;
            _resourceLoader = resourceLoader;
            
            // Create services
            _swiperService = new GameSwiperService(_gameProvider, _logger);
            _swiperService.OnTransitionStateChanged += HandleTransitionStateChanged;
            _swiperService.OnGameChanged += HandleGameChanged;
            
            // Create queue manager if enabled
            if (ctx.EnableQueuedSwipes)
            {
                _queueManager = new SwipeQueueManager(_swiperService, _logger, maxQueueSize: 3);
                _queueManager.OnQueueSizeChanged += HandleQueueSizeChanged;
                _queueManager.OnSwipeCompleted += HandleSwipeCompleted;
                _logger.Log("Queue support enabled for rapid consecutive swipes");
            }
            
            _onNextGame = new ReactiveTrigger();
            _onPreviewGame = new ReactiveTrigger();
        }
        
        public async ValueTask InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
            {
                _logger.LogWarning("GameSwiperControllerWithQueue is already initialized");
                return;
            }
            
            try
            {
                _logger.Log("Initializing GameSwiperControllerWithQueue");
                
                // Load UI
                await LoadGameSwiperViewAsync(cancellationToken);
                
                // Set up event subscriptions
                AddDispose(_onNextGame.Subscribe(HandleNextGameRequested));
                AddDispose(_onPreviewGame.Subscribe(HandlePreviousGameRequested));
                
                // Update initial state
                UpdateUI();
                
                _isInitialized = true;
                _logger.Log($"GameSwiperControllerWithQueue initialized (Queue: {_ctx.EnableQueuedSwipes})");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to initialize: {ex.Message}");
                throw;
            }
        }
        
        private async void HandleNextGameRequested()
        {
            if (!_isInitialized) return;
            
            if (_ctx.EnableQueuedSwipes && _queueManager != null)
            {
                // Use queue for rapid swipes
                _logger.Log("Queueing next game request");
                var result = await _queueManager.EnqueueSwipeAsync(
                    SwipeQueueManager.SwipeType.Next, 
                    _cancellationTokenSource.Token);
                    
                if (!result)
                {
                    _logger.LogWarning("Failed to queue next game swipe");
                }
            }
            else
            {
                // Original behavior - block if transitioning
                if (!_swiperService.CanSwipeNext)
                {
                    _logger.LogWarning("Cannot swipe - transition in progress or no next game");
                    return;
                }
                
                await ExecuteSwipeAsync(true);
            }
        }
        
        private async void HandlePreviousGameRequested()
        {
            if (!_isInitialized) return;
            
            if (_ctx.EnableQueuedSwipes && _queueManager != null)
            {
                // Use queue for rapid swipes
                _logger.Log("Queueing previous game request");
                var result = await _queueManager.EnqueueSwipeAsync(
                    SwipeQueueManager.SwipeType.Previous, 
                    _cancellationTokenSource.Token);
                    
                if (!result)
                {
                    _logger.LogWarning("Failed to queue previous game swipe");
                }
            }
            else
            {
                // Original behavior - block if transitioning
                if (!_swiperService.CanSwipePrevious)
                {
                    _logger.LogWarning("Cannot swipe - transition in progress or no previous game");
                    return;
                }
                
                await ExecuteSwipeAsync(false);
            }
        }
        
        private async Task ExecuteSwipeAsync(bool isNext)
        {
            try
            {
                // Get textures
                var (current, next, previous) = _swiperService.GetRenderTextures();
                var targetTexture = isNext ? next : previous;
                
                // Start transition
                var transitionTask = isNext ?
                    _swiperService.SwipeToNextGameAsync(_cancellationTokenSource.Token) :
                    _swiperService.SwipeToPreviousGameAsync(_cancellationTokenSource.Token);
                
                // Animate
                if (_gameSwiperView != null && current != null && targetTexture != null)
                {
                    var direction = isNext ? 
                        GameSwiperView.TransitionDirection.Next : 
                        GameSwiperView.TransitionDirection.Previous;
                        
                    await _gameSwiperView.AnimateTransition(current, targetTexture, direction);
                }
                
                // Wait for completion
                await transitionTask;
                
                // Update UI
                UpdateUI();
                
                _logger.Log($"Successfully switched to {(isNext ? "next" : "previous")} game");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during swipe: {ex.Message}");
            }
        }
        
        private void HandleQueueSizeChanged(int size)
        {
            _logger.Log($"Swipe queue size: {size}");
            
            // Update UI to show queue indicator
            if (_gameSwiperView != null)
            {
                // You can add a queue size indicator in the UI
                // _gameSwiperView.SetQueueSize(size);
            }
        }
        
        private void HandleSwipeCompleted(SwipeQueueManager.SwipeType type, bool success)
        {
            if (!success)
            {
                _logger.LogWarning($"Queued {type} swipe failed");
            }
            
            // Update UI after queued swipe completes
            UpdateUI();
        }
        
        private void HandleTransitionStateChanged(GameSwiperService.TransitionState state)
        {
            _logger.Log($"Transition state: {state}");
            
            // Show/hide loading based on state
            bool isLoading = state != GameSwiperService.TransitionState.Idle;
            _gameSwiperView?.SetLoadingState(isLoading);
            
            if (state == GameSwiperService.TransitionState.Idle)
            {
                UpdateUI();
            }
        }
        
        private void HandleGameChanged(Type from, Type to)
        {
            _logger.Log($"Game changed: {from?.Name} -> {to?.Name}");
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            if (_gameSwiperView == null || _gameProvider == null) return;
            
            // Update current texture
            if (_gameProvider.CurrentGameRenderTexture != null)
            {
                _gameSwiperView.SetCurrentGameTexture(_gameProvider.CurrentGameRenderTexture);
            }
            
            // Update preview textures
            _gameSwiperView.SetPreviewTextures(
                _gameProvider.NextGameRenderTexture,
                _gameProvider.PreviousGameRenderTexture);
            
            // Update navigation buttons
            _gameSwiperView.UpdateNavigationButtons(
                _gameProvider.QueueService.HasNext,
                _gameProvider.QueueService.HasPrevious);
        }
        
        private async ValueTask LoadGameSwiperViewAsync(CancellationToken cancellationToken)
        {
            try
            {
                var prefab = await _resourceLoader.LoadResourceAsync<GameObject>(
                    ResourceIdsContainer.DefaultLocalGroup.GameSwiper, 
                    cancellationToken);
                
                if (prefab != null)
                {
                    var disposables = new CompositeDisposable();
                    var instance = AddComponent(Object.Instantiate(prefab, _ctx.PlaceForAllUi));
                    _gameSwiperView = instance.GetComponent<GameSwiperView>();
                    _gameSwiperView.SetCtx(new GameSwiperView.Ctx
                    {
                        Disposables = disposables,
                        OnNextGameRequested = _onNextGame,
                        OnPreviousGameRequested = _onPreviewGame
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load view: {ex.Message}");
            }
        }
        
        protected override void OnDispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            
            if (_swiperService != null)
            {
                _swiperService.OnTransitionStateChanged -= HandleTransitionStateChanged;
                _swiperService.OnGameChanged -= HandleGameChanged;
                _swiperService.Dispose();
            }
            
            if (_queueManager != null)
            {
                _queueManager.OnQueueSizeChanged -= HandleQueueSizeChanged;
                _queueManager.OnSwipeCompleted -= HandleSwipeCompleted;
                _queueManager.Dispose();
            }
            
            base.OnDispose();
        }
    }
}
