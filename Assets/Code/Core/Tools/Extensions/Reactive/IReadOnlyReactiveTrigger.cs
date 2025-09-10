using System;
namespace Root.Extensions.Reactive
{
  public interface IReadOnlyReactiveTrigger
  {
    IDisposable Subscribe(Action action);
    IDisposable SubscribeOnce(Action action);
  }
  
  public interface IReadOnlyReactiveTrigger<T>
  {
    IDisposable Subscribe(Action<T> action);
    IDisposable SubscribeOnce(Action<T> action);
  }
}