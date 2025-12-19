using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Code.Core.GameStats;
using R3;
using UnityEngine;

namespace Code.Core.GameSwiper.MVVM.Models
{
internal class GameItemModel : ModelBase
{
	public ReadOnlyReactiveProperty<RenderTexture> RenderTexture => _renderTexture;
	public ReadOnlyReactiveProperty<bool> IsLoading => _isLoading;
	public ReadOnlyReactiveProperty<bool> IsActive => _isActive;
	public ReadOnlyReactiveProperty<GamePresentationData?> PresentationData => _presentationData;
	public int Index { get; }
	
	private readonly ReactiveProperty<GamePresentationData?> _presentationData;
	private readonly ReactiveProperty<bool> _isActive;
	private readonly ReactiveProperty<bool> _isLoading;
	private readonly ReactiveProperty<RenderTexture> _renderTexture;

	public GameItemModel(int index)
	{
		Index = index;
		_renderTexture = AddDisposable(new ReactiveProperty<RenderTexture>(null));
		_isLoading = AddDisposable(new ReactiveProperty<bool>(false));
		_isActive = AddDisposable(new ReactiveProperty<bool>(false));
		_presentationData = AddDisposable(new ReactiveProperty<GamePresentationData?>(null));
	}

	public void UpdateRenderTexture(RenderTexture texture)
	{
		_renderTexture.Value = texture;
	}

	public void SetLoadingState(bool isLoading)
	{
		_isLoading.Value = isLoading;
	}

	public void SetActiveState(bool isActive)
	{
		_isActive.Value = isActive;
	}

	public void SetPresentationData(GamePresentationData? data)
	{
		_presentationData.Value = data;
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
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		return default;
	}
}
}