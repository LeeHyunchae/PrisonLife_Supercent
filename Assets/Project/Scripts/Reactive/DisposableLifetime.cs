using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrisonLife.Reactive
{
    public class DisposableLifetime : MonoBehaviour
    {
        private readonly List<IDisposable> tracked = new();

        public void Add(IDisposable _disposable)
        {
            if (_disposable == null) return;
            tracked.Add(_disposable);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < tracked.Count; i++)
            {
                try { tracked[i]?.Dispose(); }
                catch (Exception ex) { Debug.LogException(ex); }
            }
            tracked.Clear();
        }
    }

    public static class DisposableExtensions
    {
        public static IDisposable AddTo(this IDisposable _disposable, GameObject _gameObject)
        {
            if (_disposable == null) return null;
            if (_gameObject == null) { _disposable.Dispose(); return _disposable; }

            DisposableLifetime lifetime = _gameObject.GetComponent<DisposableLifetime>();
            if (lifetime == null) lifetime = _gameObject.AddComponent<DisposableLifetime>();
            lifetime.Add(_disposable);
            return _disposable;
        }

        public static IDisposable AddTo(this IDisposable _disposable, Component _component)
        {
            if (_component == null) { _disposable?.Dispose(); return _disposable; }
            return _disposable.AddTo(_component.gameObject);
        }
    }
}
