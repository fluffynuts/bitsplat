using System.IO;

namespace bitsplat.ResumeStrategies
{
    public class AlwaysResumeStrategy: IResumeStrategy
    {
        public bool CanResume(Stream source, Stream target)
        {
            return true;
        }
    }
}