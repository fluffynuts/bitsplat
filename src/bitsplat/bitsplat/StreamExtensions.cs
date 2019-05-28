using System.IO;
using bitsplat.Pipes;

namespace bitsplat
{
    public static class StreamExtensions
    {
        public static ISource AsSource(
            this Stream source)
        {
            return new StreamSource(source, true);
        }

        public static ISink Pipe(
            this Stream source,
            Stream target)
        {
            return new StreamSource(source)
                .Pipe(new StreamSink(target, true));
        }

        public static IPassThrough Pipe(
            this Stream source,
            IPassThrough other)
        {
            return new StreamSource(source, true)
                .Pipe(other);
        }

        public static ISink Pipe(
            this IPassThrough source,
            Stream other)
        {
            return source.Pipe(
                new StreamSink(other, true)
            );
        }
    }
}