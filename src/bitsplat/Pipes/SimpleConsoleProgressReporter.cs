using System;
using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public interface IMessageWriter
    {
        void Write(string message);
        void Rewrite(string message);
    }

    public class ConsoleMessageWriter : IMessageWriter
    {
        private string _lastMessage;

        public void Write(string message)
        {
            _lastMessage = null;
            Console.WriteLine(message);
        }

        public void Rewrite(string message)
        {
            var overwrite = new String(' ', _lastMessage?.Length ?? 0);
            _lastMessage = message;
            Console.Out.Write($"\r{overwrite}{message}");
            Console.Out.Flush();
        }
    }

    public class SimpleConsoleProgressReporter : IProgressReporter
    {
        private readonly IMessageWriter _messageWriter;
        private string _current;
        private int _maxLabelLength;

        public SimpleConsoleProgressReporter(
            IMessageWriter messageWriter)
        {
            _messageWriter = messageWriter;
        }

        public void NotifyCurrent(
            string label,
            int percentComplete)
        {
            if (label == null)
            {
                label = "(unknown)";
            }

            switch (percentComplete)
            {
                case 0:
                    NotifyStart(label);
                    break;
                case 100:
                    NotifyComplete(label);
                    break;
                default:
                    ReportIntermediateState(
                        label,
                        percentComplete
                    );
                    break;
            }
        }

        protected virtual void ReportIntermediateState(
            string label,
            int percentageComplete)
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
            if (required < 1)
            {
                var cutFrom = start.Length + required - 4;
                if (cutFrom < 0)
                {
                    cutFrom = 0;
                }

                start = $"...{start.Substring(cutFrom)}";
                required = 1;
            }

            var spacing = new String(' ', required);
            var fullLine = $"\r{start}{spacing}{end}";
            return fullLine;
        }

        public void NotifyError(string label)
        {
            Write(label, "[FAIL]");
        }

        public void SetMaxLabelLength(int chars)
        {
            try
            {
                _maxLabelLength = chars + 8; // add 2 spaces and 6 chars for [ -- ]
                // if Console.WindowWidth is available, limit to that value with a space
                if (_maxLabelLength >= Console.WindowWidth)
                {
                    _maxLabelLength = Console.WindowWidth - 1;
                }
            }
            catch
            {
                /* intentionally left blank */
            }
        }

        public void NotifyPrepare(
            IFileSystem source,
            IFileSystem target)
        {
            _messageWriter.Write(
                $"Preparing to sync: {source.BasePath} => {target.BasePath}"
            );
        }

        public void NotifyOverall(
            string label,
            int current,
            int total)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                return;
            }

            if (current == 0)
            {
                _messageWriter.Write($"{label} ({total})");
            }
        }
    }
}