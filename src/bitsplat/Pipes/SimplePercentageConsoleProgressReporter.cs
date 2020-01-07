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
                        )
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

        private string HumanReadableRateFor(double rate)
        {
            var suffix = 0;
            while (rate > 1024 &&
                   suffix < _lastSuffix)
            {
                rate /= 1024;
                suffix++;
            }

            return $"{rate:F1}{_suffixes[suffix]}/s";
        }

        private static string HumanReadableTimeFor(
            int secondsRemaining)
        {
            var seconds = secondsRemaining % 60;
            var minutes = (secondsRemaining / 60) % 60;
            var hours = (secondsRemaining / 3600) % 3600;
            var parts = new List<string>();
            if (hours > 0)
            {
                parts.Add(hours.ToString());
                parts.Add(minutes.ToString("D2"));
            }
            else
            {
                parts.Add(minutes.ToString());
            }

            parts.Add(seconds.ToString("D2"));

            return string.Join(":",
                parts
            );
        }

        private static readonly string[] _suffixes =
        {
            "b",
            "K",
            "M",
            "G",
            "T",
            "P"
            // TODO: is this likely to be used in > 1Pb/s arenas?
        };

        private static readonly int _lastSuffix = _suffixes.Length - 1;
    }
}