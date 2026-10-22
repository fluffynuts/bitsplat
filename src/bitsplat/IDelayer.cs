using System;
using System.Threading;

namespace bitsplat
{
    public interface IDelayer
    {
        void Delay(TimeSpan duration);
    }

    public class ThreadSleepDelayer : IDelayer
    {
        public void Delay(TimeSpan duration)
        {
            Thread.Sleep(duration);
        }
    }
}
