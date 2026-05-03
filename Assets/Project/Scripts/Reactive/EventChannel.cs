using System;
using System.Collections.Generic;

namespace PrisonLife.Reactive
{
    public class EventChannel<T>
    {
        private readonly List<Action<T>> subscribers = new();

        public IDisposable Subscribe(Action<T> _handler)
        {
            if (_handler == null) throw new ArgumentNullException(nameof(_handler));
            subscribers.Add(_handler);
            return new Subscription(this, _handler);
        }

        public void Raise(T _payload)
        {
            Action<T>[] snapshot = subscribers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke(_payload);
            }
        }

        private void Unsubscribe(Action<T> _handler)
        {
            subscribers.Remove(_handler);
        }

        private sealed class Subscription : IDisposable
        {
            private EventChannel<T> owner;
            private Action<T> handler;

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
        private readonly List<Action> subscribers = new();

        public IDisposable Subscribe(Action _handler)
        {
            if (_handler == null) throw new ArgumentNullException(nameof(_handler));
            subscribers.Add(_handler);
            return new Subscription(this, _handler);
        }

        public void Raise()
        {
            Action[] snapshot = subscribers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke();
            }
        }

        private void Unsubscribe(Action _handler)
        {
            subscribers.Remove(_handler);
        }

        private sealed class Subscription : IDisposable
        {
            private EventChannel owner;
            private Action handler;

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
