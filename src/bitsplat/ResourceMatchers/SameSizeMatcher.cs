using bitsplat.Storage;

namespace bitsplat.ResourceMatchers
{
    public class SameSizeMatcher : IResourceMatcher
    {
        public bool AreMatched(
            IReadWriteFileResource left, 
            IReadWriteFileResource right)
        {
            return left.Size == right.Size;
        }
    }

}