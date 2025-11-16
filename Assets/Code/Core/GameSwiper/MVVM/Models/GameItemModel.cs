using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;
using Code.Core.GameStats;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.Models
{
internal class GameItemModel : ModelBase
{
	public IReactiveProperty<RenderTexture> RenderTexture { get; }
	public IReactiveProperty<bool> IsLoading { get; }
	public IReactiveProperty<bool> IsActive { get; }
	public IReactiveProperty<GamePresentationData?> PresentationData { get; }
	public int Index { get; }

	public GameItemModel(int index)
	{
		Index = index;
		RenderTexture = new ReactiveProperty<RenderTexture>(null);
		IsLoading = new ReactiveProperty<bool>(false);
		IsActive = new ReactiveProperty<bool>(false);
		PresentationData = new ReactiveProperty<GamePresentationData?>(null);
	}

	public void UpdateRenderTexture(RenderTexture texture)
	{
		RenderTexture.SetValue(texture);
	}

	public void SetLoadingState(bool isLoading)
	{
		IsLoading.SetValue(isLoading);
	}

	public void SetActiveState(bool isActive)
	{
		IsActive.SetValue(isActive);
	}

	public void SetPresentationData(GamePresentationData? data)
	{
		PresentationData.SetValue(data);
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
		RenderTexture?.Dispose();
		IsLoading?.Dispose();
		IsActive?.Dispose();
		PresentationData?.Dispose();
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		RenderTexture?.Dispose();
		IsLoading?.Dispose();
		IsActive?.Dispose();
		PresentationData?.Dispose();

		return default;
	}
}
}