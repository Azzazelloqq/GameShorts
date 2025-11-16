using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Azzazelloqq.MVVM.ReactiveLibrary.Collections;
using Code.Core.GamesLoader;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.Models
{
    /// <summary>
    /// Model for the entire GameSwiper, managing the collection of games
    /// and navigation state.
    /// </summary>
    public class GameSwiperModel : ModelBase
    {
        /// <summary>
        /// List of all game items in the swiper
        /// </summary>
        public IReactiveList<GameItemModel> Games { get; }
        
        /// <summary>
        /// Current game index
        /// </summary>
        public IReactiveProperty<int> CurrentGameIndex { get; }
        
        /// <summary>
        /// Whether navigation to next game is available
        /// </summary>
        public IReactiveProperty<bool> CanGoNext { get; }
        
        /// <summary>
        /// Whether navigation to previous game is available
        /// </summary>
        public IReactiveProperty<bool> CanGoPrevious { get; }
        
        /// <summary>
        /// Whether the swiper is currently transitioning between games
        /// </summary>
        public IReactiveProperty<bool> IsTransitioning { get; }
        
        /// <summary>
        /// Global loading state
        /// </summary>
        public IReactiveProperty<bool> IsLoading { get; }
        
        /// <summary>
        /// Swiper settings
        /// </summary>
        public SwiperSettings Settings { get; }
        
        private readonly IShortGameServiceProvider _gameServiceProvider;
        private readonly Dictionary<int, GameItemModel> _gameItemsCache;
        
        public GameSwiperModel(IShortGameServiceProvider gameServiceProvider, SwiperSettings settings = null)
        {
            _gameServiceProvider = gameServiceProvider ?? throw new ArgumentNullException(nameof(gameServiceProvider));
            Settings = settings ?? new SwiperSettings();
            
            Games = new ReactiveList<GameItemModel>();
            CurrentGameIndex = new ReactiveProperty<int>(0);
            CanGoNext = new ReactiveProperty<bool>(false);
            CanGoPrevious = new ReactiveProperty<bool>(false);
            IsTransitioning = new ReactiveProperty<bool>(false);
            IsLoading = new ReactiveProperty<bool>(false);
            
            _gameItemsCache = new Dictionary<int, GameItemModel>();
        }

        /// <summary>
        /// Initialize the model with games from the service provider
        /// </summary>
        public async Task InitializeGamesAsync(CancellationToken token)
        {
            IsLoading.SetValue(true);
            
            try
            {
                // Wait for the current game to be ready
                if (!_gameServiceProvider.IsCurrentGameReady)
                {
                    int waitTime = 0;
                    const int maxWaitTime = 10000; // 10 seconds timeout
                    
                    while (!_gameServiceProvider.IsCurrentGameReady && waitTime < maxWaitTime)
                    {
                        await Task.Delay(100, token);
                        waitTime += 100;
                    }
                }
                
                // Initialize with current visible games (previous, current, next)
                UpdateVisibleGames();
                UpdateNavigationState();
                
                // Start the current game
                if (_gameServiceProvider.IsCurrentGameReady)
                {
                    _gameServiceProvider.StartCurrentGame();
                    
                    // Mark the current game as active
                    if (Games.Count > CurrentGameIndex.Value)
                    {
                        Games[CurrentGameIndex.Value].SetActiveState(true);
                    }
                }
            }
            finally
            {
                IsLoading.SetValue(false);
            }
        }

        /// <summary>
        /// Navigate to the next game
        /// </summary>
        public async Task<bool> GoToNextGameAsync(CancellationToken token)
        {
            if (!CanGoNext.Value || IsTransitioning.Value)
            {
                return false;
            }
            
            IsTransitioning.SetValue(true);
            
            try
            {
                // Show loading if game is not ready
                if (!_gameServiceProvider.IsNextGameReady)
                {
                    IsLoading.SetValue(true);
                }
                
                // Switch to next game in service provider
                bool success = await _gameServiceProvider.SwipeToNextGameAsync(token);
                
                if (success)
                {
                    // Update current index
                    int newIndex = CurrentGameIndex.Value + 1;
                    CurrentGameIndex.SetValue(newIndex);
                    
                    // Update active states
                    UpdateActiveStates(newIndex);
                    
                    // Update visible games after transition
                    UpdateVisibleGames();
                    UpdateNavigationState();
                }
                
                return success;
            }
            finally
            {
                IsLoading.SetValue(false);
                IsTransitioning.SetValue(false);
            }
        }

        /// <summary>
        /// Navigate to the previous game
        /// </summary>
        public async Task<bool> GoToPreviousGameAsync(CancellationToken token)
        {
            if (!CanGoPrevious.Value || IsTransitioning.Value)
            {
                return false;
            }
            
            IsTransitioning.SetValue(true);
            
            try
            {
                // Show loading if game is not ready
                if (!_gameServiceProvider.IsPreviousGameReady)
                {
                    IsLoading.SetValue(true);
                }
                
                // Switch to previous game in service provider
                bool success = await _gameServiceProvider.SwipeToPreviousGameAsync(token);
                
                if (success)
                {
                    // Update current index
                    int newIndex = CurrentGameIndex.Value - 1;
                    CurrentGameIndex.SetValue(newIndex);
                    
                    // Update active states
                    UpdateActiveStates(newIndex);
                    
                    // Update visible games after transition
                    UpdateVisibleGames();
                    UpdateNavigationState();
                }
                
                return success;
            }
            finally
            {
                IsLoading.SetValue(false);
                IsTransitioning.SetValue(false);
            }
        }

        /// <summary>
        /// Update the visible games (previous, current, next)
        /// </summary>
        private void UpdateVisibleGames()
        {
            // Clear current games
            Games.Clear();
            
            int currentIndex = CurrentGameIndex.Value;
            
            // Add previous game
            if (_gameServiceProvider.HasPreviousGame)
            {
                var previousGame = GetOrCreateGameItem(currentIndex - 1);
                previousGame.UpdateRenderTexture(_gameServiceProvider.PreviousGameRenderTexture);
                previousGame.SetLoadingState(!_gameServiceProvider.IsPreviousGameReady);
                Games.Add(previousGame);
            }
            
            // Add current game
            var currentGame = GetOrCreateGameItem(currentIndex);
            currentGame.UpdateRenderTexture(_gameServiceProvider.CurrentGameRenderTexture);
            currentGame.SetLoadingState(!_gameServiceProvider.IsCurrentGameReady);
            currentGame.SetActiveState(true);
            Games.Add(currentGame);
            
            // Add next game
            if (_gameServiceProvider.HasNextGame)
            {
                var nextGame = GetOrCreateGameItem(currentIndex + 1);
                nextGame.UpdateRenderTexture(_gameServiceProvider.NextGameRenderTexture);
                nextGame.SetLoadingState(!_gameServiceProvider.IsNextGameReady);
                Games.Add(nextGame);
            }
        }

        /// <summary>
        /// Get or create a game item for the given index
        /// </summary>
        private GameItemModel GetOrCreateGameItem(int index)
        {
            if (!_gameItemsCache.ContainsKey(index))
            {
                var gameItem = new GameItemModel(index);
                _gameItemsCache[index] = gameItem;
                compositeDisposable.AddDisposable(gameItem);
            }
            
            return _gameItemsCache[index];
        }

        /// <summary>
        /// Update active states for games
        /// </summary>
        private void UpdateActiveStates(int activeIndex)
        {
            foreach (var game in Games)
            {
                game.SetActiveState(game.Index == activeIndex);
            }
        }

        /// <summary>
        /// Update navigation availability based on service provider state
        /// </summary>
        private void UpdateNavigationState()
        {
            CanGoNext.SetValue(_gameServiceProvider.HasNextGame);
            CanGoPrevious.SetValue(_gameServiceProvider.HasPreviousGame);
        }

        protected override void OnInitialize()
        {
            // Initialize synchronous setup
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            // Initialize async setup
            return default;
        }

        protected override void OnDispose()
        {
            // Dispose reactive properties
            Games?.Dispose();
            CurrentGameIndex?.Dispose();
            CanGoNext?.Dispose();
            CanGoPrevious?.Dispose();
            IsTransitioning?.Dispose();
            IsLoading?.Dispose();
            
            // Clear cache
            _gameItemsCache?.Clear();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }

    /// <summary>
    /// Settings for the GameSwiper
    /// </summary>
    public class SwiperSettings
    {
        public float AnimationDuration { get; set; } = 0.3f;
        public bool UseScreenHeight { get; set; } = true;
        public float ImageSpacing { get; set; } = 1920f;
        public bool EnableHapticFeedback { get; set; } = false;
        public bool EnableSoundEffects { get; set; } = false;
    }
}


