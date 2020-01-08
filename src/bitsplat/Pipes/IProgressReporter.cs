using System;
using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public class NotificationDetails
    {
        public string Label { get; set; }
        public long CurrentBytesTransferred { get; set; }
        public long CurrentTotalBytes { get; set; }
        public long TotalBytesTransferred { get; set; }
        public long TotalBytes { get; set; }
        public int CurrentItem { get; set; }
        public int TotalItems { get; set; }
        public Exception Exception { get; set; }
        public DateTime Timestamp { get; } = DateTime.Now;

        public int CurrentPercentageCompleteBySize =>
            Percentage(CurrentBytesTransferred, CurrentTotalBytes);

        public int TotalPercentageCompleteBySize =>
            Percentage(TotalBytesTransferred, TotalBytes);

        public int PercentageCompleteByItems =>
            Percentage(CurrentItem, TotalItems);

        private int Percentage(
            long enumerator,
            long denominator)
        {
            return BoundValue(
                0,
                100,
                (int) Math.Floor((100M * enumerator) / denominator)
            );
        }

        private int BoundValue(
            int min,
            int max,
            int current)
        {
            if (current > max)
            {
                return max;
            }

            return current < min
                       ? min
                       : current;
        }
    }

    public interface IProgressReporter
    {
        public bool Quiet { get; set; }
        
        void NotifyCurrent(NotificationDetails details);
        void NotifyOverall(NotificationDetails details);
        void NotifyError(NotificationDetails details);
        void SetMaxLabelLength(int longestName);

        void NotifyPrepare(
            string operation,
            IFileSystem source,
            IFileSystem target
        );

        void NotifyNoWork(
            IFileSystem source,
            IFileSystem target
        );
        
        T Bookend<T>(
            string message,
            Func<T> toRun
        );

        void Bookend(
            string message,
            Action toRun
        );
        
    }
}