using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace bitsplat.Pipes
{
    public class StreamSource : ISource
    {
        private Stream _source;
        private readonly bool _disposeAtEnd;
        private readonly List<ISink> _sinks = new List<ISink>();
        private int _readSize;
        private int _lastRead = 0;

        private static int _lastReadSize;

        public StreamSource(Stream source)
            : this(source, true)
        {
            _readSize = ReadChunkIncrement;
            _lastRead = ReadChunkIncrement;
            if (_lastReadSize > _readSize)
            {
                _readSize = _lastReadSize;
            }
        }

        public StreamSource(
            Stream source,
            bool disposeAtEnd)
        {
            _source = source;
            _disposeAtEnd = disposeAtEnd;
            _stopwatch = new Stopwatch();
        }

        public static int MaxBuffer = 32 * 1024 * 1024; // 32M
        public static int ReadChunkIncrement = 131072; // 128K
        private readonly Stopwatch _stopwatch;

        public bool Pump()
        {
            using var pooledBuffer = BufferPool.Borrow(MaxBuffer);
            var buffer = pooledBuffer.Data;

            var toRead = CalculateReadBufferSize();

            _readSize = toRead;
            _lastRead = _source?.Read(buffer, 0, toRead) ?? 0;

            if (_lastRead > 0)
            {
                Time(() =>
                    _sinks.ForEach(sink => sink.Write(buffer, _lastRead))
                );
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

        private int CalculateReadBufferSize()
        {
            var toRead = (_lastRead < _readSize || _stopwatch.ElapsedMilliseconds >= 1000)
                             ? _lastRead
                             : _lastRead + ReadChunkIncrement;
            if (_stopwatch.ElapsedMilliseconds >= 1500 &&
                toRead > ReadChunkIncrement)
            {
                // syncs are taking > 1.5 ms -- back off a little to keep the app interactive
                toRead -= ReadChunkIncrement;
            }

            if (toRead > MaxBuffer)
            {
                toRead = MaxBuffer;
            }

            return toRead;
        }

        private void Time(Action action)
        {
            _stopwatch.Reset();
            _stopwatch.Start();
            action();
            _stopwatch.Stop();
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
            _lastReadSize = _readSize; // let the next instance start at the same read size
            var sinks = _sinks.ToArray();
            _sinks.Clear();
            var source = _source;
            _source = null;
            sinks.ForEach(s => s.Dispose());
            source?.Dispose();
        }
    }
}