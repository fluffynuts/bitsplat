using System.Collections.Generic;

namespace bitsplat.Pipes
{
    public abstract class PassThrough : IPassThrough
    {
        private IPipeElement _source;
        private readonly List<ISink> _sinks = new List<ISink>();

        public void Drain()
        {
            _source?.Drain();
        }

        public bool Pump()
        {
            return _source?.Pump() ?? false;
        }

        public ISink Pipe(ISink sink)
        {
            _sinks.Add(sink);
            sink.SetSource(this);
            return sink;
        }

        public void Write(
            byte[] buffer,
            int count)
        {
            OnWrite(buffer, count);
            _sinks.ForEach(sink => sink.Write(buffer, count));
        }
        
        protected abstract void OnWrite(byte[] buffer, int count);
        protected abstract void OnEnd();

        public void End()
        {
            _sinks.ForEach(sink => sink.End());
            OnEnd();
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

        public void Dispose()
        {
        }
    }

}