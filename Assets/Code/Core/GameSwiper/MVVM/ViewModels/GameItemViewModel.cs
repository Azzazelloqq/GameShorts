using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.Core.GameSwiper.MVVM.Models;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.ViewModels
{
    /// <summary>
    /// ViewModel for a single game item.
    /// Exposes model data and provides commands for game-specific actions.
    /// </summary>
    public class GameItemViewModel : ViewModelBase<GameItemModel>
    {
        // Expose read-only reactive properties from the model
        public IReadOnlyReactiveProperty<RenderTexture> RenderTexture => model.RenderTexture;
        public IReadOnlyReactiveProperty<string> GameName => model.GameName;
        public IReadOnlyReactiveProperty<string> GameDescription => model.GameDescription;
        public IReadOnlyReactiveProperty<bool> IsLoading => model.IsLoading;
        public IReadOnlyReactiveProperty<bool> IsActive => model.IsActive;
        public IReadOnlyReactiveProperty<int> Score => model.Score;
        public IReadOnlyReactiveProperty<object> CustomData => model.CustomData;
        
        // UI-specific properties
        public IReactiveProperty<bool> IsUIVisible { get; }
        public IReactiveProperty<bool> ShowDetails { get; }
        public IReactiveProperty<float> UIOpacity { get; }
        
        // Commands
        public IActionCommand PlayCommand { get; private set; }
        public IActionCommand PauseCommand { get; private set; }
        public IActionCommand RestartCommand { get; private set; }
        public IActionCommand ShowLeaderboardCommand { get; private set; }
        public IActionCommand ToggleDetailsCommand { get; private set; }
        
        // Events for game-specific actions
        public event Action<int> OnPlayRequested;
        public event Action<int> OnPauseRequested;
        public event Action<int> OnRestartRequested;
        
        public int GameIndex => model.Index;

        public GameItemViewModel(GameItemModel model) : base(model)
        {
            // Initialize UI-specific properties
            IsUIVisible = new ReactiveProperty<bool>(true);
            ShowDetails = new ReactiveProperty<bool>(false);
            UIOpacity = new ReactiveProperty<float>(1f);
            
            // Add to composite disposable for cleanup
            compositeDisposable.AddDisposable(IsUIVisible);
            compositeDisposable.AddDisposable(ShowDetails);
            compositeDisposable.AddDisposable(UIOpacity);
        }

        protected override void OnInitialize()
        {
            // Initialize commands
            PlayCommand = new ActionCommand(OnPlay, CanPlay);
            PauseCommand = new ActionCommand(OnPause, CanPause);
            RestartCommand = new ActionCommand(OnRestart, CanRestart);
            ShowLeaderboardCommand = new ActionCommand(OnShowLeaderboard);
            ToggleDetailsCommand = new ActionCommand(OnToggleDetails);
            
            // Add commands to composite disposable
            compositeDisposable.AddDisposable(PlayCommand);
            compositeDisposable.AddDisposable(PauseCommand);
            compositeDisposable.AddDisposable(RestartCommand);
            compositeDisposable.AddDisposable(ShowLeaderboardCommand);
            compositeDisposable.AddDisposable(ToggleDetailsCommand);
            
            // Subscribe to model changes
            model.IsActive.Subscribe(OnActiveStateChanged);
            model.IsLoading.Subscribe(OnLoadingStateChanged);
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        /// <summary>
        /// Update UI visibility based on position
        /// </summary>
        public void UpdateUIVisibility(bool isVisible, float opacity = 1f)
        {
            IsUIVisible.SetValue(isVisible);
            UIOpacity.SetValue(opacity);
        }

        /// <summary>
        /// Show or hide detailed information
        /// </summary>
        public void SetDetailsVisible(bool visible)
        {
            ShowDetails.SetValue(visible);
        }

        private void OnActiveStateChanged(bool isActive)
        {
            // Update UI state based on whether this game is active
            if (!isActive)
            {
                ShowDetails.SetValue(false);
            }
        }

        private void OnLoadingStateChanged(bool isLoading)
        {
            // Update UI opacity when loading
            if (isLoading)
            {
                UIOpacity.SetValue(0.5f);
            }
            else if (IsActive.Value)
            {
                UIOpacity.SetValue(1f);
            }
        }

        private bool CanPlay()
        {
            return IsActive.Value && !IsLoading.Value;
        }

        private void OnPlay()
        {
            OnPlayRequested?.Invoke(GameIndex);
        }

        private bool CanPause()
        {
            return IsActive.Value && !IsLoading.Value;
        }

        private void OnPause()
        {
            OnPauseRequested?.Invoke(GameIndex);
        }

        private bool CanRestart()
        {
            return IsActive.Value && !IsLoading.Value;
        }

        private void OnRestart()
        {
            OnRestartRequested?.Invoke(GameIndex);
        }

        private void OnShowLeaderboard()
        {
            // Implement leaderboard logic
            // This could open a modal or navigate to a leaderboard screen
        }

        private void OnToggleDetails()
        {
            ShowDetails.SetValue(!ShowDetails.Value);
        }

        protected override void OnDispose()
        {
            // Unsubscribe from model events
            model.IsActive.Unsubscribe(OnActiveStateChanged);
            model.IsLoading.Unsubscribe(OnLoadingStateChanged);
            
            // Clear event handlers
            OnPlayRequested = null;
            OnPauseRequested = null;
            OnRestartRequested = null;
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}

