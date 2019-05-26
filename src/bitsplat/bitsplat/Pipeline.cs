using System;
using System.Collections.Generic;
using System.IO;
using PeanutButter.Utils;

namespace bitsplat
{
    public interface IPipeline
    {
        IPipeline Pipe(Stream target);
        void Drain();
    }

    public interface IReader
    {
        int Read(byte[] buffer);
    }

    public interface IWriter
    {
        void Write(
            byte[] buffer,
            int count);
    }

    public interface IData
        : IReader,
          IWriter
    {
    }

    internal class ReaderWriterFacade : IData
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
            if (_stream != null)
            {
                Console.WriteLine($"read from stream: {_stream.GetMetadata<string>("streamId")}");
            }

            return _stream?.Read(buffer, 0, buffer.Length) ?? _pipe.Read(buffer);
        }

        public void Write(
            byte[] buffer,
            int count)
        {
            if (_stream != null)
            {
                Console.WriteLine($"write to stream: {_stream.GetMetadata<string>("streamId")}");
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
        private readonly IData _stream;
        private readonly List<Pipeline> _sinks = new List<Pipeline>();
        private readonly Pipeline _upstream;
        private static int _counter = 0;
        private int _id = _counter++;

        private void Log(string msg)
        {
            Console.WriteLine($"{_id}: {msg}");
        }

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
            Stream target)
            : this(new ReaderWriterFacade(target))
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
            Log("write to _stream");
            _stream.Write(buffer, count);
        }

        public IPipeline Pipe(Stream target)
        {
            Log("create pipeline");
            var sink = new Pipeline(this, target);
            _sinks.Add(sink);
            return sink;
        }

        public bool Pump()
        {
            Log("pump");
            _upstream?.Pump();

            Log($"read from _stream");
            var buffer = new byte[32768];
            var read = _stream.Read(buffer);
            if (read > 0)
            {
                Log("write to sinks");
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

    public static class StreamToPipeExtensions
    {
        public static IPipeline Pipe(
            this Stream source,
            Stream target)
        {
            return new Pipeline(source)
                .Pipe(target);
        }
    }
}