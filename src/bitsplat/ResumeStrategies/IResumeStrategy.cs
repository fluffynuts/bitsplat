using System.IO;
using bitsplat.Storage;

namespace bitsplat.ResumeStrategies
{
    public interface IResumeStrategy
    {
        bool CanResume(
            IFileResource sourceResource,
            IFileResource targetResource,
            Stream source, 
            Stream target);
    }
}