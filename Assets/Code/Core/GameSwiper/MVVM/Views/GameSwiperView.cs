using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Code.Core.GameSwiper.InputHandlers;
using Code.Core.GameSwiper.MVVM.ViewModels;
using DG.Tweening;
using UnityEngine;
using Azzazelloqq.MVVM.ReactiveLibrary.Callbacks;

namespace Code.Core.GameSwiper.MVVM.Views
{
    /// <summary>
    /// Main View for the GameSwiper.
    /// Manages the carousel of game views and handles input/animations.
    /// </summary>
    public class GameSwiperView : ViewMonoBehavior<GameSwiperViewModel>
    {
        [Header("Game View Containers")]
        [SerializeField]
        private Transform _topContainer; // Previous game
        
        [SerializeField]
        private Transform _centerContainer; // Current game
        
        [SerializeField]
        private Transform _bottomContainer; // Next game
        
        [Header("Game View Prefab")]
        [SerializeField]
        private GameItemView _gameItemViewPrefab;
        
        [Header("Input Handlers")]
        [SerializeField]
        private List<GameSwiperInputHandler> _inputHandlers = new();
        
        [Header("Loading")]
        [SerializeField]
        private GameObject _globalLoadingIndicator;
        
        [Header("Animation Settings")]
        [SerializeField]
        private float _animationDuration = 0.3f;
        
        [SerializeField]
        private Ease _animationEase = Ease.OutQuad;
        
        [SerializeField]
        private bool _useScreenHeight = true;
        
        [SerializeField]
        private float _imageSpacing = 1920f;
        
        private float ActualImageSpacing => _useScreenHeight ? Screen.height : _imageSpacing;
        
        // Active game views
        private GameItemView _topGameView;
        private GameItemView _centerGameView;
        private GameItemView _bottomGameView;
        
        // Pool of game views for reuse
        private readonly Queue<GameItemView> _viewPool = new();
        private readonly Dictionary<int, GameItemView> _activeViews = new();
        
        private bool _isAnimating;
        private bool _isInitialized;

        protected override void OnInitialize()
        {
            if (_isInitialized)
                return;
                
            _isInitialized = true;
            
            // Setup initial positions
            SetupInitialPositions();
            
            // Setup input handlers
            SetupInputHandlers();
            
            // Bind to ViewModel
            BindToViewModel();
            
            // Hide global loading initially
            if (_globalLoadingIndicator != null)
            {
                _globalLoadingIndicator.SetActive(false);
            }
        }

        protected override async ValueTask OnInitializeAsync(CancellationToken token)
        {
            // Wait for ViewModel to initialize
            await Task.Yield();
            
            // Create initial game views after ViewModel is ready
            UpdateGameViews();
        }

        private void SetupInitialPositions()
        {
            if (_topContainer)
            {
                _topContainer.localPosition = new Vector3(0, ActualImageSpacing, 0);
            }
            
            if (_centerContainer)
            {
                _centerContainer.localPosition = Vector3.zero;
            }
            
            if (_bottomContainer)
            {
                _bottomContainer.localPosition = new Vector3(0, -ActualImageSpacing, 0);
            }
        }

        private void SetupInputHandlers()
        {
            foreach (var handler in _inputHandlers)
            {
                if (handler != null)
                {
                    handler.OnNextGameRequested += HandleNextGameRequest;
                    handler.OnPreviousGameRequested += HandlePreviousGameRequest;
                    handler.OnDragProgress += HandleSwipeProgress;
                }
            }
        }

        private void BindToViewModel()
        {
            // Bind loading state
            compositeDisposable.AddDisposable(viewModel.IsLoading.Subscribe(OnLoadingStateChanged));
            
            // Bind transition state
            compositeDisposable.AddDisposable(viewModel.IsTransitioning.Subscribe(OnTransitioningChanged));
            
            // Bind navigation availability
            compositeDisposable.AddDisposable(viewModel.CanGoNext.Subscribe(_ => UpdateNavigationStates()));
            compositeDisposable.AddDisposable(viewModel.CanGoPrevious.Subscribe(_ => UpdateNavigationStates()));
            
            // Bind game ViewModels - subscribe to collection changes
            compositeDisposable.AddDisposable(viewModel.GameViewModels.SubscribeOnItemAdded(_ => UpdateGameViews()));
            compositeDisposable.AddDisposable(viewModel.GameViewModels.SubscribeOnRemoved(_ => UpdateGameViews()));
            compositeDisposable.AddDisposable(viewModel.GameViewModels.SubscribeOnCleared(UpdateGameViews));
            
            // Bind swipe progress
            compositeDisposable.AddDisposable(viewModel.SwipeProgress.Subscribe(OnSwipeProgressChanged));
            
            // Subscribe to navigation events
            viewModel.OnNextGameStarted += OnNextGameStarted;
            viewModel.OnPreviousGameStarted += OnPreviousGameStarted;
        }

