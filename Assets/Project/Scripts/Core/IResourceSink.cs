using UnityEngine;

namespace PrisonLife.Core
{
    public interface IResourceSink
    {
        ResourceType InputType { get; }
        int StoredCount { get; }
        int StorageCapacity { get; }
        Vector3 ReceiveTargetPosition { get; }
        bool CanAcceptOne();
        void AcceptOne();
    }
}
