using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using PeanutButter.Utils;

namespace bitsplat
{
    public interface IPipeElement
    {
        void Drain();
        bool Pump();
        IPassThrough Pipe(IPassThrough pipe);
    }

    public interface ISource : IPipeElement
    {
        ISink Pipe(ISink sink);
    }

    public interface ISink : IPipeElement
    {
        void Write(byte[] buffer, int count);
        void End();
        void SetSource(IPipeElement source);
    }

    public interface IPassThrough
        : ISource,
          ISink
    {
    }

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

    public class PassThrough : IPassThrough
    {
        private readonly Action<byte[], int> _writeAction;
        private readonly Action _endAction;
        private IPipeElement _source;
        private readonly List<ISink> _sinks = new List<ISink>();

        public PassThrough(
            Action<byte[], int> writeAction,
            Action endAction)
        {
            _writeAction = writeAction;
            _endAction = endAction;
        }

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
            _writeAction(buffer, count);
            _sinks.ForEach(sink => sink.Write(buffer, count));
        }

        public void End()
        {
            _sinks.ForEach(sink => sink.End());
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