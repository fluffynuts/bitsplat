using System;
using System.Collections.Generic;
using bitsplat.Storage;

namespace bitsplat.Pipes
{
    public class SimpleConsoleProgressReporter : IProgressReporter
    {
        public bool Quiet { get; set; } = true;

        private readonly IMessageWriter _messageWriter;
        private string _current;
        private int _maxLabelLength;
        
        private const string OK = "[ OK ]";
        private const string FAIL = "[FAIL]";

        public SimpleConsoleProgressReporter(
            IMessageWriter messageWriter)
        {
            _messageWriter = messageWriter;
            try
            {
                _maxLabelLength = Console.WindowWidth - 1;
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

        protected virtual void NotifyComplete(string label)
        {
            if (_current == null)
            {
                return;
            }

            _current = null;
            Write(label, OK);
        }

        protected virtual void NotifyStart(string label)
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
            Write(details.Label, FAIL);
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

        private bool _notifiedStart;
        
        public void NotifyOverall(
            NotificationDetails details
        )
        {
            if (string.IsNullOrWhiteSpace(details.Label))
            {
                return;
            }

            if (details.IsStarting && !_notifiedStart)
            {
                _notifiedStart = true;
                _messageWriter.Write(details.Label);
                _messageWriter.Write(
                    $@"Overall transfer: {
                            details.TotalItems
                        } files, {
                            HumanReadableSizeFor(
                                details.TotalBytes
                            )
                        }");
                _started = DateTime.Now;
            }
            else if (details.IsComplete &&
                     _started != null)
            {
                var timeTaken = DateTime.Now - _started.Value;
                _messageWriter.Write(details.Label);
                _messageWriter.Write(
                    $@"Transferred {
                            details.TotalItems
                        } files, {
                            HumanReadableSizeFor(details.TotalBytes)
                        }, {
                            HumanReadableTimeFor((int) timeTaken.TotalSeconds)
                        }, avg {
                            HumanReadableRateFor(
                                details.TotalBytes / timeTaken.TotalSeconds
                            )
                        }");
                _started = null;
            }
        }

        protected string HumanReadableSizeFor(double size)
        {
            var suffix = 0;
            while (size > 1024 &&
                   suffix < _lastSuffix)
            {
                size /= 1024;
                suffix++;
            }

            return $"{size:F1}{_suffixes[suffix]}";
        }

        protected string HumanReadableRateFor(double rate)
        {
            return $"{HumanReadableSizeFor(rate)}/s";
        }

        protected static string HumanReadableTimeFor(
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
        private DateTime? _started;

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
                Write(message, OK);
            }
            else
            {
                _messageWriter.StopProgress(RenderMessageLine(message, OK));
            }
        }

        private void FailProgress(string message)
        {
            if (Quiet)
            {
                Write(message, FAIL);
            }
            else
            {
                _messageWriter.StopProgress(RenderMessageLine(message, FAIL));
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