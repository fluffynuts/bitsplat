using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public interface IProgressReporter
    {
        void NotifyCurrent(string label, int percentComplete);
        void NotifyOverall(string label, int current, int total);
        void NotifyError(string label);
        void SetMaxLabelLength(int longestName);
        void NotifyPrepare(IFileSystem source, IFileSystem target);
    }
}