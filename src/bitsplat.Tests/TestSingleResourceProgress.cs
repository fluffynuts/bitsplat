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
                var batchLabel = GetRandomString();
                // Act
                sut.NotifySyncBatchStart(batchLabel, resources);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(
                        Arg.Is<NotificationDetails>(o =>
                            o.Label == batchLabel &&
                            o.CurrentItem == 0 &&
                            o.TotalItems == resources.Count()
                        )
                    );
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
                var batchLabel = GetRandomString();
                // Act
                sut.NotifySyncBatchStart(batchLabel, sources);
                sut.NotifySyncStart(source, target);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(
                        Arg.Is<NotificationDetails>(o =>
                            o.Label == batchLabel &&
                            o.CurrentItem == 1 &&
                            o.TotalItems == total
                        )
                    );
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyCurrent(Arg.Is<NotificationDetails>(o =>
                            o.Label == source.RelativePath &&
                            o.CurrentBytesTransferred == 0 &&
                            o.CurrentTotalBytes == source.Size
                        )
                    );
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
                var batchLabel = GetRandomString();
                sut.NotifySyncBatchStart(batchLabel, sources);
                sut.NotifySyncStart(firstSource, firstTarget);
                sut.NotifySyncStart(secondSource, secondTarget);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(
                        Arg.Is<NotificationDetails>(o =>
                            o.Label == batchLabel &&
                            o.CurrentItem == 2 &&
                            o.TotalItems == total
                        )
                    );
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyCurrent(
                        Arg.Is<NotificationDetails>(o =>
                            o.Label == secondSource.RelativePath &&
                            o.CurrentBytesTransferred == 0 &&
                            o.CurrentTotalBytes == secondSource.Size
                        )
                    );
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
                sut.NotifySyncBatchStart(GetRandomString(), sources);
                sut.NotifySyncStart(firstSource, firstTarget);
                sut.NotifySyncComplete(firstSource, firstTarget);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyCurrent(
                        Arg.Is<NotificationDetails>(o =>
                            o.Label == firstSource.RelativePath &&
                            o.CurrentBytesTransferred == 0 &&
                            o.CurrentTotalBytes == firstSource.Size
                        )
                    );
                Expect(reporter)
                    .To.Have.Received()
                    .NotifyCurrent(
                        Arg.Is<NotificationDetails>(o =>
                            o.Label == firstSource.RelativePath &&
                            o.CurrentBytesTransferred == firstSource.Size &&
                            o.CurrentTotalBytes == firstSource.Size
                        )
                    );
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
                var batchLabel = GetRandomString();
                // Act
                sut.NotifySyncBatchStart(batchLabel, sources);
                sut.NotifySyncBatchComplete(batchLabel, sources);
                // Assert
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(
                        Arg.Is<NotificationDetails>(o =>
                            o.Label == batchLabel &&
                            o.CurrentItem == 0 &&
                            o.TotalItems == total
                            )
                        );
                Expect(reporter)
                    .To.Have.Received(1)
                    .NotifyOverall(
                        Arg.Is<NotificationDetails>(o =>
                            o.Label == batchLabel &&
                            o.CurrentItem == total &&
                            o.TotalItems == total
                            )
                        );
            }
        }

        [TestFixture]
        [Ignore("WIP")]
        public class OnWrite
        {
        }

        private static IFileResource Duplicate(IFileResource arg)
        {
            var result = Substitute.For<IFileResource>();
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

        private static IFileResource SetBasePath(
            IFileResource fileResource,
            string basePath)
        {
            var relPath = fileResource.RelativePath;
            fileResource.Path.Returns(
                Path.Combine(basePath, relPath)
            );
            return fileResource;
        }

        private static IEnumerable<IFileResource> FakeResourcesUnder(
            string basePath = null
        )
        {
            basePath = basePath ?? GetRandomString(2);
            return GetRandomCollection<IFileResource>(2, 5)
                .Select(o => SetBasePath(o, basePath));
        }

        private static SynchronisationProgressPipe Create(IProgressReporter reporter)
        {
            return new SynchronisationProgressPipe(
                reporter
            );
        }
    }
}