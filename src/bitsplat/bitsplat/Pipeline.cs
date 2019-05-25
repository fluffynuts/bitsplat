using System.Collections.Generic;
using System.IO;

namespace bitsplat
{
    public interface IPipeline
    {
        IPipeline Pipe(Stream target);
        void Drain();
    }

    internal class ReaderWriterFacade
    {
        private readonly Pipeline _pipe;
        private readonly Stream _stream;

        public ReaderWriterFacade(
            Stream stream)
        {
            _stream = stream;
        }

        public ReaderWriterFacade(
            Pipeline pipe)
        {
            _pipe = pipe;
        }

        public int Read(byte[] buffer)
        {
            return _stream?.Read(buffer, 0, buffer.Length)
                ?? _pipe.Read(buffer);
        }

        public void Write(byte[] buffer, int count)
        {
            if (_stream != null)
            {
                _stream.Write(buffer, 0, count);
            }
            else
            {
                _pipe.Write(buffer, count);
            }
        }
    }

    public class Pipeline : IPipeline
    {
        private readonly ReaderWriterFacade _stream;
        private readonly List<Pipeline> _sinks = new List<Pipeline>();
        private Pipeline _upstream;

        public Pipeline(Stream source)
            : this(new ReaderWriterFacade(source))
        {
        }

        internal Pipeline(
            ReaderWriterFacade stream)
        {
            _stream = stream;
        }

        internal Pipeline(
            Pipeline source,
            Stream target): this(new ReaderWriterFacade(target))
        {
            _upstream = source;
        }
        
        internal int Read(byte[] buffer)
        {
            return _stream.Read(buffer);
        }

        internal void Write(
            byte[] buffer,
            int count)
        {
            _stream.Write(buffer, count);
        }

        public IPipeline Pipe(Stream target)
        {
            var sink = new Pipeline(this, target);
            _sinks.Add(sink);
            return sink;
        }

        public bool Pump()
        {
            if (_upstream != null)
            {
                return _upstream.Pump();
            }

            var buffer = new byte[32768];
            var read = _stream.Read(buffer);
            if (read > 0)
            {
                _sinks.ForEach(sink => sink.Write(buffer, read));
            }
            return read > 0;
        }

        public void Drain()
        {
            while (_upstream.Pump())
            {
            }
        }
    }
}