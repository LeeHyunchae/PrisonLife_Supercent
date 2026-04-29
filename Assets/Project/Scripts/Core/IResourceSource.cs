namespace PrisonLife.Core
{
    public interface IResourceSource
    {
        ResourceType OutputType { get; }
        bool HasAvailable();
        bool TryProvideOne();
    }
}
