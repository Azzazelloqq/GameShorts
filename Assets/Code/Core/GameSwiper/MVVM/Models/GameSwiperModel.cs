using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Code.Core.GameStats;
using R3;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.Models
{
internal class GameSwiperModel : ModelBase
{
	public ReadOnlyReactiveProperty<GameItemModel> PreviousGame => _previousGame;
	public ReadOnlyReactiveProperty<GameItemModel> CurrentGame => _currentGame;
	public ReadOnlyReactiveProperty<GameItemModel> NextGame => _nextGame;
	public ReadOnlyReactiveProperty<int> CurrentGameIndex => _currentGameIndex;
	public ReadOnlyReactiveProperty<bool> CanGoNext => _canGoNext;
	public ReadOnlyReactiveProperty<bool> CanGoPrevious => _canGoPrevious;
	public ReadOnlyReactiveProperty<bool> IsTransitioning => _isTransitioning;
	public ReadOnlyReactiveProperty<bool> IsLoading => _isLoading;
	public SwiperSettings Settings { get; }

	private readonly Dictionary<int, GameItemModel> _gameItemsCache;
	private readonly ReactiveProperty<GameItemModel> _previousGame;
	private readonly ReactiveProperty<GameItemModel> _currentGame;
	private readonly ReactiveProperty<GameItemModel> _nextGame;
	private readonly ReactiveProperty<int> _currentGameIndex;
	private readonly ReactiveProperty<bool> _canGoNext;
	private readonly ReactiveProperty<bool> _canGoPrevious;
	private readonly ReactiveProperty<bool> _isTransitioning;
	private readonly ReactiveProperty<bool> _isLoading;

	public GameSwiperModel(
		SwiperSettings settings = null)
	{
		Settings = settings ?? new SwiperSettings();

		_previousGame = AddDisposable(new ReactiveProperty<GameItemModel>(null));
		_currentGame = AddDisposable(new ReactiveProperty<GameItemModel>(null));
		_nextGame = AddDisposable(new ReactiveProperty<GameItemModel>(null));
		_currentGameIndex = AddDisposable(new ReactiveProperty<int>(0));
		_canGoNext = AddDisposable(new ReactiveProperty<bool>(false));
		_canGoPrevious = AddDisposable(new ReactiveProperty<bool>(false));
		_isTransitioning = AddDisposable(new ReactiveProperty<bool>(false));
		_isLoading = AddDisposable(new ReactiveProperty<bool>(false));

		_gameItemsCache = new Dictionary<int, GameItemModel>();
	}


	protected override void OnInitialize()
	{
	}

	protected override ValueTask OnInitializeAsync(CancellationToken token)
	{
		return default;
	}

	protected override void OnDispose()
	{
		_gameItemsCache?.Clear();
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		return default;
	}

	public void SetLoadingState(bool isLoading)
	{
		_isLoading.Value = isLoading;
	}

	public void SetTransitionState(bool isTransitioning)
	{
		_isTransitioning.Value = isTransitioning;
	}

	public void UpdateCurrentGameIndex(int index)
	{
		_currentGameIndex.Value = index;
	}

	public void UpdateNavigationState(bool hasPreviousGame, bool hasNextGame)
	{
		_canGoPrevious.Value = hasPreviousGame;
		
		_canGoNext.Value = hasNextGame;
	}

	/// <summary>
	/// Updates the cached slot models that represent the previous, current, and next games.
	/// </summary>
	/// <param name="previous">State for the previous game slot.</param>
	/// <param name="current">State for the current game slot.</param>
	/// <param name="next">State for the next game slot.</param>
	/// <param name="activeIndex">Index of the active game.</param>
	public void UpdateGameSlots(GameSlotState previous, GameSlotState current, GameSlotState next, int activeIndex)
	{
		UpdateSlot(_previousGame, previous, activeIndex);
		UpdateSlot(_currentGame, current, activeIndex);
		UpdateSlot(_nextGame, next, activeIndex);
	}

	private GameItemModel GetOrCreateGameItem(int index)
	{
		if (!_gameItemsCache.ContainsKey(index))
		{
			var gameItem = new GameItemModel(index);
			_gameItemsCache[index] = gameItem;
			compositeDisposable.AddDisposable(gameItem);
		}

		return _gameItemsCache[index];
	}

	/// <summary>
	/// Updates active state flags for all cached games.
	/// </summary>
	/// <param name="activeIndex">Index that should be marked as active.</param>
	public void UpdateActiveStates(int activeIndex)
	{
		foreach (var game in _gameItemsCache.Values)
		{
			game.SetActiveState(game.Index == activeIndex);
		}
	}

	private void UpdateSlot(ReactiveProperty<GameItemModel> slotProperty, GameSlotState state, int activeIndex)
	{
		var gameItem = GetOrCreateGameItem(state.Index);
		gameItem.UpdateRenderTexture(state.RenderTexture);
		gameItem.SetLoadingState(!state.IsReady);
		gameItem.SetActiveState(state.Index == activeIndex);
		gameItem.SetPresentationData(state.PresentationData);

		slotProperty.Value = gameItem;
	}
}

public class SwiperSettings
{
	public float AnimationDuration { get; set; } = 0.3f;
	public bool UseScreenHeight { get; set; } = true;
	public float ImageSpacing { get; set; } = 1920f;
}

public readonly struct GameSlotState
{
	public GameSlotState(int index, RenderTexture renderTexture, bool isReady,
		GamePresentationData? presentationData = null)
	{
		Index = index;
		RenderTexture = renderTexture;
		IsReady = isReady;
		PresentationData = presentationData;
	}

	public int Index { get; }
	public RenderTexture RenderTexture { get; }
	public bool IsReady { get; }
	public GamePresentationData? PresentationData { get; }
}
}