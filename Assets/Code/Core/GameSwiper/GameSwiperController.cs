using System;
using System.Threading;
using System.Threading.Tasks;
using Code.Core.BaseDMDisposable.Scripts;
using Code.Core.Tools;
using Code.Generated.Addressables;
using InGameLogger;
using LightDI.Runtime;
using R3;
using ResourceLoader;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Code.Core.GameSwiper
{
    /// <summary>
    /// Контроллер для управления связью между GameSwiper и GameSwiperView
    /// </summary>

    public struct Ctx
    {
        public Transform PlaceForAllUi;
    }
    public class GameSwiperController : BaseDisposable
    {
        private readonly ISwiperGame _gameSwiper;
        private readonly IInGameLogger _logger;
        private CancellationTokenSource _cancellationTokenSource;
        private readonly IResourceLoader _resourceLoader;
        private readonly Ctx _ctx;
        private GameSwiperView _gameSwiperView;
        private readonly ReactiveTrigger _onPreviewGame;
        private readonly ReactiveTrigger _onNextGame;

        public GameSwiperController(Ctx ctx, [Inject] IInGameLogger logger, [Inject] IResourceLoader resourceLoader)
        {
            _ctx = ctx;
            _cancellationTokenSource = new CancellationTokenSource();
            _gameSwiper = GameSwiperFactory.CreateGameSwiper();
            _logger = logger;
            _resourceLoader = resourceLoader;
            _onNextGame = new ReactiveTrigger();
            _onPreviewGame = new ReactiveTrigger();
            LoadView(_cancellationTokenSource.Token);
            AddDispose(_onNextGame.Subscribe(HandleNextGameRequested));
            AddDispose(_onPreviewGame.Subscribe(HandlePreviousGameRequested));
        }

        private async Task LoadView(CancellationToken cancellationToken)
        {
            await LoadGameSwiperViewAsync(cancellationToken);
        }
        
        private async Task LoadGameSwiperViewAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Загружаем префаб GameSwiperView
                var swiperViewPrefab = await _resourceLoader.LoadResourceAsync<GameObject>(
                    ResourceIdsContainer.DefaultLocalGroup.GameSwiper, cancellationToken);
                
                if (swiperViewPrefab != null)
                {
                    // Создаем экземпляр UI
                    var disposables = new CompositeDisposable();
                    var swiperViewInstance = AddComponent(Object.Instantiate(swiperViewPrefab, _ctx.PlaceForAllUi));
                    _gameSwiperView = swiperViewInstance.GetComponent<GameSwiperView>();
                    _gameSwiperView.SetCtx(new GameSwiperView.Ctx()
                    {
                        Disposables = disposables,
                        OnNextGameRequested = _onNextGame,
                        OnPreviousGameRequested = _onPreviewGame,
                    });
                    
                }
                else
                {
                    _logger.LogWarning($"GameSwiperView prefab not found at path: {ResourceIdsContainer.DefaultLocalGroup.GameSwiper}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to load GameSwiperView: {ex.Message}");
            }
        }
        
        private async void HandleNextGameRequested()
        {
            if (_gameSwiper == null) return;
            
            try
            {
                _logger.Log("GameSwiperController: Next game requested");
                
                // Уведомляем view о начале загрузки
                _gameSwiperView?.SetLoadingState(true);
                
                var nextGame = await _gameSwiper.NextGameAsync(_cancellationTokenSource.Token);
                
                // Уведомляем view о завершении загрузки
                _gameSwiperView?.SetLoadingState(false);
                
                if (nextGame != null)
                {
                    _logger.Log($"GameSwiperController: Successfully switched to next game: {nextGame.GetType().Name}");
                }
                else
                {
                    _logger.LogWarning("GameSwiperController: Failed to switch to next game - no game returned");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log("GameSwiperController: Next game operation was cancelled");
                _gameSwiperView?.SetLoadingState(false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GameSwiperController: Error switching to next game: {ex.Message}");
                _gameSwiperView?.SetLoadingState(false);
            }
        }
        
        private async void HandlePreviousGameRequested()
        {
            if (_gameSwiper == null) return;
            
            try
            {
                _logger.Log("GameSwiperController: Previous game requested");
                
                // Уведомляем view о начале загрузки
                _gameSwiperView?.SetLoadingState(true);
                
                var previousGame = await _gameSwiper.PreviousGameAsync(_cancellationTokenSource.Token);
                
                // Уведомляем view о завершении загрузки
                _gameSwiperView?.SetLoadingState(false);
                
                if (previousGame != null)
                {
                    _logger.Log($"GameSwiperController: Successfully switched to previous game: {previousGame.GetType().Name}");
                   
                }
                else
                {
                    _logger.LogWarning("GameSwiperController: Failed to switch to previous game - no game returned");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.Log("GameSwiperController: Previous game operation was cancelled");
                _gameSwiperView?.SetLoadingState(false);
            }
            catch (Exception ex)
            {
                _logger.LogError($"GameSwiperController: Error switching to previous game: {ex.Message}");
                _gameSwiperView?.SetLoadingState(false);
            }
        }

        protected override void OnDispose()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            base.OnDispose();
        }
    }
}
