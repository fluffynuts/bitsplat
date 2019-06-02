using bitsplat.Storage;

namespace bitsplat.ResourceMatchers
{
    public interface IResourceMatcher
    {
        bool AreMatched(
            IFileResource left,
            IFileResource right
        );
    }
}