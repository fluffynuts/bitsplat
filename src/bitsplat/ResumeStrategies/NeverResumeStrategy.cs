using System.IO;

namespace bitsplat.ResumeStrategies
{
    public class NeverResumeStrategy : IResumeStrategy
    {
        public bool CanResume(Stream source, Stream target)
        {
            return false;
        }
    }
}