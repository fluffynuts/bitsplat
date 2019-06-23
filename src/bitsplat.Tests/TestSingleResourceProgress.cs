using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using bitsplat.Pipes;
using bitsplat.Storage;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestSingleResourceProgress
    {
        [TestFixture]
        public class Contracts
        {
            [TestCase(typeof(IPassThrough))]
            [TestCase(typeof(ISyncQueueNotifiable))]
            public void ShouldImplement_(Type contract)
            {
                // Arrange
                var sut = typeof(SynchronisationProgressPipe);
                // Act
                Expect(sut)
                    .To.Implement(contract);
                // Assert
            }
        }

        [TestFixture]
        public class NotifyBatchStart
        {
            [Test]
            public void ShouldNotifyProgressReporterOfTotalAndIndexZero()
            {
                // Arrange
                var reporter = Substitute.For<IProgressReporter>();
                var sut = Create(reporter);
                var resources = FakeResourcesUnder();
                // Act
                sut.NotifySyncBatchStart(resources);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(0, resources.Count());
            }
        }

        [TestFixture]
        public class NotifySyncStart
        {
            [Test]
            public void ShouldNotifyOfFirstResourceStarting()
            {
                // Arrange
                var reporter = Substitute.For<IProgressReporter>();
                var sut = Create(reporter);
                var sourceBase = GetRandomString(2);
                var sources = FakeResourcesUnder(sourceBase);
                var targetBase = GetRandomString(2);
                var targets = sources
                    .Select(Duplicate)
                    .Select(o => SetBasePath(o, targetBase));
                var source = sources.First();
                var total = sources.Count();
                var target = targets.First();
                // Act
                sut.NotifySyncBatchStart(sources);
                sut.NotifySyncStart(source, target);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(1, total);
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyCurrent(source.RelativePath, 0);
            }
            
            [Test]
            public void ShouldNotifyOfSecondResourceStarting()
            {
                // Arrange
                var reporter = Substitute.For<IProgressReporter>();
                var sut = Create(reporter);
                var sourceBase = GetRandomString(2);
                var sources = FakeResourcesUnder(sourceBase);
                var targetBase = GetRandomString(2);
                var targets = sources
                    .Select(Duplicate)
                    .Select(o => SetBasePath(o, targetBase));
                var total = sources.Count();
                var firstSource = sources.First();
                var firstTarget = targets.First();
                var secondSource = sources.Second();
                var secondTarget = targets.Second();
                // Act
                sut.NotifySyncBatchStart(sources);
                sut.NotifySyncStart(firstSource, firstTarget);
                sut.NotifySyncStart(secondSource, secondTarget);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(2, total);
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyCurrent(secondSource.RelativePath, 0);
            }
        }

        [TestFixture]
        public class NotifySyncComplete
        {
            [Test]
            public void ShouldNotifyOf100PercentCompletionForResource()
            {
                // Arrange
                var reporter = Substitute.For<IProgressReporter>();
                var sut = Create(reporter);
                var sourceBase = GetRandomString(2);
                var sources = FakeResourcesUnder(sourceBase);
                var targetBase = GetRandomString(2);
                var targets = sources
                    .Select(Duplicate)
                    .Select(o => SetBasePath(o, targetBase));
                var total = sources.Count();
                var firstSource = sources.First();
                var firstTarget = targets.First();
                // Act
                sut.NotifySyncBatchStart(sources);
                sut.NotifySyncStart(firstSource, firstTarget);
                sut.NotifySyncComplete(firstSource, firstTarget);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyCurrent(firstSource.RelativePath, 0);
                Expect(reporter)
                    .To.Have.Received()
                    .NotifyCurrent(
                        firstSource.RelativePath,
                        100);
            }
        }

        [TestFixture]
        public class NotifyBatchComplete
        {
            [Test]
            public void ShouldNotifyOverallWithIndexEqualToTotal()
            {
                // Arrange
                var reporter = Substitute.For<IProgressReporter>();
                var sut = Create(reporter);
                var sourceBase = GetRandomString(2);
                var sources = FakeResourcesUnder(sourceBase);
                var targetBase = GetRandomString(2);
                var targets = sources
                    .Select(Duplicate)
                    .Select(o => SetBasePath(o, targetBase));
                var total = sources.Count();
                // Act
                sut.NotifySyncBatchStart(sources);
                sut.NotifySyncBatchComplete(sources);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(0, total);
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(total, total);
            }
        }

        [TestFixture]
        [Ignore("WIP")]
        public class OnWrite
        {
            
        }

        private static IFileResourceProperties Duplicate(IFileResourceProperties arg)
        {
            var result = Substitute.For<IFileResourceProperties>();
            // NSubstitute does some dark, sneaky magick to achieve
            // it's end goals -- and provides a great library in the
            // process. However, I know that doing 
            // .Returns({some other NSubstitute'd property})
            // doesn't work because of the magic, so we have to var off
            // values first
            var (path, relPath, size) = (arg.Path, arg.RelativePath, arg.Size);
            result.Path.Returns(path);
            result.RelativePath.Returns(relPath);
            result.Size.Returns(size);
            return result;
        }

        private static IFileResourceProperties SetBasePath(
            IFileResourceProperties fileResourceProperties,
            string basePath)
        {
            var relPath = fileResourceProperties.RelativePath;
            fileResourceProperties.Path.Returns(
                Path.Combine(basePath, relPath)
            );
            return fileResourceProperties;
        }

        private static IEnumerable<IFileResourceProperties> FakeResourcesUnder(
            string basePath = null
        )
        {
            basePath = basePath ?? GetRandomString(2);
            return GetRandomCollection<IFileResourceProperties>(2, 5)
                .Select(o => SetBasePath(o, basePath));
        }

        private static SynchronisationProgressPipe Create(IProgressReporter reporter)
        {
            return new SynchronisationProgressPipe(
                reporter
            );
        }
    }

    public static class StringExtensions
    {
        public static string RemoveRelativePath(
            this string path,
            string relativePath)
        {
            var result = path.RegexReplace($"{relativePath}$", "");
            result.TrimEnd(Path.DirectorySeparatorChar);
            return result;
        }
    }

    public class FakeResourceBuilder : GenericBuilder<FakeResourceBuilder, IFileResourceProperties>
    {
        public override IFileResourceProperties ConstructEntity()
        {
            var result = Substitute.For<IFileResourceProperties>();
            result.SetMetadata("basePath", GetRandomPath());
            return result;
        }

        public override FakeResourceBuilder WithRandomProps()
        {
            return WithProp(RandomRelativePath)
                .WithProp(RandomSize)
                .WithProp(o =>
                {
                    var basePath = o.GetMetadata<string>("basePath");
                    var relPath = o.RelativePath;
                    o.Path.Returns(Path.Combine(basePath, relPath));
                });
        }

        private void RandomRelativePath(IFileResourceProperties obj)
        {
            obj.RelativePath.Returns(GetRandomPath());
        }

        private void RandomSize(IFileResourceProperties obj)
        {
            obj.Size.Returns(GetRandomInt(100, 1024));
        }

        private static string GetRandomPath()
        {
            return GetRandomCollection<string>(1, 3)
                .JoinWith(Path.DirectorySeparatorChar.ToString());
        }

        public FakeResourceBuilder WithBasePath(string basePath)
        {
            return WithProp(o =>
            {
                o.Path.Returns(Path.Combine(basePath, o.RelativePath));
            });
        }
    }
}