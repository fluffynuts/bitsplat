using System;
using System.Threading;
using System.Threading.Tasks;

namespace bitsplat.Pipes
{
    public interface IMessageWriter
    {
        public bool Quiet { get; set; }

        void Write(string message);
        void Rewrite(string message);
        void StartProgress(string message);
        void StopProgress(string message);
    }

    public class ConsoleMessageWriter : IMessageWriter
    {
        private string _lastMessage;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _indeterminateTask;
        private LinkedList<string> _spinner;

        private readonly object _lock = new object();

        public bool Quiet { get; set; }

        public void Write(string message)
        {
            lock (_lock)
            {
                _lastMessage = null;
                Console.WriteLine(message);
            }
        }

        public void Rewrite(string message)
        {
            lock (_lock)
            {
                var overwrite = new String(' ', _lastMessage?.Length ?? 0);
                _lastMessage = message;
                Console.Out.Write($"\r{overwrite}\r{message}");
                Console.Out.Flush();
            }
        }

        public void StartProgress(string message)
        {
            // just a safety-check: console can only do one spinner at a time
            StopProgress(null);
            _cancellationTokenSource = new CancellationTokenSource();
            _indeterminateTask = Task.Run(() =>
                {
                    var step = 0;
                    Rewrite($"{message} {_spinner.Step()}");
                    while (!_cancellationTokenSource.IsCancellationRequested)
                    {
                        // only report 4x per second
                        if (step++ % 25 == 0)
                        {
                            Rewrite($"{message} {_spinner.Step()}");
                        }

                        // fine granularity to provide responsive stop
                        Thread.Sleep(10);
                    }

                    Rewrite($"{_lastMessage}\n");
                    _lastMessage = null;
                },
                _cancellationTokenSource.Token);
        }

        public void StopProgress(string message)
        {
            _lastMessage = message;
            _cancellationTokenSource?.Cancel();
            _indeterminateTask?.Wait();
            _cancellationTokenSource = null;
            _indeterminateTask = null;
        }

        public ConsoleMessageWriter()
        {
            SetupSpinnerLinkedList();
        }

        private void SetupSpinnerLinkedList()
        {
            _spinner = new LinkedList<string>("|");
            _spinner.Add("/")
                .Add("-")
                .Add("\\");
            _spinner.Close();
        }
    }
}