using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.GamesLoader;
using Code.Core.GameSwiper.MVVM.Models;
using Code.Core.GameSwiper.MVVM.ViewModels;
using Code.Core.GameSwiper.MVVM.Views;
using Code.Generated.Addressables;
using InGameLogger;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.GameSwiper.MVVM.Factory
{
    /// <summary>
    /// Factory for creating and initializing MVVM GameSwiper components
    /// </summary>
    public class GameSwiperFactory
    {
        private readonly IResourceLoader _resourceLoader;
        private readonly IInGameLogger _logger;

        public GameSwiperFactory(IResourceLoader resourceLoader, IInGameLogger logger)
        {
            _resourceLoader = resourceLoader ?? throw new ArgumentNullException(nameof(resourceLoader));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Create a complete MVVM GameSwiper with all components
        /// </summary>
        public async Task<GameSwiperMVVM> CreateGameSwiperAsync(
            IShortGameServiceProvider gameServiceProvider,
            Transform uiRoot,
            SwiperSettings settings = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Create Model
                var model = CreateModel(gameServiceProvider, settings);
                
                // Create ViewModel
                var viewModel = CreateViewModel(model);
                
                // Create View
                var view = await CreateViewAsync(viewModel, uiRoot, cancellationToken);
                
                // Initialize all components
                await InitializeComponentsAsync(model, viewModel, view, cancellationToken);
                
                return new GameSwiperMVVM
                {
                    Model = model,
                    ViewModel = viewModel,
                    View = view
                };
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Create the Model component
        /// </summary>
        private GameSwiperModel CreateModel(IShortGameServiceProvider gameServiceProvider, SwiperSettings settings)
        {
            settings ??= new SwiperSettings
            {
                AnimationDuration = 0.3f,
                UseScreenHeight = true,
                ImageSpacing = 1920f,
                EnableHapticFeedback = false,
                EnableSoundEffects = false
            };
            
            var model = new GameSwiperModel(gameServiceProvider, settings);
            
            return model;
        }

        /// <summary>
        /// Create the ViewModel component
        /// </summary>
        private GameSwiperViewModel CreateViewModel(GameSwiperModel model)
        {
            var viewModel = new GameSwiperViewModel(model);
            viewModel.Initialize();
            
            return viewModel;
        }

        /// <summary>
        /// Create and setup the View component
        /// </summary>
        private async Task<GameSwiperView> CreateViewAsync(
            GameSwiperViewModel viewModel,
            Transform uiRoot,
            CancellationToken cancellationToken)
        {
            // Load the prefab
            var prefab = await LoadPrefabAsync(cancellationToken);
            
            if (prefab == null)
            {
                throw new InvalidOperationException("GameSwiper prefab not found");
            }
            
            // Instantiate the prefab
            var instance = Object.Instantiate(prefab, uiRoot);
            
            // Try to get existing GameSwiperView component
            var view = instance.GetComponent<GameSwiperView>();
            
            if (view == null)
            {
                // If not present, add it
                view = instance.AddComponent<GameSwiperView>();
            }
            
            // Initialize view with ViewModel
            view.Initialize(viewModel);
            
            return view;
        }

        /// <summary>
        /// Load the GameSwiper prefab from resources
        /// </summary>
        private async Task<GameObject> LoadPrefabAsync(CancellationToken cancellationToken)
        {
            try
            {
                var prefab = await _resourceLoader.LoadResourceAsync<GameObject>(
                    ResourceIdsContainer.DefaultLocalGroup.GameSwiper,
                    cancellationToken);
                
                if (prefab == null)
                {
                }
                
                return prefab;
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Initialize all components asynchronously
        /// </summary>
        private async Task InitializeComponentsAsync(
            GameSwiperModel model,
            GameSwiperViewModel viewModel,
            GameSwiperView view,
            CancellationToken cancellationToken)
        {
            // Initialize Model's async operations (loading games)
            await model.InitializeGamesAsync(cancellationToken);
            
            // Initialize ViewModel's async operations
            await viewModel.InitializeAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Container for MVVM components
    /// </summary>
    public class GameSwiperMVVM : IDisposable
    {
        public GameSwiperModel Model { get; set; }
        public GameSwiperViewModel ViewModel { get; set; }
        public GameSwiperView View { get; set; }

        public void Dispose()
        {
            View?.Dispose();
            ViewModel?.Dispose();
            Model?.Dispose();
        }
    }
}


