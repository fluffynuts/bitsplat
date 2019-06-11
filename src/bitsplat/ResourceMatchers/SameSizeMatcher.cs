using bitsplat.Storage;

namespace bitsplat.ResourceMatchers
{
    public class SameSizeMatcher : IResourceMatcher
    {
        public bool AreMatched(
            IFileResource left, 
            IFileResource right)
        {
            return left.Size == right.Size;
        }
    }

}