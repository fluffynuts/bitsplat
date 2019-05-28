using System.IO;

namespace bitsplat
{
    public interface IResumeStrategy
    {
        bool CanResume(Stream source, Stream target);
    }

    public class AlwaysResumeStrategy: IResumeStrategy
    {
        public bool CanResume(Stream source, Stream target)
        {
            return true;
        }
    }
}