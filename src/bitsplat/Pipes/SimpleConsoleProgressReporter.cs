using System;
using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public class SimpleConsoleProgressReporter : IProgressReporter
    {
        public bool Quiet { get; set; } = true;

        private readonly IMessageWriter _messageWriter;
        private string _current;
        private int _maxLabelLength;

        public SimpleConsoleProgressReporter(
            IMessageWriter messageWriter)
        {
            _messageWriter = messageWriter;
            try
            {
                _maxLabelLength = (int) (Console.WindowWidth * 0.8);
            }
            catch
            {
                /* ignore */
            }
        }

        public void NotifyCurrent(
            NotificationDetails details)
        {
            var label = details.Label ?? "(unknown)";
            var isStart = details.CurrentBytesTransferred == 0;
            var isEnd = details.CurrentBytesTransferred == details.CurrentTotalBytes;

            if (isStart)
            {
                NotifyStart(label);
            }
            else if (isEnd)
            {
                NotifyComplete(label);
            }
            else
            {
                ReportIntermediateState(details);
            }
        }

        protected virtual void ReportIntermediateState(
            NotificationDetails details)
        {
            // intentionally left blank - this reporter is quiet
        }

        private void NotifyComplete(string label)
        {
            if (_current == null)
            {
                return;
            }

            _current = null;
            Write(label, "[ OK ]");
        }

        private void NotifyStart(string label)
        {
            if (_current == label)
            {
                return;
            }

            _current = label;
            Rewrite(label, null);
        }

        protected void Write(
            string start,
            string end)
        {
            _messageWriter.Write(
                RenderMessageLine(
                    start,
                    end
                )
            );
        }

        protected void Rewrite(
            string start,
            string end)
        {
            _messageWriter.Rewrite(
                RenderMessageLine(
                    start,
                    end
                )
            );
        }

        private string RenderMessageLine(string start, string end)
        {
            start ??= "";
            var required = _maxLabelLength - start.Length - (end ?? "").Length;
            if (required < 1 &&
                _maxLabelLength > 0)
            {
                var cutFrom = start.Length + required - 2;
                if (cutFrom < 0)
                {
                    cutFrom = 0;
                }

                start = $"…{start.Substring(cutFrom)}";
                required = 1;
            }

            var spacing = new String(' ',
                required > 0
                    ? required
                    : 1);
            var fullLine = $"\r{start}{spacing}{end}";
            return fullLine;
        }

        public void NotifyError(NotificationDetails details)
        {
            Write(details.Label, "[FAIL]");
            Write(details.Exception.Message, "");
            Write(details.Exception.StackTrace, "");
        }

        // add 2 spaces and 6 chars for [ -- ]
        protected virtual int DetailPadding { get; set; } = 8;

        public void SetMaxLabelLength(int chars)
        {
            var toSet = chars + DetailPadding;
            try
            {
                // if Console.WindowWidth is available, limit to that value with a space
                if (toSet >= Console.WindowWidth)
                {
                    toSet = Console.WindowWidth - 1;
                }
            }
            catch
            {
                /* intentionally left blank */
            }

            if (toSet > _maxLabelLength)
            {
                _maxLabelLength = toSet;
            }
        }

        public void NotifyPrepare(
            string operation,
            IFileSystem source,
            IFileSystem target)
        {
            _messageWriter.Write(
                operation
            );
        }

        public void NotifyNoWork(
            IFileSystem source,
            IFileSystem target)
        {
            _messageWriter.Write($"Up to date: {source.BasePath} => {target.BasePath}");
        }

        public void NotifyOverall(
            NotificationDetails details)
        {
            var (label, current, total) =
                (details.Label, details.CurrentItem, details.TotalItems);
            if (string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            if (current == 0)
            {
                _messageWriter.Write($"{label} ({total})");
            }
        }

        public T Bookend<T>(string message, Func<T> toRun)
        {
            StartProgress(message);

            try
            {
                var result = toRun();
                StopProgress(message);
                return result;
            }
            catch
            {
                FailProgress(message);
                throw;
            }
        }

        private void StartProgress(string message)
        {
            if (Quiet)
            {
                Rewrite(message, null);
            }
            else
            {
                _messageWriter.StartProgress(message);
            }
        }

        private void StopProgress(string message)
        {
            if (Quiet)
            {
                Write(message, "[ OK ]");
            }
            else
            {
                _messageWriter.StopProgress(RenderMessageLine(message, "[ OK ]"));
            }
        }

        private void FailProgress(string message)
        {
            if (Quiet)
            {
                Write(message, "[FAIL]");
            }
            else
            {
                _messageWriter.StopProgress(RenderMessageLine(message, "[FAIL]"));
            }
        }

        public void Bookend(string message, Action toRun)
        {
            Bookend(
                message,
                () =>
                {
                    toRun();
                    return 0;
                });
        }
    }
}