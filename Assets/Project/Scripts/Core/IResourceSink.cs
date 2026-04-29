namespace PrisonLife.Core
{
    public interface IResourceSink
    {
        ResourceType InputType { get; }
        bool CanAcceptOne();
        bool TryAcceptOne();
    }
}
