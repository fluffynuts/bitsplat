using bitsplat.Storage;

namespace bitsplat.ResourceMatchers
{
    public class SameRelativePathMatcher : IResourceMatcher
    {
        public bool AreMatched(
            IFileResource left,
            IFileResource right)
        {
            return left.RelativePath == right.RelativePath;
        }
    }
}