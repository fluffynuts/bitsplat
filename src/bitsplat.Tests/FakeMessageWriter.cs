using System;
using bitsplat.Pipes;
using bitsplat.Storage;

namespace bitsplat.Tests
{
    public class FakeProgressReporter : IProgressReporter
    {
        public bool Quiet { get; set; }
        public void NotifyCurrent(NotificationDetails details)
        {
        }

        public void NotifyOverall(NotificationDetails details)
        {
        }

        public void NotifyError(NotificationDetails details)
        {
        }

        public void Log(string info)
        {
        }

        public void SetMaxLabelLength(int longestName)
        {
        }

        public void NotifyPrepare(string operation,
            IFileSystem source,
            IFileSystem target)
        {
        }

        public void NotifyNoWork(IFileSystem source, IFileSystem target)
        {
        }

        public void Write(string message)
        {
        }

        public void Rewrite(string message)
        {
        }

        public T Bookend<T>(string message, Func<T> toRun)
        {
            return toRun();
        }

        public void Bookend(string message, Action toRun)
        {
            toRun();
        }
    }
}