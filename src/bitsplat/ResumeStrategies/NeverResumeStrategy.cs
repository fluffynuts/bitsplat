using System.IO;
using bitsplat.Storage;

namespace bitsplat.ResumeStrategies
{
    public class NeverResumeStrategy : IResumeStrategy
    {
        public bool CanResume(IFileResource sourceResource,
            IFileResource targetResource,
            Stream source,
            Stream target)
        {
            return false;
        }
    }
}