        private void UpdateGameViews()
        {
            // Clear current views
            ClearActiveViews();
            
            // Get ViewModels for visible games
            var previousVM = viewModel.PreviousGameViewModel.Value;
            var currentVM = viewModel.CurrentGameViewModel.Value;
            var nextVM = viewModel.NextGameViewModel.Value;
            
            // Create/assign views for each position
            if (previousVM != null)
            {
                _topGameView = GetOrCreateView(previousVM);
                SetupGameView(_topGameView, _topContainer);
            }
            
            if (currentVM != null)
            {
                _centerGameView = GetOrCreateView(currentVM);
                SetupGameView(_centerGameView, _centerContainer);
            }
            
            if (nextVM != null)
            {
                _bottomGameView = GetOrCreateView(nextVM);
                SetupGameView(_bottomGameView, _bottomContainer);
            }
        }

        private GameItemView GetOrCreateView(GameItemViewModel gameViewModel)
        {
            // Check if view already exists for this ViewModel
            if (_activeViews.TryGetValue(gameViewModel.GameIndex, out var existingView))
            {
                return existingView;
            }
            
            // Try to get from pool or create new
            GameItemView view;
            if (_viewPool.Count > 0)
            {
                view = _viewPool.Dequeue();
            }
            else
            {
                view = Instantiate(_gameItemViewPrefab);
            }
            
            // Initialize with ViewModel
            view.Initialize(gameViewModel);
            _activeViews[gameViewModel.GameIndex] = view;
            
            return view;
        }

        private void SetupGameView(GameItemView view, Transform container)
        {
            if (view != null && container != null)
            {
                view.transform.SetParent(container, false);
                view.transform.localPosition = Vector3.zero;
                view.transform.localScale = Vector3.one;
                view.gameObject.SetActive(true);
            }
        }

        private void ClearActiveViews()
        {
            // Move all active views back to pool
            foreach (var view in _activeViews.Values)
            {
                if (view != null)
                {
                    view.gameObject.SetActive(false);
                    view.transform.SetParent(transform, false);
                    _viewPool.Enqueue(view);
                }
            }
            
            _activeViews.Clear();
            _topGameView = null;
            _centerGameView = null;
            _bottomGameView = null;
        }

        private void HandleNextGameRequest()
        {
            if (_isAnimating || !viewModel.CanGoNext.Value)
                return;
                
            _ = viewModel.GoToNextCommand.ExecuteAsync();
        }

        private void HandlePreviousGameRequest()
        {
            if (_isAnimating || !viewModel.CanGoPrevious.Value)
                return;
                
            _ = viewModel.GoToPreviousCommand.ExecuteAsync();
        }

        private void HandleSwipeProgress(float progress)
        {
            if (_isAnimating)
                return;
                
            viewModel.UpdateSwipeProgressCommand.Execute(progress);
        }

        private void OnSwipeProgressChanged(float progress)
        {
            if (_isAnimating)
                return;
                
            // Visual feedback during swipe
            float offset = -progress * ActualImageSpacing * 0.3f;
            
            if (_topContainer)
            {
                _topContainer.localPosition = new Vector3(0, ActualImageSpacing - offset, 0);
            }
            
            if (_centerContainer)
            {
                _centerContainer.localPosition = new Vector3(0, -offset, 0);
            }
            
            if (_bottomContainer)
            {
                _bottomContainer.localPosition = new Vector3(0, -ActualImageSpacing - offset, 0);
            }
            
            // Reset positions if progress returns to 0
            if (Mathf.Approximately(progress, 0f))
            {
                ResetPositions();
            }
        }

        private void OnLoadingStateChanged(bool isLoading)
        {
            if (_globalLoadingIndicator != null)
            {
                _globalLoadingIndicator.SetActive(isLoading);
            }
        }

