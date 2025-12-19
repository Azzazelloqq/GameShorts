using System;
using System.Threading;
using System.Threading.Tasks;
using Azzazelloqq.MVVM.Core;
using R3;

namespace Code.Core.GameSwiper.MVVM.Models
{
internal class GameVotePanelModel : ModelBase
{
	public GameItemModel Owner { get; }
	public ReadOnlyReactiveProperty<bool> IsBusy => _isBusy;
	
	private ReactiveProperty<bool> _isBusy;

	public GameVotePanelModel(GameItemModel owner)
	{
		Owner = owner ?? throw new ArgumentNullException(nameof(owner));
		_isBusy = new ReactiveProperty<bool>(false);
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
		IsBusy?.Dispose();
	}

	protected override ValueTask OnDisposeAsync(CancellationToken token)
	{
		IsBusy?.Dispose();

		return default;
	}

	public void MakeBusy()
	{
		_isBusy.Value = true;
	}

	public void MakeNotBusy()
	{
		_isBusy.Value = false;
	}
}
}







