using System;
using R3;

namespace Code.Core.Tools
{
    public class ReactiveTrigger : IReadOnlyReactiveTrigger, IDisposable
    {
        private readonly Subject<bool> _subject;

        public ReactiveTrigger()
        {
            _subject = new Subject<bool>();
        }

        public void Dispose()
        {
            _subject.Dispose();
        }

        public IDisposable Subscribe(Action action)
        {
            return _subject.Subscribe(_ => action?.Invoke());
        }

        public IDisposable SubscribeOnce(Action action)
        {
            return _subject.Take(1).Subscribe(_ => action?.Invoke());
        }

        public void Notify()
        {
            _subject.OnNext(true);
        }
    }

    public class ReactiveTrigger<T> : IReadOnlyReactiveTrigger<T>, IDisposable
    {
        private readonly Subject<bool> _subject;
        private T _payload;

        public ReactiveTrigger()
        {
            _subject = new Subject<bool>();
        }

        public void Dispose()
        {
            _subject.Dispose();
        }

        public IDisposable Subscribe(Action<T> action)
        {
            return _subject.Subscribe(_ => action?.Invoke(_payload));
        }

        public IDisposable SubscribeOnce(Action<T> action)
        {
            return _subject.Take(1).Subscribe(_ => action?.Invoke(_payload));
        }

        public void Notify(T payload)
        {
            _payload = payload;
            _subject.OnNext(true);
        }
    }
}