using System;

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
}