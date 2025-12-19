using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Code.Core.GameStats;
using Code.Core.GameSwiper.MVVM.Models;
using InGameLogger;
using R3;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.ViewModels
{
internal class GameItemViewModel : ViewModelBase<GameItemModel>
{
	public ReadOnlyReactiveProperty<RenderTexture> RenderTexture => model.RenderTexture;
	public ReadOnlyReactiveProperty<bool> IsLoading => model.IsLoading;
	public ReadOnlyReactiveProperty<bool> IsActive => model.IsActive;
	public ReadOnlyReactiveProperty<GamePresentationData?> Presentation => model.PresentationData;

	public ReadOnlyReactiveProperty<bool> IsUIVisible => _isUIVisible;
	public ReadOnlyReactiveProperty<float> UIOpacity => _uIOpacity;
	public ReadOnlyReactiveProperty<bool> ShouldShowLoadingIndicator => _shouldShowLoadingIndicator;

	public int GameIndex => model.Index;
	public GameVotePanelViewModel VotePanelViewModel { get; }

	private const float LoadingIndicatorDelaySeconds = 0.1f;
	private CancellationTokenSource _loadingIndicatorCts;
	private bool _isVotePanelInitialized;
	private readonly ReactiveProperty<bool> _isUIVisible;
	private readonly ReactiveProperty<float> _uIOpacity;
	private readonly ReactiveProperty<bool> _shouldShowLoadingIndicator;

	public GameItemViewModel(GameItemModel model, IGameStatsService gameStatsService, IInGameLogger logger) : base(model)
	{
		_isUIVisible = AddDisposable(new ReactiveProperty<bool>(true));
		_uIOpacity = AddDisposable(new ReactiveProperty<float>(1f));
		_shouldShowLoadingIndicator = AddDisposable(new ReactiveProperty<bool>(false));

		compositeDisposable.AddDisposable(IsUIVisible);
		compositeDisposable.AddDisposable(UIOpacity);
		compositeDisposable.AddDisposable(ShouldShowLoadingIndicator);
		compositeDisposable.AddDisposable(Presentation);

		var votePanelModel = new GameVotePanelModel(model);
		VotePanelViewModel = new GameVotePanelViewModel(votePanelModel, gameStatsService, logger);
		compositeDisposable.AddDisposable(votePanelModel);
		compositeDisposable.AddDisposable(VotePanelViewModel);
	}

	protected override void OnInitialize()
	{
		EnsureVotePanelInitialized();
		compositeDisposable.AddDisposable(model.IsLoading.Subscribe(OnLoadingStateChanged));
	}

	protected override ValueTask OnInitializeAsync(CancellationToken token)
	{
		EnsureVotePanelInitialized();
		compositeDisposable.AddDisposable(model.IsLoading.Subscribe(OnLoadingStateChanged));
		return default;
	}

	protected override void OnDispose()
	{
		_loadingIndicatorCts?.Cancel();
		_loadingIndicatorCts?.Dispose();
		_loadingIndicatorCts = null;
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		_loadingIndicatorCts?.Cancel();
		_loadingIndicatorCts?.Dispose();
		_loadingIndicatorCts = null;

		return default;
	}

	public void UpdateUIVisibility(bool isVisible, float opacity = 1f)
	{
		_isUIVisible.Value = isVisible;
		_uIOpacity.Value = opacity;
	}

	private void OnLoadingStateChanged(bool isLoading)
	{
		UpdateOpacityForLoadingState(isLoading);
		UpdateLoadingIndicatorState(isLoading);
	}

	private void UpdateOpacityForLoadingState(bool isLoading)
	{
		if (isLoading)
		{
			_uIOpacity.Value = 0.5f;
		}
		else if (IsActive.CurrentValue)
		{
			_uIOpacity.Value = 1f;
		}
	}

	private void UpdateLoadingIndicatorState(bool isLoading)
	{
		_loadingIndicatorCts?.Cancel();
		_loadingIndicatorCts?.Dispose();
		_loadingIndicatorCts = null;

		if (!isLoading)
		{
			_shouldShowLoadingIndicator.Value = false;
			return;
		}

		_shouldShowLoadingIndicator.Value = false;

		_loadingIndicatorCts = new CancellationTokenSource();
		_ = ShowLoadingIndicatorWithDelayAsync(_loadingIndicatorCts.Token);
	}

	private async Task ShowLoadingIndicatorWithDelayAsync(CancellationToken token)
	{
		try
		{
			await Task.Delay(TimeSpan.FromSeconds(LoadingIndicatorDelaySeconds), token);
		}
		catch (TaskCanceledException)
		{
			return;
		}

		if (!token.IsCancellationRequested && model.IsLoading.CurrentValue)
		{
			_shouldShowLoadingIndicator.Value = true;
		}
	}

	private void EnsureVotePanelInitialized()
	{
		if (_isVotePanelInitialized || VotePanelViewModel == null)
		{
			return;
		}

		VotePanelViewModel.Initialize();
		_isVotePanelInitialized = true;
	}
}
}