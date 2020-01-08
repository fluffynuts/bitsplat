using System.IO;
using bitsplat.Storage;

namespace bitsplat.ResumeStrategies
{
    public class AlwaysResumeWhenTargetSmallerStrategy: IResumeStrategy
    {
        public bool CanResume(
            IFileResource sourceResource,
            IFileResource targetResource,
            Stream source,
            Stream target)
        {
            return targetResource.Size < sourceResource.Size;
        }
    }
}