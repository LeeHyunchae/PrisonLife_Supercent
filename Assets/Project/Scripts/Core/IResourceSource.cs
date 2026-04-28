using UnityEngine;

namespace PrisonLife.Core
{
    public interface IResourceSource
    {
        ResourceType OutputType { get; }
        int AvailableCount { get; }
        Vector3 DispenseOriginPosition { get; }
        bool TryDispenseOne();
    }
}
