using System;
using System.Threading;
using System.Threading.Tasks;

namespace bitsplat.Pipes
{
    public interface IMessageWriter
    {
        void Write(string message);
        void Rewrite(string message);
        void StartProgress(string message);
        void StopProgress(string message);
        void EndRewrite();
        void Log(string message);
    }

    public class ConsoleMessageWriter : IMessageWriter
    {
        private string _lastMessage;
        private CancellationTokenSource _cancellationTokenSource;
        private Task _indeterminateTask;
        private LinkedList<string> _spinner;

        private readonly object _lock = new object();
        private bool _lastOperationWasRewrite;

        public void Write(string message)
        {
            lock (_lock)
            {
                if (_lastOperationWasRewrite)
                {
                    Console.WriteLine("");
                }

                _lastMessage = null;
                _lastOperationWasRewrite = false;
                Console.WriteLine(message);
            }
        }

        public void EndRewrite()
        {
            if (!_lastOperationWasRewrite)
            {
                return;
            }

            Console.WriteLine("");
            _lastOperationWasRewrite = false;
            _lastMessage = null;
        }

        public void Log(string message)
        {
            Console.WriteLine($"[${TimeStamp}] ${message}");
        }
        
        private string TimeStamp =>
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public void Rewrite(string message)
        {
            lock (_lock)
            {
                var overwrite = new String(' ', _lastMessage?.Length ?? 0);
                _lastMessage = message;
                _lastOperationWasRewrite = true;
                Console.Write($"\r{overwrite}\r{message}");
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
            try
            {
                _indeterminateTask?.Wait();
            }
            catch
            {
                /* ignore: this happens if the task is cancelled early */
            }

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