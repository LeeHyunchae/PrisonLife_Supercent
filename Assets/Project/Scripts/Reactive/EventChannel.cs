using System;
using System.Collections.Generic;

namespace PrisonLife.Reactive
{
    public class EventChannel<T>
    {
        readonly List<Action<T>> subscribers = new();

        public IDisposable Subscribe(Action<T> _handler)
        {
            if (_handler == null) throw new ArgumentNullException(nameof(_handler));
            subscribers.Add(_handler);
            return new Subscription(this, _handler);
        }

        public void Raise(T _payload)
        {
            var snapshot = subscribers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke(_payload);
            }
        }

        void Unsubscribe(Action<T> _handler)
        {
            subscribers.Remove(_handler);
        }

        sealed class Subscription : IDisposable
        {
            EventChannel<T> owner;
            Action<T> handler;

            public Subscription(EventChannel<T> _owner, Action<T> _handler)
            {
                owner = _owner;
                handler = _handler;
            }

            public void Dispose()
            {
                owner?.Unsubscribe(handler);
                owner = null;
                handler = null;
            }
        }
    }

    public class EventChannel
    {
        readonly List<Action> subscribers = new();

        public IDisposable Subscribe(Action _handler)
        {
            if (_handler == null) throw new ArgumentNullException(nameof(_handler));
            subscribers.Add(_handler);
            return new Subscription(this, _handler);
        }

        public void Raise()
        {
            var snapshot = subscribers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke();
            }
        }

        void Unsubscribe(Action _handler)
        {
            subscribers.Remove(_handler);
        }

        sealed class Subscription : IDisposable
        {
            EventChannel owner;
            Action handler;

            public Subscription(EventChannel _owner, Action _handler)
            {
                owner = _owner;
                handler = _handler;
            }

            public void Dispose()
            {
                owner?.Unsubscribe(handler);
                owner = null;
                handler = null;
            }
        }
    }
}
