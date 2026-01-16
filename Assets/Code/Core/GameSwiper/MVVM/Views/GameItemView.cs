using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Code.Core.GameSwiper.MVVM.ViewModels;
using DG.Tweening;
using R3;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.GameSwiper.MVVM.Views
{
internal class GameItemView : ViewMonoBehavior<GameItemViewModel>
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

	[Header("Votes")]
	[SerializeField]
	private GameVotePanelView _votePanelView;

	[Header("Loading")]
	[SerializeField]
	private Image _loadingSpinner;

    [SerializeField]
    private CanvasGroup _loadingParent;

	[Header("Animation Settings")]
	[SerializeField]
	private float _fadeDuration = 0.3f;

	[SerializeField]
	private float _scaleDuration = 0.2f;

	private Tween _loadingTween;
	private Tween _loadingFadeTween;

	protected override void OnInitialize()
	{
		PrepareView();
		_votePanelView.Initialize(viewModel.VotePanelViewModel);
	}

	protected override async ValueTask OnInitializeAsync(CancellationToken token)
	{
		PrepareView();
		await _votePanelView.InitializeAsync(viewModel.VotePanelViewModel, token);
	}

	protected override void OnDispose()
	{
		_loadingTween?.Kill();
		_loadingFadeTween?.Kill();
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		_loadingTween?.Kill();
		_loadingFadeTween?.Kill();
		return default;
	}

	private void PrepareView()
	{
		if (_imageFitter == null && _gameRenderImage != null)
		{
			_imageFitter = _gameRenderImage.GetComponent<GameSwiperImageFitter>();
			if (_imageFitter == null)
			{
				_imageFitter = _gameRenderImage.gameObject.AddComponent<GameSwiperImageFitter>();
			}
		}

		ConfigureLoadingSpinner();
		SetupInitialUIState();
		BindProperties();
	}

	private void ConfigureLoadingSpinner()
	{
		if (_loadingSpinner == null)
		{
			return;
		}

		if (_loadingParent == null)
		{
			_loadingParent = _loadingSpinner.GetComponentInParent<CanvasGroup>();
			if (_loadingParent == null)
			{
				_loadingParent = _loadingSpinner.gameObject.AddComponent<CanvasGroup>();
			}
		}

		_loadingParent.ignoreParentGroups = true;
		_loadingParent.interactable = false;
		_loadingParent.blocksRaycasts = false;
		_loadingParent.alpha = 0f;
		_loadingParent.gameObject.SetActive(false);
	}

	private void BindProperties()
	{
		compositeDisposable.AddDisposable(viewModel.RenderTexture.Subscribe(OnRenderTextureChanged));
		compositeDisposable.AddDisposable(viewModel.ShouldShowLoadingIndicator.Subscribe(OnLoadingIndicatorStateChanged));
		compositeDisposable.AddDisposable(viewModel.IsActive.Subscribe(OnActiveStateChanged));
		compositeDisposable.AddDisposable(viewModel.IsUIVisible.Subscribe(OnUIVisibilityChanged));
		compositeDisposable.AddDisposable(viewModel.UIOpacity.Subscribe(OnUIOpacityChanged));
	}

	private void SetupInitialUIState()
	{
		if (_loadingParent != null)
		{
			_loadingParent.alpha = 0f;
			_loadingParent.gameObject.SetActive(false);
		}

		if (_overlayCanvasGroup != null)
		{
			_overlayCanvasGroup.alpha = 0f;
		}
	}

	private void OnRenderTextureChanged(RenderTexture texture)
	{
		_gameRenderImage.texture = texture;
		_gameRenderImage.enabled = texture != null;
		_imageFitter?.OnTextureChanged();
	}

	private void OnLoadingIndicatorStateChanged(bool isVisible)
	{
		if (_loadingSpinner == null)
		{
			return;
		}

		_loadingTween?.Kill();
		_loadingFadeTween?.Kill();

		if (_loadingParent != null)
		{
			if (isVisible)
			{
				_loadingParent.gameObject.SetActive(true);
				_loadingParent.alpha = 0f;
				_loadingFadeTween = _loadingParent
					.DOFade(1f, _fadeDuration)
					.SetEase(Ease.OutQuad);
			}
			else
			{
				_loadingFadeTween = _loadingParent
					.DOFade(0f, _fadeDuration)
					.SetEase(Ease.OutQuad)
					.OnComplete(() =>
					{
						if (_loadingParent != null)
						{
							_loadingParent.gameObject.SetActive(false);
						}
					});
			}
		}

		if (isVisible)
		{
			_loadingTween = _loadingSpinner.transform
				.DORotate(new Vector3(0, 0, -360), 2f, RotateMode.FastBeyond360)
				.SetLoops(-1, LoopType.Restart)
				.SetEase(Ease.InOutBounce);
		}
	}

	private void OnActiveStateChanged(bool isActive)
	{
		var targetAlpha = isActive ? 1f : 0f;
		_overlayCanvasGroup.DOFade(targetAlpha, _fadeDuration);
		_overlayCanvasGroup.interactable = isActive;
		_overlayCanvasGroup.blocksRaycasts = isActive;
	}

	private void OnUIVisibilityChanged(bool isVisible)
	{
		_uiOverlay.SetActive(isVisible);
	}

	private void OnUIOpacityChanged(float opacity)
	{
		_overlayCanvasGroup.alpha = opacity;
	}
}
}