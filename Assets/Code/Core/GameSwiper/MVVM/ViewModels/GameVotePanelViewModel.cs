using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.Core.GameStats;
using Code.Core.GameSwiper.MVVM.Models;
using InGameLogger;

namespace Code.Core.GameSwiper.MVVM.ViewModels
{
internal class GameVotePanelViewModel : ViewModelBase<GameVotePanelModel>
{
	private readonly IGameStatsService _gameStatsService;
	private readonly IInGameLogger _logger;

	private readonly ReactiveProperty<int> _likesCount;
	private readonly ReactiveProperty<int> _dislikesCount;
	private readonly ReactiveProperty<float> _likeRatio;
	private readonly ReactiveProperty<bool> _hasStats;
	private readonly ReactiveProperty<bool> _isInteractable;

	private GamePresentationData? _currentPresentation;

	public GameVotePanelViewModel(
		GameVotePanelModel model,
		IGameStatsService gameStatsService,
		IInGameLogger logger) : base(model)
	{
		_gameStatsService = gameStatsService ?? throw new ArgumentNullException(nameof(gameStatsService));
		_logger = logger;
		_likesCount = new ReactiveProperty<int>(0);
		_dislikesCount = new ReactiveProperty<int>(0);
		_likeRatio = new ReactiveProperty<float>(0f);
		_hasStats = new ReactiveProperty<bool>(false);
		_isInteractable = new ReactiveProperty<bool>(false);

		compositeDisposable.AddDisposable(_likesCount);
		compositeDisposable.AddDisposable(_dislikesCount);
		compositeDisposable.AddDisposable(_likeRatio);
		compositeDisposable.AddDisposable(_hasStats);
		compositeDisposable.AddDisposable(_isInteractable);
	}

	public IReadOnlyReactiveProperty<int> LikesCount => _likesCount;
	public IReadOnlyReactiveProperty<int> DislikesCount => _dislikesCount;
	public IReadOnlyReactiveProperty<float> LikeRatio => _likeRatio;
	public IReadOnlyReactiveProperty<bool> HasStats => _hasStats;
	public IReadOnlyReactiveProperty<bool> IsInteractable => _isInteractable;
	public IReactiveProperty<bool> IsBusy => model.IsBusy;

	public IActionAsyncCommand LikeCommand { get; private set; }
	public IActionAsyncCommand DislikeCommand { get; private set; }

	public event Action<VoteAnimationRequest> VoteAnimationRequested;

	protected override void OnInitialize()
	{
		InitializeCommands();
		BindModel();
		RefreshState();
	}

	protected override ValueTask OnInitializeAsync(CancellationToken token)
	{
		InitializeCommands();
		BindModel();
		RefreshState();
		return default;
	}

	protected override void OnDispose()
	{
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		return default;
	}

	private void InitializeCommands()
	{
		if (LikeCommand != null)
		{
			return;
		}

		LikeCommand = new ActionAsyncCommand(() => SubmitVoteAsync(GameVoteType.Like), CanVote);
		DislikeCommand = new ActionAsyncCommand(() => SubmitVoteAsync(GameVoteType.Dislike), CanVote);

		compositeDisposable.AddDisposable(LikeCommand);
		compositeDisposable.AddDisposable(DislikeCommand);
	}

	private void BindModel()
	{
		compositeDisposable.AddDisposable(model.Owner.PresentationData.Subscribe(OnPresentationChanged));
		compositeDisposable.AddDisposable(model.Owner.IsActive.Subscribe(_ => UpdateInteractableState()));
		compositeDisposable.AddDisposable(model.IsBusy.Subscribe(_ => UpdateInteractableState()));
	}

	private void RefreshState()
	{
		OnPresentationChanged(model.Owner.PresentationData.Value);
		UpdateInteractableState();
	}

	private void OnPresentationChanged(GamePresentationData? presentation)
	{
		_currentPresentation = presentation;
		if (presentation.HasValue)
		{
			var stats = presentation.Value.StatsData;
			_likesCount.SetValue(stats.Likes);
			_dislikesCount.SetValue(stats.Dislikes);
			_likeRatio.SetValue(stats.LikeRatio);
			_hasStats.SetValue(true);
		}
		else
		{
			_likesCount.SetValue(0);
			_dislikesCount.SetValue(0);
			_likeRatio.SetValue(0f);
			_hasStats.SetValue(false);
		}

		UpdateInteractableState();
	}

	private bool CanVote()
	{
		return _currentPresentation.HasValue && !model.IsBusy.Value && _isInteractable.Value;
	}

	private void UpdateInteractableState()
	{
		var canInteract = model.Owner.IsActive.Value && _currentPresentation.HasValue && !model.IsBusy.Value;
		_isInteractable.SetValue(canInteract);
	}

	private async Task SubmitVoteAsync(GameVoteType voteType)
	{
		if (!CanVote())
		{
			return;
		}

		var presentation = _currentPresentation;
		if (!presentation.HasValue)
		{
			return;
		}

		model.IsBusy.SetValue(true);
		UpdateInteractableState();

		try
		{
			var gameType = presentation.Value.GameType;
			await _gameStatsService.SubmitVoteAsync(gameType, voteType, CancellationToken.None);
			var updatedStats = await _gameStatsService.GetStatsAsync(gameType, CancellationToken.None);

			var refreshedPresentation = new GamePresentationData(
				gameType,
				presentation.Value.DisplayName,
				updatedStats);

			model.Owner.SetPresentationData(refreshedPresentation);
			VoteAnimationRequested?.Invoke(new VoteAnimationRequest(voteType, updatedStats));
		}
		catch (Exception ex)
		{
			_logger?.LogError($"Failed to submit {voteType} vote: {ex.Message}");
		}
		finally
		{
			model.IsBusy.SetValue(false);
			UpdateInteractableState();
		}
	}
}

internal readonly struct VoteAnimationRequest
{
	public VoteAnimationRequest(GameVoteType voteType, GameStatsData statsData)
	{
		VoteType = voteType;
		StatsData = statsData;
	}

	public GameVoteType VoteType { get; }
	public GameStatsData StatsData { get; }
}
}
