using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Pipes;
using bitsplat.Storage;

namespace bitsplat
{
    public interface ISynchronizer
    {
        void Synchronize(
            IFileSystem from,
            IFileSystem to);
    }

    public class Synchronizer
        : ISynchronizer
    {
        private readonly IPassThrough[] _intermediatePipes;

        public Synchronizer(
            params IPassThrough[] intermediatePipes)
        {
            _intermediatePipes = intermediatePipes;
        }

        public void Synchronize(
            IFileSystem source,
            IFileSystem target
        )
        {
            var sourceResources = source.ListResourcesRecursive();
            var targetResourcesCollection = target.ListResourcesRecursive();
            var targetResources = targetResourcesCollection as IFileResource[]
                ?? targetResourcesCollection.ToArray();
            sourceResources.ForEach(sourceResource =>
            {
                var targetResource = targetResources.FirstOrDefault(
                    r => r.Matches(sourceResource));
                if (targetResource != null)
                {
                    return;
                }

                var sourceStream = sourceResource.Read();
                var targetStream = target.Open(sourceResource.RelativePath, FileMode.OpenOrCreate);
                
                targetResource = targetResources.FirstOrDefault(
                    r => r.RelativePath == sourceResource.RelativePath);
                if (targetResource != null)
                {
                    // TODO: some data checks
                    // - if the prescribed data checks pass, then this
                    //   seek strategy is fine
                    // - if not, then don't do it (:
                    sourceStream.Seek(targetResource.Size, SeekOrigin.Begin);
                    targetStream.Seek(targetResource.Size, SeekOrigin.Begin);
                }
                
                
                var composition = ComposePipeline(sourceStream, targetStream);
                composition.Drain();
            });
        }

        private ISink ComposePipeline(
            Stream source,
            Stream target)
        {
            if (!_intermediatePipes.Any())
            {
                return source.Pipe(target);
            }

            var composition = _intermediatePipes.Aggregate(
                source.Pipe(new NullPassThrough()),
                (acc, cur) => acc.Pipe(cur)
            );
            return composition.Pipe(target);
        }
    }

    public static class FileResourceExtensions
    {
        public static bool Matches(
            this IFileResource source,
            IFileResource other)
        {
            if (source == null ||
                other == null)
            {
                return false;
            }

            return Matchers.Aggregate(
                true,
                (
                    acc,
                    cur) => acc && cur(source, other)
            );
        }

        private static Func<IFileResource, IFileResource, bool>[] Matchers =
        {
            ShouldHaveSameRelativePath,
            ShouldHaveSameSize
            // TODO: partial data check: sample source and other to look for
            // easy mismatches, which should be spottable on same-size media
            // files with 2 or 3 512-byte chunks taken at random, if the sizes
            // and names match
        };

        private static bool ShouldHaveSameSize(
            IFileResource arg1,
            IFileResource arg2)
        {
            return arg1.Size == arg2.Size;
        }

        private static bool ShouldHaveSameRelativePath(
            IFileResource arg1,
            IFileResource arg2)
        {
            return arg1.RelativePath == arg2.RelativePath;
        }
    }

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