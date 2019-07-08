using bitsplat.Storage;

namespace bitsplat.ResourceMatchers
{
    public interface IResourceMatcher
    {
        bool AreMatched(
            IReadWriteFileResource left,
            IReadWriteFileResource right
        );
    }
}