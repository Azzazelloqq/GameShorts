using System;
using R3;

namespace Code.Core.Tools
{
public static class ReactiveExtensions
{
	public static IDisposable DelayedCall(float delaySec, Action action)
	{
		if (delaySec <= 0)
		{
			action?.Invoke();
			return null;
		}

		return Observable.Timer(TimeSpan.FromSeconds(delaySec)).Take(1).Subscribe(_ => action?.Invoke());
	}

	public static IDisposable Timer(int sec, Action everyTick, Action finallyAction)
	{
		everyTick.Invoke();
		return Observable.Interval(TimeSpan.FromSeconds(1)).Take(sec).Do(onDispose: finallyAction).Subscribe(_ =>
		{
			everyTick.Invoke();
		});
	}

	public static IDisposable DelayedFinallyCall(float delaySec, Action action)
	{
		if (delaySec <= 0)
		{
			action?.Invoke();
			return null;
		}

		var executed = false;
		return Observable.Timer(TimeSpan.FromSeconds(delaySec)).Take(1).Do(onDispose: () =>
		{
			if (executed)
			{
				return;
			}

			action?.Invoke();
		}).Subscribe(_ =>
		{
			executed = true;
			action?.Invoke();
		});
	}

	public static IDisposable DelayFrame(int countFrame, Action action)
	{
		return Observable.TimerFrame(countFrame).Subscribe(_ => action?.Invoke());
	}
}
}