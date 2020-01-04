using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    public static class BufferPool
    {
        private static readonly ConcurrentBag<PooledBuffer> _buffers = new ConcurrentBag<PooledBuffer>();
        
        public static IPooledBuffer Borrow(int required)
        {
            var borrowed = _buffers.FirstOrDefault(b => b.Size >= required && b.Borrow());
            return borrowed ?? CreateNewPooledBufferFor(required);
        }

        private static IPooledBuffer CreateNewPooledBufferFor(int required)
        {
            var result = new PooledBuffer(required);
            result.Borrow();
            _buffers.Add(result);
            return result;
        }
    }

    public class StreamSource : ISource
    {
        private Stream _source;
        private readonly bool _disposeAtEnd;
        private readonly List<ISink> _sinks = new List<ISink>();
        private int _readSize;
        private int _lastRead = 0;

        public StreamSource(Stream source)
            : this(source, true)
        {
            _readSize = READ_CHUNK_INCREMENT;
            _lastRead = READ_CHUNK_INCREMENT;
        }

        public StreamSource(
            Stream source,
            bool disposeAtEnd)
        {
            _source = source;
            _disposeAtEnd = disposeAtEnd;
        }

        private const int MAX_BUFFER = 16 * 1024 * 1024;
        private const int READ_CHUNK_INCREMENT = 32768;

        public bool Pump()
        {
            using var pooledBuffer = BufferPool.Borrow(MAX_BUFFER);
            var buffer = pooledBuffer.Data;
            var toRead = _lastRead < _readSize
                         ? _lastRead
                         : _lastRead + READ_CHUNK_INCREMENT;
            if (toRead > MAX_BUFFER)
            {
                toRead = MAX_BUFFER;
            }

            _lastRead = _source?.Read(buffer, 0, toRead) ?? 0;
            if (_lastRead > 0)
            {
                _sinks.ForEach(sink => sink.Write(buffer, _lastRead));
            }
            else
            {
                if (_disposeAtEnd)
                {
                    _source?.Dispose();
                }

                _source = null;
                _sinks.ForEach(sink => sink.End());
            }

            return _lastRead > 0;
        }

        public ISink Pipe(ISink sink)
        {
            _sinks.Add(sink);
            sink.SetSource(this);
            return sink;
        }

        public void Detach()
        {
            // should this rewind, or do anything with the source stream?
            _sinks.Clear();
        }

        public IPassThrough Pipe(IPassThrough pipe)
        {
            _sinks.Add(pipe);
            pipe.SetSource(this);
            return pipe;
        }

        public void Drain()
        {
            while (Pump())
            {
            }
        }

        public void Dispose()
        {
            _source?.Dispose();
            _source = null;
        }
    }
}