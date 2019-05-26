using System.Collections.Generic;
using System.IO;

namespace bitsplat.Pipes
{
    public class StreamSource: ISource
    {
        private readonly Stream _source;
        private readonly List<ISink> _sinks = new List<ISink>();

        public StreamSource(Stream source)
        {
            _source = source;
        }

        public bool Pump()
        {
            var buffer = new byte[32768];
            var read = _source.Read(buffer, 0, buffer.Length);
            if (read > 0)
            {
                _sinks.ForEach(sink => sink.Write(buffer, read));
            }
            else
            {
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
    }
}