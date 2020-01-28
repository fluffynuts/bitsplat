using System;
using System.IO;
using bitsplat.Storage;

namespace bitsplat.ResumeStrategies
{
    public class SimpleResumeStrategy: IResumeStrategy
    {
        public SimpleResumeStrategy(IOptions options)
        {
        }

        public bool CanResume(
            IFileResource sourceResource,
            IFileResource targetResource,
            Stream source,
            Stream target)
        {
            if (targetResource.Size > sourceResource.Size)
            {
                return false;
            }

            var toCheck = (int)Math.Min(512, targetResource.Size);
            var toSeek = targetResource.Size - toCheck;
            
            Span<byte> sourceData = stackalloc byte[toCheck];

            using var sourceResetter = new StreamResetter(source);
            source.Seek(toSeek, SeekOrigin.Begin);
            var sourceRead = source.Read(sourceData);
            if (sourceRead != toCheck)
            {
                return false; // can't read all required bytes
            }
            
            using var targetResetter = new StreamResetter(target);
            target.Seek(toSeek, SeekOrigin.Begin);
            Span<byte> targetData = stackalloc byte[toCheck];
            var targetRead = target.Read(targetData);
            
            return targetRead == toCheck && 
                   sourceData.SequenceEqual(targetData);
        }

        private class StreamResetter: IDisposable
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