        private void OnTransitioningChanged(bool isTransitioning)
        {
            _isAnimating = isTransitioning;
            UpdateInputHandlersEnabled(!isTransitioning);
        }

        private void UpdateNavigationStates()
        {
            bool canGoNext = viewModel.CanGoNext.Value && !_isAnimating;
            bool canGoPrevious = viewModel.CanGoPrevious.Value && !_isAnimating;
            
            foreach (var handler in _inputHandlers)
            {
                if (handler != null)
                {
                    handler.SetNavigationAvailability(canGoNext, canGoPrevious);
                }
            }
        }

        private void UpdateInputHandlersEnabled(bool enabled)
        {
            foreach (var handler in _inputHandlers)
            {
                if (handler != null)
                {
                    handler.IsEnabled = enabled;
                    if (!enabled)
                    {
                        handler.ResetInputState();
                    }
                }
            }
        }

        private void OnNextGameStarted()
        {
            _ = AnimateToNext();
        }

        private void OnPreviousGameStarted()
        {
            _ = AnimateToPrevious();
        }

        private async Task AnimateToNext()
        {
            _isAnimating = true;
            UpdateInputHandlersEnabled(false);
            
            var sequence = DOTween.Sequence();
            
            if (_topContainer)
                sequence.Append(_topContainer.DOLocalMove(new Vector3(0, ActualImageSpacing * 2, 0), _animationDuration).SetEase(_animationEase));
            
            if (_centerContainer)
                sequence.Join(_centerContainer.DOLocalMove(new Vector3(0, ActualImageSpacing, 0), _animationDuration).SetEase(_animationEase));
            
            if (_bottomContainer)
                sequence.Join(_bottomContainer.DOLocalMove(Vector3.zero, _animationDuration).SetEase(_animationEase));
            
            await sequence.AsyncWaitForCompletion();
            
            // Reset positions and update views
            ResetPositions();
            UpdateGameViews();
            
            _isAnimating = false;
            UpdateInputHandlersEnabled(true);
        }

        private async Task AnimateToPrevious()
        {
            _isAnimating = true;
            UpdateInputHandlersEnabled(false);
            
            var sequence = DOTween.Sequence();
            
            if (_topContainer)
                sequence.Append(_topContainer.DOLocalMove(Vector3.zero, _animationDuration).SetEase(_animationEase));
            
            if (_centerContainer)
                sequence.Join(_centerContainer.DOLocalMove(new Vector3(0, -ActualImageSpacing, 0), _animationDuration).SetEase(_animationEase));
            
            if (_bottomContainer)
                sequence.Join(_bottomContainer.DOLocalMove(new Vector3(0, -ActualImageSpacing * 2, 0), _animationDuration).SetEase(_animationEase));
            
            await sequence.AsyncWaitForCompletion();
            
            // Reset positions and update views
            ResetPositions();
            UpdateGameViews();
            
            _isAnimating = false;
            UpdateInputHandlersEnabled(true);
        }

        private void ResetPositions()
        {
            if (_topContainer)
            {
                _topContainer.DOLocalMove(new Vector3(0, ActualImageSpacing, 0), 0.2f);
            }
            
            if (_centerContainer)
            {
                _centerContainer.DOLocalMove(Vector3.zero, 0.2f);
            }
            
            if (_bottomContainer)
            {
                _bottomContainer.DOLocalMove(new Vector3(0, -ActualImageSpacing, 0), 0.2f);
            }
        }

        protected override void OnDispose()
        {
            // Unsubscribe from input handlers
            foreach (var handler in _inputHandlers)
            {
                if (handler != null)
                {
                    handler.OnNextGameRequested -= HandleNextGameRequest;
                    handler.OnPreviousGameRequested -= HandlePreviousGameRequest;
                    handler.OnDragProgress -= HandleSwipeProgress;
                }
            }
            
            // Unsubscribe from ViewModel events
            if (viewModel != null)
            {
                viewModel.OnNextGameStarted -= OnNextGameStarted;
                viewModel.OnPreviousGameStarted -= OnPreviousGameStarted;
            }
            
            // Clear views
            ClearActiveViews();
            
            // Destroy pooled views
            while (_viewPool.Count > 0)
            {
                var view = _viewPool.Dequeue();
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}
