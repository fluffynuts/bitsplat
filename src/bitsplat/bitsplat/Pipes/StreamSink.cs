using System.Collections.Generic;
using System.IO;

namespace bitsplat.Pipes
{
    public class StreamSink : ISink
    {
        private Stream _target;
        private readonly bool _disposeOnEnd;
        private IPipeElement _source;
        private readonly List<ISink> _sinks = new List<ISink>();

        public StreamSink(Stream target): this(target, false)
        {
        }

        public StreamSink(
            Stream target,
            bool disposeOnEnd)
        {
            _target = target;
            _disposeOnEnd = disposeOnEnd;
        }

        public void Drain()
        {
            _source?.Drain();
        }

        public bool Pump()
        {
            return _source?.Pump() ?? false;
        }

        public void Write(
            byte[] buffer,
            int count)
        {
            _target?.Write(buffer, 0, count);
        }

        public void End()
        {
            if (_disposeOnEnd)
            {
                _target?.Dispose();
            }

            _target = null;
        }

        public void SetSource(IPipeElement source)
        {
            _source = source;
        }

        public IPassThrough Pipe(IPassThrough pipe)
        {
            _sinks.Add(pipe);
            pipe.SetSource(this);
            return pipe;
        }
    }
}