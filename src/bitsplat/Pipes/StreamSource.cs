using System.Collections.Generic;
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

        public StreamSource(Stream source)
            : this(source, true)
        {
            _readSize = ReadChunkIncrement;
            _lastRead = ReadChunkIncrement;
        }

        public StreamSource(
            Stream source,
            bool disposeAtEnd)
        {
            _source = source;
            _disposeAtEnd = disposeAtEnd;
        }
        
        public static int MaxBuffer = 5 * 1024 * 1024;
        public static int ReadChunkIncrement = 32768;
        private bool _disposed;

        public bool Pump()
        {
            using var pooledBuffer = BufferPool.Borrow(MaxBuffer);
            var buffer = pooledBuffer.Data;
            
            // TODO: instead of just increasing buffer sizes
            //       to a max, since sinks flush at write
            //       the read buffer size should work towards about
            //       what can be written in a second to keep the
            //       app interactive to, eg, SIGINT
            // -> use case: copying over wifi, smb, max rate is 4mb/s
            //    but buffer size is 16, so the app is waiting for 4 seconds
            //    per flush, which translates into a 4 second delay when
            //    attempting to exit as streams are flushing
            var toRead = _lastRead < _readSize
                         ? _lastRead
                         : _lastRead + ReadChunkIncrement;
            if (toRead > MaxBuffer)
            {
                toRead = MaxBuffer;
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
            var sinks = _sinks.ToArray();
            _sinks.Clear();
            var source = _source;
            _source = null;
            sinks.ForEach(s => s.Dispose());
            source?.Dispose();
        }
    }
}