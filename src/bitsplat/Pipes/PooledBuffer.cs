using System;

namespace bitsplat.Pipes
{
    public interface IPooledBuffer : IDisposable
    {
        byte[] Data { get; }
    }

    public class PooledBuffer : IPooledBuffer
    {
        public byte[] Data { get; }
        public int Size => Data?.Length ?? 0;

        private bool _available = true;
        private readonly object _lock = new object();

        public PooledBuffer(int size)
        {
            Data = new byte[size];
        }

        public bool Borrow()
        {
            if (!_available)
            {
                return false;
            }

            lock (_lock)
            {
                if (!_available)
                {
                    return false;
                }
                _available = false;
                return true;
            }
        }

        public void Dispose()
        {
            lock (_lock)
            {
                _available = true;
            }
        }
    }
}