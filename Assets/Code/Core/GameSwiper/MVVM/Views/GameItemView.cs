using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary.Callbacks;
using Code.Core.GameSwiper.MVVM.ViewModels;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.GameSwiper.MVVM.Views
{
    /// <summary>
    /// View for a single game item in the swiper.
    /// Displays the game render texture and overlay UI elements.
    /// </summary>
    public class GameItemView : ViewMonoBehavior<GameItemViewModel>
    {
        [Header("Core Components")]
        [SerializeField]
        private RawImage _gameRenderImage;
        
        [SerializeField]
        private GameSwiperImageFitter _imageFitter;
        
        [Header("UI Overlay")]
        [SerializeField]
        private GameObject _uiOverlay;
        
        [SerializeField]
        private CanvasGroup _overlayCanvasGroup;
        
        [Header("Game Info")]
        [SerializeField]
        private TextMeshProUGUI _gameNameText;
        
        [SerializeField]
        private TextMeshProUGUI _gameDescriptionText;
        
        [SerializeField]
        private TextMeshProUGUI _scoreText;
        
        [Header("Loading")]
        [SerializeField]
        private GameObject _loadingIndicator;
        
        [SerializeField]
        private Image _loadingSpinner;
        
        [Header("Controls")]
        [SerializeField]
        private Button _playButton;
        
        [SerializeField]
        private Button _pauseButton;
        
        [SerializeField]
        private Button _restartButton;
        
        [SerializeField]
        private Button _leaderboardButton;
        
        [SerializeField]
        private Button _detailsButton;
        
        [Header("Details Panel")]
        [SerializeField]
        private GameObject _detailsPanel;
        
        [SerializeField]
        private CanvasGroup _detailsPanelCanvasGroup;
        
        [Header("Animation Settings")]
        [SerializeField]
        private float _fadeDuration = 0.3f;
        
        [SerializeField]
        private float _scaleDuration = 0.2f;
        
        private Tween _loadingTween;
        private bool _isInitialized;

        protected override void OnInitialize()
        {
            if (_isInitialized)
                return;
                
            _isInitialized = true;
            
            // Setup image fitter if not present
            if (_imageFitter == null && _gameRenderImage != null)
            {
                _imageFitter = _gameRenderImage.GetComponent<GameSwiperImageFitter>();
                if (_imageFitter == null)
                {
                    _imageFitter = _gameRenderImage.gameObject.AddComponent<GameSwiperImageFitter>();
                }
            }
            
            // Bind to ViewModel properties
            BindProperties();
            
            // Setup button commands
            SetupButtons();
            
            // Initial UI setup
            SetupInitialUIState();
        }

        protected override ValueTask OnInitializeAsync(CancellationToken token)
        {
            return default;
        }

        private void BindProperties()
        {
            // Bind render texture
            compositeDisposable.AddDisposable(viewModel.RenderTexture.Subscribe(OnRenderTextureChanged));
            
            // Bind game info
            compositeDisposable.AddDisposable(viewModel.GameName.Subscribe(OnGameNameChanged));
            compositeDisposable.AddDisposable(viewModel.GameDescription.Subscribe(OnGameDescriptionChanged));
            compositeDisposable.AddDisposable(viewModel.Score.Subscribe(OnScoreChanged));
            
            // Bind loading state
            compositeDisposable.AddDisposable(viewModel.IsLoading.Subscribe(OnLoadingStateChanged));
            
            // Bind active state
            compositeDisposable.AddDisposable(viewModel.IsActive.Subscribe(OnActiveStateChanged));
            
            // Bind UI visibility
            compositeDisposable.AddDisposable(viewModel.IsUIVisible.Subscribe(OnUIVisibilityChanged));
            compositeDisposable.AddDisposable(viewModel.UIOpacity.Subscribe(OnUIOpacityChanged));
            
            // Bind details panel
            compositeDisposable.AddDisposable(viewModel.ShowDetails.Subscribe(OnShowDetailsChanged));
        }

        private void SetupButtons()
        {
            // Play button
            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
                _playButton.onClick.AddListener(() => viewModel.PlayCommand.Execute());
            }
            
            // Pause button
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
                _pauseButton.onClick.AddListener(() => viewModel.PauseCommand.Execute());
            }
            
            // Restart button
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
                _restartButton.onClick.AddListener(() => viewModel.RestartCommand.Execute());
            }
            
            // Leaderboard button
            if (_leaderboardButton != null)
            {
                _leaderboardButton.onClick.RemoveAllListeners();
                _leaderboardButton.onClick.AddListener(() => viewModel.ShowLeaderboardCommand.Execute());
            }
            
            // Details button
            if (_detailsButton != null)
            {
                _detailsButton.onClick.RemoveAllListeners();
                _detailsButton.onClick.AddListener(() => viewModel.ToggleDetailsCommand.Execute());
            }
        }

        private void SetupInitialUIState()
        {
            // Hide loading indicator initially
            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(false);
            }
            
            // Hide details panel initially
            if (_detailsPanel != null)
            {
                _detailsPanel.SetActive(false);
            }
            
            // Set initial overlay opacity
            if (_overlayCanvasGroup != null)
            {
                _overlayCanvasGroup.alpha = 0f;
            }
        }

        private void OnRenderTextureChanged(RenderTexture texture)
        {
            if (_gameRenderImage != null)
            {
                _gameRenderImage.texture = texture;
                _gameRenderImage.enabled = texture != null;
                
                // Update aspect ratio fitter
                if (_imageFitter != null)
                {
                    _imageFitter.OnTextureChanged();
                }
            }
        }

        private void OnGameNameChanged(string gameName)
        {
            if (_gameNameText != null)
            {
                _gameNameText.text = gameName;
            }
        }

        private void OnGameDescriptionChanged(string description)
        {
            if (_gameDescriptionText != null)
            {
                _gameDescriptionText.text = description;
            }
        }

        private void OnScoreChanged(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"Score: {score:N0}";
            }
        }

        private void OnLoadingStateChanged(bool isLoading)
        {
            if (_loadingIndicator != null)
            {
                _loadingIndicator.SetActive(isLoading);
                
                if (isLoading && _loadingSpinner != null)
                {
                    // Start spinning animation
                    _loadingTween?.Kill();
                    _loadingTween = _loadingSpinner.transform
                        .DORotate(new Vector3(0, 0, -360), 2f, RotateMode.FastBeyond360)
                        .SetLoops(-1, LoopType.Restart)
                        .SetEase(Ease.Linear);
                }
                else
                {
                    // Stop spinning
                    _loadingTween?.Kill();
                }
            }
            
            // Update button states
            UpdateButtonStates();
        }

        private void OnActiveStateChanged(bool isActive)
        {
            // Show/hide UI overlay based on active state
            if (_overlayCanvasGroup != null)
            {
                float targetAlpha = isActive ? 1f : 0f;
                _overlayCanvasGroup.DOFade(targetAlpha, _fadeDuration);
                _overlayCanvasGroup.interactable = isActive;
                _overlayCanvasGroup.blocksRaycasts = isActive;
            }
            
            // Update button states
            UpdateButtonStates();
        }

        private void OnUIVisibilityChanged(bool isVisible)
        {
            if (_uiOverlay != null)
            {
                _uiOverlay.SetActive(isVisible);
            }
        }

        private void OnUIOpacityChanged(float opacity)
        {
            if (_overlayCanvasGroup != null)
            {
                _overlayCanvasGroup.alpha = opacity;
            }
        }

        private void OnShowDetailsChanged(bool showDetails)
        {
            if (_detailsPanel != null)
            {
                if (showDetails)
                {
                    _detailsPanel.SetActive(true);
                    
                    if (_detailsPanelCanvasGroup != null)
                    {
                        _detailsPanelCanvasGroup.alpha = 0f;
                        _detailsPanelCanvasGroup.transform.localScale = Vector3.one * 0.8f;
                        
                        // Animate in
                        _detailsPanelCanvasGroup.DOFade(1f, _fadeDuration);
                        _detailsPanelCanvasGroup.transform.DOScale(1f, _scaleDuration)
                            .SetEase(Ease.OutBack);
                    }
                }
                else
                {
                    if (_detailsPanelCanvasGroup != null)
                    {
                        // Animate out
                        _detailsPanelCanvasGroup.DOFade(0f, _fadeDuration);
                        _detailsPanelCanvasGroup.transform.DOScale(0.8f, _scaleDuration)
                            .SetEase(Ease.InBack)
                            .OnComplete(() => _detailsPanel.SetActive(false));
                    }
                    else
                    {
                        _detailsPanel.SetActive(false);
                    }
                }
            }
        }

        private void UpdateButtonStates()
        {
            bool canInteract = viewModel.IsActive.Value && !viewModel.IsLoading.Value;
            
            if (_playButton != null)
            {
                _playButton.interactable = canInteract;
            }
            
            if (_pauseButton != null)
            {
                _pauseButton.interactable = canInteract;
            }
            
            if (_restartButton != null)
            {
                _restartButton.interactable = canInteract;
            }
        }

        protected override void OnDispose()
        {
            // Kill any active tweens
            _loadingTween?.Kill();
            
            // Remove button listeners
            if (_playButton != null)
            {
                _playButton.onClick.RemoveAllListeners();
            }
            
            if (_pauseButton != null)
            {
                _pauseButton.onClick.RemoveAllListeners();
            }
            
            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveAllListeners();
            }
            
            if (_leaderboardButton != null)
            {
                _leaderboardButton.onClick.RemoveAllListeners();
            }
            
            if (_detailsButton != null)
            {
                _detailsButton.onClick.RemoveAllListeners();
            }
        }

        protected override ValueTask OnDisposeAsync(CancellationToken token)
        {
            return default;
        }
    }
}
