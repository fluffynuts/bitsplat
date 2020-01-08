using System;
using System.Collections.Generic;
using System.Linq;

namespace bitsplat.Pipes
{
    public class SimplePercentageConsoleProgressReporter
        : SimpleConsoleProgressReporter
    {
        public const int HISTORY_SIZE = 15;
        public const int MIN_REPORT_WINDOW_MS = 500;
        private DateTime _lastReport = DateTime.MinValue;

        public SimplePercentageConsoleProgressReporter(IMessageWriter messageWriter)
            : base(messageWriter)
        {
            Quiet = false;
            DetailPadding = base.DetailPadding + 28; // guess? we want to put more info in here
        }

        protected override int DetailPadding { get; set; }

        private readonly Queue<NotificationDetails> _notificationDetailHistory =
            new Queue<NotificationDetails>(HISTORY_SIZE + 1);

        private bool TooEarly()
        {
            var now = DateTime.Now;
            if ((now - _lastReport).TotalMilliseconds < MIN_REPORT_WINDOW_MS)
            {
                return true;
            }

            _lastReport = now;
            return false;
        }

        protected override void ReportIntermediateState(
            NotificationDetails details)
        {
            _notificationDetailHistory.Enqueue(details);
            if (_notificationDetailHistory.Count > HISTORY_SIZE)
            {
                _notificationDetailHistory.Dequeue();
            }

            var atStartOrEnd = details.CurrentBytesTransferred > 0 &&
                               details.CurrentBytesTransferred < details.CurrentTotalBytes;

            if (!atStartOrEnd ||
                TooEarly())
            {
                return;
            }

            var last = _notificationDetailHistory.Last();
            var rate = EstimateRate();
            var itemEtr = EstimateItemTimeRemaining(last, rate);
            var totalEtr = EstimateTotalTimeRemaining(last, rate);
            var readableRate = HumanReadableRateFor(rate);

            Rewrite(
                details.Label,
                string.Join(" · ",
                    new[]
                    {
                        readableRate,
                        itemEtr,
                        totalEtr,
                        BrailleProgressBarGenerator.CompositeProgressBarFor(
                            details.CurrentPercentageCompleteBySize,
                            details.TotalPercentageCompleteBySize
                        ) + " "
                    }.Where(s => s != "")
                )
            );
        }

        private double EstimateRate()
        {
            if (_notificationDetailHistory.Count < 2)
            {
                return 0;
            }

            var last = _notificationDetailHistory.Last();
            var first = _notificationDetailHistory.First();
            var deltaBytes = last.CurrentBytesTransferred - first.CurrentBytesTransferred;
            var deltaTime = last.Timestamp - first.Timestamp;
            return deltaBytes / deltaTime.TotalSeconds;
        }

        private string EstimateItemTimeRemaining(
            NotificationDetails last,
            double rate)
        {
            var bytesRemaining = last.CurrentTotalBytes - last.CurrentBytesTransferred;
            var secondsRemaining = (int) Math.Round(bytesRemaining / rate);
            return HumanReadableTimeFor(secondsRemaining);
        }

        private string EstimateTotalTimeRemaining(
            NotificationDetails last,
            double rate)
        {
            var bytesRemaining = last.TotalBytes - last.TotalBytesTransferred;
            var secondsRemaining = (int) Math.Round(bytesRemaining / rate);
            return HumanReadableTimeFor(secondsRemaining);
        }

    }
}