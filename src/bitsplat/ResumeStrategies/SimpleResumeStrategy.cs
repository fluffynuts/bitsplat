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
        private readonly IMessageWriter _messageWriter;

        public SimpleResumeStrategy(
            IOptions options,
            IMessageWriter messageWriter)
        {
            _options = options;
            _messageWriter = messageWriter;
        }

        private void Log(string message)
        {
            if (_options.Verbose)
            {
                _messageWriter.Log(message);
            }
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

            if (SourceOrTargetAreZeroLength())
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
                var result = targetResource.Size > sourceResource.Size;
                Log($"{targetResource.RelativePath}: target is {(result ? "larger" : "smaller")} than source");
                return result;
            }

            bool TailBytesMatch()
            {
                var toSeek = targetResource.Size - toCheck;
                var result = BytesMatch(toSeek, toCheck, source, target, sourceResource, targetResource);
                Log($"{targetResource.RelativePath} tail bytes {toSeek} - {toCheck} {(result ? "" : "do not ")}match");
                return result;
            }

            bool LeadBytesMatch()
            {
                var result = BytesMatch(0, toCheck, source, target, sourceResource, targetResource);
                Log($"{targetResource.RelativePath} lead {toCheck} bytes {(result ? "" : "do not ")}match");
                return result;
            }

            bool SourceOrTargetAreZeroLength()
            {
                Log($"{targetResource.RelativePath} sizes in bytes: source: {sourceResource.Size} vs target: {targetResource.Size}");
                return sourceResource.Size == 0 ||
                       targetResource.Size == 0;
            }
        }

        private bool BytesMatch(long offset,
            int toCheck,
            Stream source,
            Stream target,
            IFileResource sourceResource,
            IFileResource targetResource)
        {
            using var sourceResetter = new StreamResetter(source);
            using var sourceData = BufferPool.Borrow(toCheck);
            if (!TryReadBytes(source, offset, toCheck, sourceData.Data, sourceResource))
            {
                Log($"can't read bytes {offset} - {toCheck} of source {sourceResource.RelativePath}");
                return false;
            }

            using var targetResetter = new StreamResetter(target);
            using var targetData = BufferPool.Borrow(toCheck);
            if (!TryReadBytes(target, offset, toCheck, targetData.Data, targetResource))
            {
                Log($"can't read bytes {offset} - {toCheck} of target {targetResource.RelativePath}");
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

        private bool TryReadBytes(Stream stream,
            long offset,
            int count,
            byte[] target,
            IFileResource resource)
        {
            try
            {
                stream.Seek(offset, SeekOrigin.Begin);
                var read = stream.Read(target, 0, count);
                return read == count;
            }
            catch (Exception ex)
            {
                _messageWriter.Write(
                    $"Resume not supported: unable to read {count} bytes from offset {offset} of {resource.RelativePath}:\n  {ex.Message}"
                );
                return false;
            }
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