using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace PrisonLife.Core
{
    public interface IMover
    {
        Vector3 CurrentPosition { get; }
        bool HasArrivedAtDestination { get; }
        void SetVelocity(Vector3 _velocityWorldSpace);
        void StopImmediately();
        UniTask MoveToAsync(Vector3 _targetWorldPosition, CancellationToken _cancellationToken = default);
    }
}
