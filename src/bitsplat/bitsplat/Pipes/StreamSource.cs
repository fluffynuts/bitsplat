using System.Collections.Generic;
using System.IO;

namespace bitsplat.Pipes
{
    public class StreamSource: ISource
    {
        private Stream _source;
        private readonly bool _disposeAtEnd;
        private readonly List<ISink> _sinks = new List<ISink>();

        public StreamSource(Stream source)
            : this(source, true)
        {
        }

        public StreamSource(
            Stream source,
            bool disposeAtEnd)
        {
            _source = source;
            _disposeAtEnd = disposeAtEnd;
        }

        public bool Pump()
        {
            var buffer = new byte[32768];
            var read = _source?.Read(buffer, 0, buffer.Length) ?? 0;
            if (read > 0)
            {
                _sinks.ForEach(sink => sink.Write(buffer, read));
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

            return read > 0;
        }

        public ISink Pipe(ISink sink)
        {
            _sinks.Add(sink);
            sink.SetSource(this);
            return sink;
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