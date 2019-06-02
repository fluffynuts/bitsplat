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

        public StreamSink(Stream target): this(target, true)
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
            Pipe(pipe as ISink);
            return pipe;
        }

        public ISink Pipe(ISink sink)
        {
            _sinks.Add(sink);
            sink.SetSource(this);
            return sink;
        }

        public void Detach()
        {
            _source = null;
            _sinks.Clear();
        }

        public void Dispose()
        {
            _source?.Dispose();
            _target?.Dispose();
            _source = null;
            _target = null;
        }
    }
}