using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.Core.GameStats;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.Models
{
internal class GameSwiperModel : ModelBase
{
	public IReactiveProperty<GameItemModel> PreviousGame { get; }
	public IReactiveProperty<GameItemModel> CurrentGame { get; }
	public IReactiveProperty<GameItemModel> NextGame { get; }
	public IReactiveProperty<int> CurrentGameIndex { get; }
	public IReactiveProperty<bool> CanGoNext { get; }
	public IReactiveProperty<bool> CanGoPrevious { get; }
	public IReactiveProperty<bool> IsTransitioning { get; }
	public IReactiveProperty<bool> IsLoading { get; }
	public SwiperSettings Settings { get; }

	private readonly Dictionary<int, GameItemModel> _gameItemsCache;

	public GameSwiperModel(
		SwiperSettings settings = null)
	{
		Settings = settings ?? new SwiperSettings();

		PreviousGame = new ReactiveProperty<GameItemModel>(null);
		CurrentGame = new ReactiveProperty<GameItemModel>(null);
		NextGame = new ReactiveProperty<GameItemModel>(null);
		CurrentGameIndex = new ReactiveProperty<int>(0);
		CanGoNext = new ReactiveProperty<bool>(false);
		CanGoPrevious = new ReactiveProperty<bool>(false);
		IsTransitioning = new ReactiveProperty<bool>(false);
		IsLoading = new ReactiveProperty<bool>(false);

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
		PreviousGame?.Dispose();
		CurrentGame?.Dispose();
		NextGame?.Dispose();
		CurrentGameIndex?.Dispose();
		CanGoNext?.Dispose();
		CanGoPrevious?.Dispose();
		IsTransitioning?.Dispose();
		IsLoading?.Dispose();

		_gameItemsCache?.Clear();
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		return default;
	}

	public void SetLoadingState(bool isLoading)
	{
		IsLoading.SetValue(isLoading);
	}

	public void SetTransitionState(bool isTransitioning)
	{
		IsTransitioning.SetValue(isTransitioning);
	}

	public void UpdateCurrentGameIndex(int index)
	{
		CurrentGameIndex.SetValue(index);
	}

	public void UpdateNavigationState(bool hasPreviousGame, bool hasNextGame)
	{
		CanGoPrevious.SetValue(hasPreviousGame);
		CanGoNext.SetValue(hasNextGame);
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
		UpdateSlot(PreviousGame, previous, activeIndex);
		UpdateSlot(CurrentGame, current, activeIndex);
		UpdateSlot(NextGame, next, activeIndex);
	}

	/// <summary>
	/// Clears data for all cached slots.
	/// </summary>
	public void ClearSlots()
	{
		PreviousGame.SetValue(null);
		CurrentGame.SetValue(null);
		NextGame.SetValue(null);
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

	private void UpdateSlot(IReactiveProperty<GameItemModel> slotProperty, GameSlotState state, int activeIndex)
	{
		var gameItem = GetOrCreateGameItem(state.Index);
		gameItem.UpdateRenderTexture(state.RenderTexture);
		gameItem.SetLoadingState(!state.IsReady);
		gameItem.SetActiveState(state.Index == activeIndex);
		gameItem.SetPresentationData(state.PresentationData);

		slotProperty.SetValue(gameItem);
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