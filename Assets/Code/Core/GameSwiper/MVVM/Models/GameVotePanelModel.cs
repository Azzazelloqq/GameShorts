using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using Azzazelloqq.MVVM.ReactiveLibrary;

namespace Code.Core.GameSwiper.MVVM.Models
{
internal class GameVotePanelModel : ModelBase
{
	public GameVotePanelModel(GameItemModel owner)
	{
		Owner = owner ?? throw new ArgumentNullException(nameof(owner));
		IsBusy = new ReactiveProperty<bool>(false);
	}

	public GameItemModel Owner { get; }
	public IReactiveProperty<bool> IsBusy { get; }

	protected override void OnInitialize()
	{
	}

	protected override ValueTask OnInitializeAsync(CancellationToken token)
	{
		return default;
	}

	protected override void OnDispose()
	{
		IsBusy?.Dispose();
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		IsBusy?.Dispose();

		return default;
	}
}
}

