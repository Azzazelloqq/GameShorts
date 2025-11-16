using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Code.Core.GameStats;
using Code.Core.GameSwiper.MVVM.ViewModels;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Core.GameSwiper.MVVM.Views
{
internal class GameVotePanelView : ViewMonoBehavior<GameVotePanelViewModel>
{
	[Header("Counters")]
	[SerializeField]
	private TMP_Text _likesCountText;

	[SerializeField]
	private TMP_Text _dislikesCountText;

	[Header("Buttons")]
	[SerializeField]
	private Button _likeButton;

	[SerializeField]
	private Button _dislikeButton;

	[Header("Icon Graphics")]
	[SerializeField]
	private Graphic _likeIconGraphic;

	[SerializeField]
	private Graphic _dislikeIconGraphic;

	[Header("Icon References")]
	[SerializeField]
	private RectTransform _likeIconTransform;

	[SerializeField]
	private RectTransform _dislikeIconTransform;

	[SerializeField]
	private RectTransform _likeCounterTransform;

	[SerializeField]
	private RectTransform _dislikeCounterTransform;

	[Header("Animation Settings")]
	[SerializeField]
	private float _iconScaleMultiplier = 1.08f;

	[SerializeField]
	private float _iconAnimationDuration = 0.18f;

	[SerializeField]
	private float _counterPunchStrength = 0.25f;

	[SerializeField]
	private float _counterPunchDuration = 0.25f;

	[SerializeField]
	private Ease _animationEase = Ease.OutBack;

	[Header("Color Settings")]
	[SerializeField]
	private Color _likeHighlightColor = new(0.98f, 0.36f, 0.47f);

	[SerializeField]
	private Color _dislikeHighlightColor = new(0.62f, 0.78f, 0.96f);

	[SerializeField]
	private float _iconColorFadeDuration = 0.25f;

	[Header("Motion Settings")]
	[SerializeField]
	private float _likeIconRiseDistance = 10f;

	[SerializeField]
	private float _dislikeIconDipDistance = 8f;

	[SerializeField]
	private float _dislikeCounterDipDistance = 6f;

	private readonly List<IDisposable> _bindings = new();

	private bool _isBound;
	private bool _hasStats;
	private int _currentLikes;
	private int _currentDislikes;

	private bool _visualDefaultsCaptured;
	private Color _likeIconDefaultColor = Color.white;
	private Color _dislikeIconDefaultColor = Color.white;
	private Vector3 _likeIconDefaultScale = Vector3.one;
	private Vector3 _dislikeIconDefaultScale = Vector3.one;
	private Quaternion _likeIconDefaultRotation = Quaternion.identity;
	private Quaternion _dislikeIconDefaultRotation = Quaternion.identity;
	private Vector3 _likeIconDefaultPosition = Vector3.zero;
	private Vector3 _dislikeIconDefaultPosition = Vector3.zero;
	private Vector3 _likeCounterDefaultScale = Vector3.one;
	private Vector3 _dislikeCounterDefaultScale = Vector3.one;
	private Vector3 _likeCounterDefaultPosition = Vector3.zero;
	private Vector3 _dislikeCounterDefaultPosition = Vector3.zero;

	private Tween _likeIconColorTween;
	private Tween _dislikeIconColorTween;
	private Tween _likeIconAnimTween;
	private Tween _dislikeIconAnimTween;
	private Tween _likeCounterAnimTween;
	private Tween _dislikeCounterAnimTween;

	protected override void OnInitialize()
	{
		Bind();
	}

	protected override ValueTask OnInitializeAsync(CancellationToken token)
	{
		Bind();
		return default;
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		Unbind();
		return default;
	}

	protected override void OnDispose()
	{
		Unbind();
	}

	private void OnDestroy()
	{
		Unbind();
	}
	
	private void Bind()
	{
		if (_isBound)
		{
			return;
		}

		if (viewModel == null)
		{
			UpdateLikesPlaceholder();
			UpdateDislikesPlaceholder();
			SetButtonsInteractable(false);
			return;
		}

		_isBound = true;
		CaptureVisualDefaults();
		ResetVisualState();

		_bindings.Add(viewModel.LikesCount.Subscribe(value =>
		{
			_currentLikes = value;
			UpdateLikesText();
		}));

		_bindings.Add(viewModel.DislikesCount.Subscribe(value =>
		{
			_currentDislikes = value;
			UpdateDislikesText();
		}));

		_bindings.Add(viewModel.HasStats.Subscribe(value =>
		{
			_hasStats = value;
			UpdateLikesText();
			UpdateDislikesText();
		}));

		_bindings.Add(viewModel.IsInteractable.Subscribe(SetButtonsInteractable));

		if (_likeButton != null)
		{
			_likeButton.onClick.AddListener(OnLikeClicked);
		}

		if (_dislikeButton != null)
		{
			_dislikeButton.onClick.AddListener(OnDislikeClicked);
		}

		viewModel.VoteAnimationRequested += HandleVoteAnimationRequested;
		SetButtonsInteractable(viewModel.IsInteractable.Value);
	}

	private void Unbind()
	{
		if (!_isBound)
		{
			return;
		}

		if (viewModel != null)
		{
			viewModel.VoteAnimationRequested -= HandleVoteAnimationRequested;
		}

		if (_likeButton != null)
		{
			_likeButton.onClick.RemoveListener(OnLikeClicked);
		}

		if (_dislikeButton != null)
		{
			_dislikeButton.onClick.RemoveListener(OnDislikeClicked);
		}

		foreach (var disposable in _bindings)
		{
			disposable?.Dispose();
		}

		_bindings.Clear();
		KillTweens();
		_isBound = false;
	}

	private void CaptureVisualDefaults()
	{
		if (_visualDefaultsCaptured)
		{
			return;
		}

		if (_likeIconGraphic == null && _likeIconTransform != null)
		{
			_likeIconGraphic = _likeIconTransform.GetComponent<Graphic>();
		}

		if (_dislikeIconGraphic == null && _dislikeIconTransform != null)
		{
			_dislikeIconGraphic = _dislikeIconTransform.GetComponent<Graphic>();
		}

		if (_likeIconGraphic != null)
		{
			_likeIconDefaultColor = _likeIconGraphic.color;
		}

		if (_dislikeIconGraphic != null)
		{
			_dislikeIconDefaultColor = _dislikeIconGraphic.color;
		}

		if (_likeIconTransform != null)
		{
			_likeIconDefaultScale = _likeIconTransform.localScale;
			_likeIconDefaultRotation = _likeIconTransform.localRotation;
			_likeIconDefaultPosition = _likeIconTransform.localPosition;
		}

		if (_dislikeIconTransform != null)
		{
			_dislikeIconDefaultScale = _dislikeIconTransform.localScale;
			_dislikeIconDefaultRotation = _dislikeIconTransform.localRotation;
			_dislikeIconDefaultPosition = _dislikeIconTransform.localPosition;
		}

		if (_likeCounterTransform != null)
		{
			_likeCounterDefaultScale = _likeCounterTransform.localScale;
			_likeCounterDefaultPosition = _likeCounterTransform.localPosition;
		}

		if (_dislikeCounterTransform != null)
		{
			_dislikeCounterDefaultScale = _dislikeCounterTransform.localScale;
			_dislikeCounterDefaultPosition = _dislikeCounterTransform.localPosition;
		}

		_visualDefaultsCaptured = true;
	}

	private void ResetVisualState()
	{
		if (_likeIconGraphic != null)
		{
			_likeIconGraphic.color = _likeIconDefaultColor;
		}

		if (_dislikeIconGraphic != null)
		{
			_dislikeIconGraphic.color = _dislikeIconDefaultColor;
		}

		if (_likeIconTransform != null)
		{
			_likeIconTransform.localScale = _likeIconDefaultScale;
			_likeIconTransform.localRotation = _likeIconDefaultRotation;
			_likeIconTransform.localPosition = _likeIconDefaultPosition;
		}

		if (_dislikeIconTransform != null)
		{
			_dislikeIconTransform.localScale = _dislikeIconDefaultScale;
			_dislikeIconTransform.localRotation = _dislikeIconDefaultRotation;
			_dislikeIconTransform.localPosition = _dislikeIconDefaultPosition;
		}

		if (_likeCounterTransform != null)
		{
			_likeCounterTransform.localScale = _likeCounterDefaultScale;
			_likeCounterTransform.localPosition = _likeCounterDefaultPosition;
		}

		if (_dislikeCounterTransform != null)
		{
			_dislikeCounterTransform.localScale = _dislikeCounterDefaultScale;
			_dislikeCounterTransform.localPosition = _dislikeCounterDefaultPosition;
		}
	}

	private void OnLikeClicked()
	{
		if (viewModel?.LikeCommand == null)
		{
			return;
		}

		_ = viewModel.LikeCommand.ExecuteAsync();
	}

	private void OnDislikeClicked()
	{
		if (viewModel?.DislikeCommand == null)
		{
			return;
		}

		_ = viewModel.DislikeCommand.ExecuteAsync();
	}

	private void SetButtonsInteractable(bool isEnabled)
	{
		if (_likeButton != null)
		{
			_likeButton.interactable = isEnabled;
		}

		if (_dislikeButton != null)
		{
			_dislikeButton.interactable = isEnabled;
		}
	}

	private void UpdateLikesText()
	{
		if (_likesCountText == null)
		{
			return;
		}

		if (!_hasStats)
		{
			UpdateLikesPlaceholder();
			return;
		}

		_likesCountText.text = _currentLikes.ToString();
	}

	private void UpdateDislikesText()
	{
		if (_dislikesCountText == null)
		{
			return;
		}

		if (!_hasStats)
		{
			UpdateDislikesPlaceholder();
			return;
		}

		_dislikesCountText.text = _currentDislikes.ToString();
	}

	private void UpdateLikesPlaceholder()
	{
		if (_likesCountText != null)
		{
			_likesCountText.text = "--";
		}
	}

	private void UpdateDislikesPlaceholder()
	{
		if (_dislikesCountText != null)
		{
			_dislikesCountText.text = "--";
		}
	}

	private void HandleVoteAnimationRequested(VoteAnimationRequest request)
	{
		switch (request.VoteType)
		{
			case GameVoteType.Like:
				PlayLikeAnimation();
				break;
			case GameVoteType.Dislike:
				PlayDislikeAnimation();
				break;
		}
	}

	private void PlayLikeAnimation()
	{
		AnimateIconColor(_likeIconGraphic, _likeHighlightColor, ref _likeIconColorTween);
		AnimateIconColor(_dislikeIconGraphic, _dislikeIconDefaultColor, ref _dislikeIconColorTween);
		AnimateCheerfulIcon();
		AnimateCheerfulCounter();
	}

	private void PlayDislikeAnimation()
	{
		AnimateIconColor(_dislikeIconGraphic, _dislikeHighlightColor, ref _dislikeIconColorTween);
		AnimateIconColor(_likeIconGraphic, _likeIconDefaultColor, ref _likeIconColorTween);
		AnimateCalmIcon();
		AnimateCalmCounter();
	}

	private void AnimateIconColor(Graphic target, Color destination, ref Tween tween)
	{
		if (target == null)
		{
			return;
		}

		tween?.Kill();
		tween = target.DOColor(destination, _iconColorFadeDuration).SetEase(Ease.InOutSine);
	}

	private void AnimateCheerfulIcon()
	{
		if (_likeIconTransform == null)
		{
			return;
		}

		_likeIconAnimTween?.Kill();
		_likeIconTransform.DOKill();

		var sequence = DOTween.Sequence();
		sequence.Append(_likeIconTransform.DOScale(_likeIconDefaultScale * _iconScaleMultiplier, _iconAnimationDuration)
			.SetEase(_animationEase));
		sequence.Join(_likeIconTransform.DOLocalMoveY(_likeIconDefaultPosition.y + _likeIconRiseDistance, _iconAnimationDuration * 0.8f)
			.SetEase(Ease.OutQuad));
		sequence.Join(_likeIconTransform.DOLocalRotate(new Vector3(0f, 0f, 6f), _iconAnimationDuration * 0.8f)
			.SetEase(Ease.InOutSine));
		sequence.Append(_likeIconTransform.DOScale(_likeIconDefaultScale, _iconAnimationDuration * 0.85f)
			.SetEase(Ease.OutBounce));
		sequence.Join(_likeIconTransform.DOLocalMove(_likeIconDefaultPosition, _iconAnimationDuration * 0.85f)
			.SetEase(Ease.OutQuad));
		sequence.Join(_likeIconTransform.DOLocalRotateQuaternion(_likeIconDefaultRotation, _iconAnimationDuration * 0.85f)
			.SetEase(Ease.OutSine));

		_likeIconAnimTween = sequence;
	}

	private void AnimateCheerfulCounter()
	{
		if (_likeCounterTransform == null)
		{
			return;
		}

		_likeCounterAnimTween?.Kill();
		_likeCounterTransform.DOKill();

		_likeCounterTransform.localScale = _likeCounterDefaultScale;
		_likeCounterAnimTween = _likeCounterTransform.DOPunchScale(
			new Vector3(_counterPunchStrength, _counterPunchStrength, 0f),
			_counterPunchDuration,
			2,
			0.6f)
			.SetEase(Ease.OutBack);
	}

	private void AnimateCalmIcon()
	{
		if (_dislikeIconTransform == null)
		{
			return;
		}

		_dislikeIconAnimTween?.Kill();
		_dislikeIconTransform.DOKill();

		var sequence = DOTween.Sequence();
		sequence.Append(_dislikeIconTransform.DOScale(_dislikeIconDefaultScale * 0.94f, _iconAnimationDuration)
			.SetEase(Ease.InOutSine));
		sequence.Join(_dislikeIconTransform.DOLocalMoveY(_dislikeIconDefaultPosition.y - _dislikeIconDipDistance, _iconAnimationDuration)
			.SetEase(Ease.InOutSine));
		sequence.Append(_dislikeIconTransform.DOScale(_dislikeIconDefaultScale, _iconAnimationDuration * 1.1f)
			.SetEase(Ease.OutSine));
		sequence.Join(_dislikeIconTransform.DOLocalMove(_dislikeIconDefaultPosition, _iconAnimationDuration * 1.1f)
			.SetEase(Ease.OutSine));
		sequence.Join(_dislikeIconTransform.DOLocalRotateQuaternion(_dislikeIconDefaultRotation, _iconAnimationDuration * 1.1f)
			.SetEase(Ease.OutSine));

		_dislikeIconAnimTween = sequence;
	}

	private void AnimateCalmCounter()
	{
		if (_dislikeCounterTransform == null)
		{
			return;
		}

		_dislikeCounterAnimTween?.Kill();
		_dislikeCounterTransform.DOKill();

		var sequence = DOTween.Sequence();
		var downPosition = _dislikeCounterDefaultPosition;
		downPosition.y -= _dislikeCounterDipDistance;

		sequence.Append(_dislikeCounterTransform.DOLocalMove(downPosition, _counterPunchDuration)
			.SetEase(Ease.InOutSine));
		sequence.Join(_dislikeCounterTransform.DOScale(_dislikeCounterDefaultScale * 0.95f, _counterPunchDuration)
			.SetEase(Ease.InOutSine));
		sequence.Append(_dislikeCounterTransform.DOLocalMove(_dislikeCounterDefaultPosition, _counterPunchDuration * 1.1f)
			.SetEase(Ease.InOutSine));
		sequence.Join(_dislikeCounterTransform.DOScale(_dislikeCounterDefaultScale, _counterPunchDuration * 1.1f)
			.SetEase(Ease.InOutSine));

		_dislikeCounterAnimTween = sequence;
	}

	private void KillTweens()
	{
		_likeIconColorTween?.Kill();
		_dislikeIconColorTween?.Kill();
		_likeIconAnimTween?.Kill();
		_dislikeIconAnimTween?.Kill();
		_likeCounterAnimTween?.Kill();
		_dislikeCounterAnimTween?.Kill();
		_likeIconColorTween = null;
		_dislikeIconColorTween = null;
		_likeIconAnimTween = null;
		_dislikeIconAnimTween = null;
		_likeCounterAnimTween = null;
		_dislikeCounterAnimTween = null;
	}
}
}
