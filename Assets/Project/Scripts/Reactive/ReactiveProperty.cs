using System;
using System.Collections.Generic;

namespace PrisonLife.Reactive
{
    public class ReactiveProperty<T>
    {
        T currentValue;
        readonly List<Action<T>> subscribers = new();
        readonly IEqualityComparer<T> comparer;

        public ReactiveProperty(T _initialValue = default, IEqualityComparer<T> _comparer = null)
        {
            currentValue = _initialValue;
            comparer = _comparer ?? EqualityComparer<T>.Default;
        }

        public T Value
        {
            get => currentValue;
            set
            {
                if (comparer.Equals(currentValue, value)) return;
                currentValue = value;
                NotifySubscribers();
            }
        }

        public void ForceNotify()
        {
            NotifySubscribers();
        }

        public IDisposable Subscribe(Action<T> _onChanged)
        {
            return SubscribeInternal(_onChanged, _invokeImmediately: true);
        }

        public IDisposable SubscribeOnChange(Action<T> _onChanged)
        {
            return SubscribeInternal(_onChanged, _invokeImmediately: false);
        }

        IDisposable SubscribeInternal(Action<T> _onChanged, bool _invokeImmediately)
        {
            if (_onChanged == null) throw new ArgumentNullException(nameof(_onChanged));
            subscribers.Add(_onChanged);
            if (_invokeImmediately) _onChanged(currentValue);
            return new Subscription(this, _onChanged);
        }

        void Unsubscribe(Action<T> _handler)
        {
            subscribers.Remove(_handler);
        }

        void NotifySubscribers()
        {
            var snapshot = subscribers.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                snapshot[i]?.Invoke(currentValue);
            }
        }

        sealed class Subscription : IDisposable
        {
            ReactiveProperty<T> owner;
            Action<T> handler;

            public Subscription(ReactiveProperty<T> _owner, Action<T> _handler)
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
