using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.Models
{
    /// <summary>
    /// Model representing a single game item in the swiper.
    /// Contains all data related to a single game including its render texture and metadata.
    /// </summary>
    public class GameItemModel : ModelBase
    {
        /// <summary>
        /// The render texture of the game being displayed
        /// </summary>
        public IReactiveProperty<RenderTexture> RenderTexture { get; }
        
        /// <summary>
        /// The name/title of the game
        /// </summary>
        public IReactiveProperty<string> GameName { get; }
        
        /// <summary>
        /// A short description of the game
        /// </summary>
        public IReactiveProperty<string> GameDescription { get; }
        
        /// <summary>
        /// Whether the game is currently loading
        /// </summary>
        public IReactiveProperty<bool> IsLoading { get; }
        
        /// <summary>
        /// Whether the game is currently active/visible
        /// </summary>
        public IReactiveProperty<bool> IsActive { get; }
        
        /// <summary>
        /// The score or progress in the game (if applicable)
        /// </summary>
        public IReactiveProperty<int> Score { get; }
        
        /// <summary>
        /// Custom metadata for game-specific UI elements
        /// </summary>
        public IReactiveProperty<object> CustomData { get; }
        
        /// <summary>
        /// The index of this game in the swiper
        /// </summary>
        public int Index { get; }

        public GameItemModel(int index)
        {
            Index = index;
            RenderTexture = new ReactiveProperty<RenderTexture>(null);
            GameName = new ReactiveProperty<string>($"Game {index}");
            GameDescription = new ReactiveProperty<string>(string.Empty);
            IsLoading = new ReactiveProperty<bool>(false);
            IsActive = new ReactiveProperty<bool>(false);
            Score = new ReactiveProperty<int>(0);
            CustomData = new ReactiveProperty<object>(null);
        }

        /// <summary>
        /// Update the render texture for this game
        /// </summary>
        public void UpdateRenderTexture(RenderTexture texture)
        {
            RenderTexture.SetValue(texture);
        }

        /// <summary>
        /// Update game metadata
        /// </summary>
        public void UpdateMetadata(string name, string description = null)
        {
            if (!string.IsNullOrEmpty(name))
            {
                GameName.SetValue(name);
            }
            
            if (description != null)
            {
                GameDescription.SetValue(description);
            }
        }

        /// <summary>
        /// Set the loading state of the game
        /// </summary>
        public void SetLoadingState(bool isLoading)
        {
            IsLoading.SetValue(isLoading);
        }

        /// <summary>
        /// Set whether this game is currently active/visible
        /// </summary>
        public void SetActiveState(bool isActive)
        {
            IsActive.SetValue(isActive);
        }

        /// <summary>
        /// Update the score/progress
        /// </summary>
        public void UpdateScore(int score)
        {
            Score.SetValue(score);
        }

        /// <summary>
        /// Set custom data for game-specific UI
        /// </summary>
        public void SetCustomData(object data)
        {
            CustomData.SetValue(data);
        }

        protected override void OnInitialize()
        {
            // Initialize any synchronous setup here if needed
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            // Initialize any async setup here if needed
            return default;
        }

        protected override void OnDispose()
        {
            // Dispose of reactive properties
            RenderTexture?.Dispose();
            GameName?.Dispose();
            GameDescription?.Dispose();
            IsLoading?.Dispose();
            IsActive?.Dispose();
            Score?.Dispose();
            CustomData?.Dispose();
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}


