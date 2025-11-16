using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Azzazelloqq.MVVM.ReactiveLibrary.Collections;
using Code.Core.GameSwiper.MVVM.Models;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.ViewModels
{
    /// <summary>
    /// Main ViewModel for the GameSwiper.
    /// Manages the collection of game ViewModels and provides navigation commands.
    /// </summary>
    public class GameSwiperViewModel : ViewModelBase<GameSwiperModel>
    {
        // Expose model properties
        public IReadOnlyReactiveProperty<int> CurrentGameIndex => model.CurrentGameIndex;
        public IReadOnlyReactiveProperty<bool> CanGoNext => model.CanGoNext;
        public IReadOnlyReactiveProperty<bool> CanGoPrevious => model.CanGoPrevious;
        public IReadOnlyReactiveProperty<bool> IsTransitioning => model.IsTransitioning;
        public IReadOnlyReactiveProperty<bool> IsLoading => model.IsLoading;
        
        // View-specific properties
        public IReactiveList<GameItemViewModel> GameViewModels { get; }
        public IReactiveProperty<float> SwipeProgress { get; }
        public IReactiveProperty<bool> IsEnabled { get; }
        public IReactiveProperty<SwipeDirection> LastSwipeDirection { get; }
        
        // Current visible game ViewModels
        public IReadOnlyReactiveProperty<GameItemViewModel> PreviousGameViewModel { get; }
        public IReadOnlyReactiveProperty<GameItemViewModel> CurrentGameViewModel { get; }
        public IReadOnlyReactiveProperty<GameItemViewModel> NextGameViewModel { get; }
        
        // Commands
        public IActionAsyncCommand GoToNextCommand { get; private set; }
        public IActionAsyncCommand GoToPreviousCommand { get; private set; }
        public IActionCommand ResetSwipeCommand { get; private set; }
        public IRelayCommand<float> UpdateSwipeProgressCommand { get; private set; }
        
        // Events
        public event Action OnNextGameStarted;
        public event Action OnPreviousGameStarted;
        public event Action<float> OnSwipeProgressChanged;
        
        private readonly ReactiveProperty<GameItemViewModel> _previousGameVM;
        private readonly ReactiveProperty<GameItemViewModel> _currentGameVM;
        private readonly ReactiveProperty<GameItemViewModel> _nextGameVM;
        private readonly Dictionary<int, GameItemViewModel> _gameViewModelCache;
        private CancellationTokenSource _navigationCts;

        public GameSwiperViewModel(GameSwiperModel model) : base(model)
        {
            GameViewModels = new ReactiveList<GameItemViewModel>();
            SwipeProgress = new ReactiveProperty<float>(0f);
            IsEnabled = new ReactiveProperty<bool>(true);
            LastSwipeDirection = new ReactiveProperty<SwipeDirection>(SwipeDirection.None);
            
            _previousGameVM = new ReactiveProperty<GameItemViewModel>(null);
            _currentGameVM = new ReactiveProperty<GameItemViewModel>(null);
            _nextGameVM = new ReactiveProperty<GameItemViewModel>(null);
            
            PreviousGameViewModel = _previousGameVM;
            CurrentGameViewModel = _currentGameVM;
            NextGameViewModel = _nextGameVM;
            
            _gameViewModelCache = new Dictionary<int, GameItemViewModel>();
            _navigationCts = new CancellationTokenSource();
            
            // Add to composite disposable
            compositeDisposable.AddDisposable(GameViewModels);
            compositeDisposable.AddDisposable(SwipeProgress);
            compositeDisposable.AddDisposable(IsEnabled);
            compositeDisposable.AddDisposable(LastSwipeDirection);
            compositeDisposable.AddDisposable(_previousGameVM);
            compositeDisposable.AddDisposable(_currentGameVM);
            compositeDisposable.AddDisposable(_nextGameVM);
        }

        protected override void OnInitialize()
        {
            // Initialize commands
            GoToNextCommand = new ActionAsyncCommand(OnGoToNext, CanGoToNext);
            GoToPreviousCommand = new ActionAsyncCommand(OnGoToPrevious, CanGoToPrevious);
            ResetSwipeCommand = new ActionCommand(OnResetSwipe);
            UpdateSwipeProgressCommand = new RelayCommand<float>(OnUpdateSwipeProgress);
            
            // Add commands to composite disposable
            compositeDisposable.AddDisposable(GoToNextCommand);
            compositeDisposable.AddDisposable(GoToPreviousCommand);
            compositeDisposable.AddDisposable(ResetSwipeCommand);
            compositeDisposable.AddDisposable(UpdateSwipeProgressCommand);
            
            // Subscribe to model changes
            // For IReactiveList, we need to subscribe to collection change events
            compositeDisposable.AddDisposable(model.Games.SubscribeOnAdded(OnGameAdded));
            compositeDisposable.AddDisposable(model.Games.SubscribeOnRemoved(OnGameRemoved));
            compositeDisposable.AddDisposable(model.Games.SubscribeOnCleared(OnGamesCleared));
            
            // Subscribe to properties
            compositeDisposable.AddDisposable(model.CurrentGameIndex.Subscribe(OnCurrentIndexChanged));
            compositeDisposable.AddDisposable(model.IsTransitioning.Subscribe(OnTransitioningChanged));
            
            // Initialize view models from model
            UpdateGameViewModels();
        }

        protected override async ValueTask OnInitializeAsync(CancellationToken token)
        {
            // Initialize games in the model
            await model.InitializeGamesAsync(token);
            
            // Update view models after initialization
            UpdateGameViewModels();
        }

        /// <summary>
        /// Update game view models based on model changes
        /// </summary>
        private void UpdateGameViewModels()
        {
            GameViewModels.Clear();
            
            foreach (var gameModel in model.Games)
            {
                var viewModel = GetOrCreateGameViewModel(gameModel);
                GameViewModels.Add(viewModel);
            }
            
            // Update current visible game references
            UpdateVisibleGameReferences();
        }

        /// <summary>
        /// Update references to visible game ViewModels
        /// </summary>
        private void UpdateVisibleGameReferences()
        {
            var currentIndex = model.CurrentGameIndex.Value;
            
            // Find ViewModels by index
            _previousGameVM.SetValue(GameViewModels.FirstOrDefault(vm => vm.GameIndex == currentIndex - 1));
            _currentGameVM.SetValue(GameViewModels.FirstOrDefault(vm => vm.GameIndex == currentIndex));
            _nextGameVM.SetValue(GameViewModels.FirstOrDefault(vm => vm.GameIndex == currentIndex + 1));
            
            // Update UI visibility for each ViewModel
            foreach (var vm in GameViewModels)
            {
                bool isVisible = Math.Abs(vm.GameIndex - currentIndex) <= 1;
                float opacity = vm.GameIndex == currentIndex ? 1f : 0.7f;
                vm.UpdateUIVisibility(isVisible, opacity);
            }
        }

        /// <summary>
        /// Get or create a ViewModel for a game model
        /// </summary>
        private GameItemViewModel GetOrCreateGameViewModel(GameItemModel gameModel)
        {
            if (!_gameViewModelCache.ContainsKey(gameModel.Index))
            {
                var viewModel = new GameItemViewModel(gameModel);
                viewModel.Initialize();
                
                // Subscribe to game-specific events
                viewModel.OnPlayRequested += OnGamePlayRequested;
                viewModel.OnPauseRequested += OnGamePauseRequested;
                viewModel.OnRestartRequested += OnGameRestartRequested;
                
                _gameViewModelCache[gameModel.Index] = viewModel;
                compositeDisposable.AddDisposable(viewModel);
            }
            
            return _gameViewModelCache[gameModel.Index];
        }

        private void OnGameAdded(GameItemModel gameModel)
        {
            UpdateGameViewModels();
        }
        
        private void OnGameRemoved(GameItemModel gameModel)
        {
            UpdateGameViewModels();
        }
        
        private void OnGamesCleared()
        {
            UpdateGameViewModels();
        }

        private void OnCurrentIndexChanged(int index)
        {
            UpdateVisibleGameReferences();
        }

        private void OnTransitioningChanged(bool isTransitioning)
        {
            // Update enabled state based on transitioning
            IsEnabled.SetValue(!isTransitioning);
        }

        private bool CanGoToNext()
        {
            return model.CanGoNext.Value && !model.IsTransitioning.Value;
        }

        private async Task OnGoToNext()
        {
            LastSwipeDirection.SetValue(SwipeDirection.Up);
            
            bool success = await model.GoToNextGameAsync(_navigationCts.Token);
            
            if (success)
            {
                OnNextGameStarted?.Invoke();
                ResetSwipeProgress();
            }
        }

        private bool CanGoToPrevious()
        {
            return model.CanGoPrevious.Value && !model.IsTransitioning.Value;
        }

        private async Task OnGoToPrevious()
        {
            LastSwipeDirection.SetValue(SwipeDirection.Down);
            
            bool success = await model.GoToPreviousGameAsync(_navigationCts.Token);
            
            if (success)
            {
                OnPreviousGameStarted?.Invoke();
                ResetSwipeProgress();
            }
        }

        private void OnResetSwipe()
        {
            ResetSwipeProgress();
        }

        private void OnUpdateSwipeProgress(float progress)
        {
            SwipeProgress.SetValue(progress);
            OnSwipeProgressChanged?.Invoke(progress);
        }

        private void ResetSwipeProgress()
        {
            SwipeProgress.SetValue(0f);
            LastSwipeDirection.SetValue(SwipeDirection.None);
        }

        private void OnGamePlayRequested(int gameIndex)
        {
            // Handle game play request
            // This could communicate with the game service provider
        }

        private void OnGamePauseRequested(int gameIndex)
        {
            // Handle game pause request
        }

        private void OnGameRestartRequested(int gameIndex)
        {
            // Handle game restart request
        }

        /// <summary>
        /// Handle input from swipe or other input methods
        /// </summary>
        public void HandleSwipeInput(float deltaY)
        {
            if (!IsEnabled.Value)
                return;
                
            // Update swipe progress based on input
            float progress = Mathf.Clamp(SwipeProgress.Value + deltaY, -1f, 1f);
            UpdateSwipeProgressCommand.Execute(progress);
            
            // Determine if we should trigger navigation
            const float swipeThreshold = 0.4f;
            
            if (progress > swipeThreshold && CanGoToNext())
            {
                _ = GoToNextCommand.Execute();
            }
            else if (progress < -swipeThreshold && CanGoToPrevious())
            {
                _ = GoToPreviousCommand.Execute();
            }
        }

        protected override void OnDispose()
        {
            // Cancel any ongoing navigation
            _navigationCts?.Cancel();
            _navigationCts?.Dispose();
            
            // Unsubscribe is handled by composite disposable
            // which disposes all subscriptions automatically
            
            // Clear view model cache
            foreach (var vm in _gameViewModelCache.Values)
            {
                vm.OnPlayRequested -= OnGamePlayRequested;
                vm.OnPauseRequested -= OnGamePauseRequested;
                vm.OnRestartRequested -= OnGameRestartRequested;
            }
            _gameViewModelCache.Clear();
            
            // Clear events
            OnNextGameStarted = null;
            OnPreviousGameStarted = null;
            OnSwipeProgressChanged = null;
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }

    /// <summary>
    /// Swipe direction enumeration
    /// </summary>
    public enum SwipeDirection
    {
        None,
        Up,
        Down
    }
}
