using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.Core.GameStats;
using Code.Core.GameSwiper.MVVM.Models;
using InGameLogger;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.ViewModels
{
internal class GameItemViewModel : ViewModelBase<GameItemModel>
{
	public IReadOnlyReactiveProperty<RenderTexture> RenderTexture => model.RenderTexture;
	public IReadOnlyReactiveProperty<bool> IsLoading => model.IsLoading;
	public IReadOnlyReactiveProperty<bool> IsActive => model.IsActive;
	public IReadOnlyReactiveProperty<GamePresentationData?> Presentation => model.PresentationData;

	public IReactiveProperty<bool> IsUIVisible { get; }
	public IReactiveProperty<float> UIOpacity { get; }
	public IReactiveProperty<bool> ShouldShowLoadingIndicator { get; }

	public int GameIndex => model.Index;
	public GameVotePanelViewModel VotePanelViewModel { get; }

	private const float LoadingIndicatorDelaySeconds = 0.1f;
	private CancellationTokenSource _loadingIndicatorCts;
	private bool _isVotePanelInitialized;

	public GameItemViewModel(GameItemModel model, IGameStatsService gameStatsService, IInGameLogger logger) : base(model)
	{
		IsUIVisible = new ReactiveProperty<bool>(true);
		UIOpacity = new ReactiveProperty<float>(1f);
		ShouldShowLoadingIndicator = new ReactiveProperty<bool>(false);

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
		IsUIVisible.SetValue(isVisible);
		UIOpacity.SetValue(opacity);
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
			UIOpacity.SetValue(0.5f);
		}
		else if (IsActive.Value)
		{
			UIOpacity.SetValue(1f);
		}
	}

	private void UpdateLoadingIndicatorState(bool isLoading)
	{
		_loadingIndicatorCts?.Cancel();
		_loadingIndicatorCts?.Dispose();
		_loadingIndicatorCts = null;

		if (!isLoading)
		{
			ShouldShowLoadingIndicator.SetValue(false);
			return;
		}

		ShouldShowLoadingIndicator.SetValue(false);

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

		if (!token.IsCancellationRequested && model.IsLoading.Value)
		{
			ShouldShowLoadingIndicator.SetValue(true);
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