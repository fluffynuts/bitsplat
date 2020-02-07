using System;
using System.IO;
using System.Linq;
using bitsplat.Pipes;
using bitsplat.Storage;

namespace bitsplat.ResumeStrategies
{
    public class SimpleResumeStrategy : IResumeStrategy
    {
        public const int DEFAULT_CHECK_BYTES = 2048;
        private readonly IOptions _options;

        public SimpleResumeStrategy(IOptions options)
        {
            _options = options;
        }

        public bool CanResume(
            IFileResource sourceResource,
            IFileResource targetResource,
            Stream source,
            Stream target)
        {
            if (TargetIsLargerThanSource())
            {
                return false;
            }

            var toCheck = (int) Math.Min(
                _options.ResumeCheckBytes,
                Math.Ceiling(targetResource.Size / 2M)
            );

            // most likely fail is at the tail (corruption from interruption of copy)
            return TailBytesMatch() &&
                   // but also check lead, just for paranoia
                   LeadBytesMatch();

            bool TargetIsLargerThanSource()
            {
                return targetResource.Size > sourceResource.Size;
            }

            bool TailBytesMatch()
            {
                var toSeek = targetResource.Size - toCheck;
                return BytesMatch(toSeek, toCheck, source, target);
            }

            bool LeadBytesMatch()
            {
                return BytesMatch(0, toCheck, source, target);
            }
        }

        private static bool BytesMatch(
            long offset,
            int toCheck,
            Stream source,
            Stream target)
        {
            using var sourceResetter = new StreamResetter(source);
            using var sourceData = BufferPool.Borrow(toCheck);
            if (!TryReadBytes(source, offset, toCheck, sourceData.Data))
            {
                return false;
            }

            using var targetResetter = new StreamResetter(target);
            using var targetData = BufferPool.Borrow(toCheck);
            if (!TryReadBytes(target, offset, toCheck, targetData.Data))
            {
                return false;
            }

            // buffer-pool can give back a bigger buffer than required
            // -> have to ensure that we only check byte-for-byte against
            //    the tail {toCheck} bytes
            return sourceData.Data
                .Take(toCheck)
                .SequenceEqual(
                    targetData.Data.Take(toCheck)
                );
        }

        private static bool TryReadBytes(
            Stream stream,
            long offset,
            int count,
            byte[] target)
        {
            stream.Seek(offset, SeekOrigin.Begin);
            var read = stream.Read(target, 0, count);
            return read == count;
        }

        private class StreamResetter : IDisposable
        {
            private readonly Stream _stream;

            public StreamResetter(Stream stream)
            {
                _stream = stream;
            }

            public void Dispose()
            {
                _stream.Seek(0, SeekOrigin.Begin);
            }
        }
    }
}