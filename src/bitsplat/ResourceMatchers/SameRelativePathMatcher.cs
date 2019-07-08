using bitsplat.Storage;

namespace bitsplat.ResourceMatchers
{
    public class SameRelativePathMatcher : IResourceMatcher
    {
        public bool AreMatched(
            IReadWriteFileResource left,
            IReadWriteFileResource right)
        {
            return left.RelativePath == right.RelativePath;
        }
    }
}