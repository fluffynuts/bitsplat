using System.IO;

namespace bitsplat.ResumeStrategies
{
    public interface IResumeStrategy
    {
        bool CanResume(Stream source, Stream target);
    }
}