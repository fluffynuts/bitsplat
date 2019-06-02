using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Pipes;
using bitsplat.ResourceMatchers;
using bitsplat.ResumeStrategies;
using bitsplat.Storage;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

// ReSharper disable PossibleMultipleEnumeration

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestSynchronizer
    {
        [Test]
        public void ShouldImplement_ISynchronizer()
        {
            // Arrange
            Expect(typeof(Synchronizer))
                .To.Implement<ISynchronizer>();
            // Act
            // Assert
        }

        [TestFixture]
        public class Behavior
        {
            [TestFixture]
            public class WhenPresentedWithTwoEmptyFilesystems
            {
                [Test]
                public void ShouldListRecursiveOnBoth()
                {
                    // Arrange
                    var fs1 = Substitute.For<IFileSystem>();
                    var fs2 = Substitute.For<IFileSystem>();
                    var sut = Create();
                    // Act
                    sut.Synchronize(fs1, fs2);
                    // Assert
                    Expect(fs1)
                        .To.Have.Received(1)
                        .ListResourcesRecursive();
                    Expect(fs2)
                        .To.Have.Received(1)
                        .ListResourcesRecursive();
                }
            }

            [TestFixture]
            public class WhenSourceHasOneFileAndTargetIsEmpty
            {
                [Test]
                public void ShouldCopyTheSourceFileToTarget()
                {
                    // Arrange
                    using (var arena = new TestArena())
                    {
                        var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                        var relPath = "some-file.ext";
                        var data = GetRandomBytes();
                        arena.CreateResource(
                            arena.SourcePath,
                            relPath,
                            data);
                        var sut = Create();
                        // Act
                        sut.Synchronize(source, target);
                        // Assert
                        var inTarget = target.ListResourcesRecursive();
                        Expect(inTarget)
                            .To.Contain.Only(1)
                            .Item();
                        var copied = inTarget.Single();
                        Expect(copied.RelativePath)
                            .To.Equal(relPath);
                        Expect(copied)
                            .To.Have.Data(data);
                    }
                }
            }

            [TestFixture]
            public class WhenSourceHasFileAndTargetHasSameFile
            {
                [Test]
                public void ShouldNotReWriteTheFile()
                {
                    // Arrange
                    using (var arena = new TestArena())
                    {
                        var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                        var relPath = GetRandomString(2);
                        var data = GetRandomBytes(100);
                        var targetFilePath = arena.CreateResource(
                            arena.TargetPath,
                            relPath,
                            data);
                        arena.CreateResource(
                            arena.SourcePath,
                            relPath,
                            data);
                        var beforeTest = DateTime.Now;
                        var lastWrite = beforeTest.AddMinutes(GetRandomInt(-100, -1));
                        File.SetLastWriteTime(targetFilePath, lastWrite);

                        var sut = Create();
                        // Act
                        sut.Synchronize(source, target);
                        // Assert
                        Expect(targetFilePath)
                            .To.Be.A.File();
                        var targetInfo = new FileInfo(targetFilePath);
                        Expect(targetInfo.LastWriteTime)
                            .To.Be.Less.Than(beforeTest);
                    }
                }
            }

            [TestFixture]
            public class WhenSourceHasFileAndTargetHasPartialFileWithNoErrors
            {
                [Test]
                public void ShouldResumeWhenResumeStrategySaysYes()
                {
                    // Arrange
                    using (var arena = new TestArena())
                    {
                        var resumeStrategy = Substitute.For<IResumeStrategy>();
                        resumeStrategy.CanResume(Arg.Any<Stream>(), Arg.Any<Stream>())
                            .Returns(true);
                        var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                        var relPath = GetRandomString();
                        var allData = GetRandomBytes(1024);
                        var partialSize = GetRandomInt(384, 768);
                        var partialData = allData.Take(partialSize)
                            .ToArray();
                        var expected = allData.Skip(partialSize)
                            .ToArray();
                        var targetFilePath = arena.CreateResource(
                            arena.TargetPath,
                            relPath,
                            partialData);
                        arena.CreateResource(
                            arena.SourcePath,
                            relPath,
                            allData);
                        var captured = new List<byte>();
                        var ended = false;
                        var intermediate = new GenericPassThrough(
                            (data, count) => captured.AddRange(data.Take(count)),
                            () => ended = true
                        );
                        var sut = Create(
                            resumeStrategy,
                            new IPassThrough[] { intermediate }
                        );
                        // Act
                        sut.Synchronize(source, target);
                        // Assert
                        var targetData = File.ReadAllBytes(targetFilePath);
                        Expect(targetData)
                            .To.Equal(allData);
                        var transferred = captured.ToArray();
                        Expect(transferred)
                            .To.Equal(expected, "unexpected transferred data");
                        Expect(ended)
                            .To.Be.True();
                    }
                }

                [Test]
                public void ShouldNotResumeIfResumeStrategySaysNo()
                {
                    // Arrange
                    using (var arena = new TestArena())
                    {
                        var resumeStrategy = Substitute.For<IResumeStrategy>();
                        resumeStrategy.CanResume(Arg.Any<Stream>(), Arg.Any<Stream>())
                            .Returns(false);
                        var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                        var relPath = GetRandomString();
                        var allData = GetRandomBytes(1024);
                        var partialSize = GetRandomInt(384, 768);
                        var partialData = allData.Take(partialSize)
                            .ToArray();
                        var targetFilePath = arena.CreateResource(
                            arena.TargetPath,
                            relPath,
                            partialData);
                        arena.CreateResource(
                            arena.SourcePath,
                            relPath,
                            allData);
                        var captured = new List<byte>();
                        var ended = false;
                        var intermediate = new GenericPassThrough(
                            (data, count) => captured.AddRange(data.Take(count)),
                            () => ended = true
                        );
                        var sut = Create(
                            resumeStrategy,
                            new IPassThrough[] { intermediate }
                        );
                        // Act
                        sut.Synchronize(source, target);
                        // Assert
                        var targetData = File.ReadAllBytes(targetFilePath);
                        Expect(targetData)
                            .To.Equal(allData);
                        var transferred = captured.ToArray();
                        Expect(transferred)
                            .To.Equal(allData, "should have re-transferred all data");
                        Expect(ended)
                            .To.Be.True();
                    }
                }

                [Test]
                public void ShouldPassThroughAllProvidedIntermediatesInOrder()
                {
                    // Arrange
                    using (var arena = new TestArena())
                    {
                        var resumeStrategy = Substitute.For<IResumeStrategy>();
                        resumeStrategy.CanResume(Arg.Any<Stream>(), Arg.Any<Stream>())
                            .Returns(false);
                        var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                        var relPath = GetRandomString();
                        var allData = GetRandomBytes(100, 200); // small buffer, likely to be read in one pass
                        arena.CreateResource(
                            arena.SourcePath,
                            relPath,
                            allData);
                        var intermediateCalls = new List<string>();
                        var endCalls = new List<string>();
                        var intermediate1 = new GenericPassThrough(
                            (data, count) => intermediateCalls.Add("first"),
                            () => endCalls.Add("first")
                        );
                        var intermediate2 = new GenericPassThrough(
                            (data, count) => intermediateCalls.Add("second"),
                            () => endCalls.Add("second")
                        );
                        var intermediate3 = new GenericPassThrough(
                            (data, count) => intermediateCalls.Add("third"),
                            () => endCalls.Add("third")
                        );
                        var expected = new[]
                        {
                            "first",
                            "second",
                            "third"
                        };
                        var sut = Create(
                            resumeStrategy,
                            new IPassThrough[]
                            {
                                intermediate1, intermediate2, intermediate3
                            });
                        // Act
                        sut.Synchronize(source, target);
                        // Assert
                        Expect(intermediateCalls)
                            .To.Equal(expected);
                        Expect(endCalls)
                            .To.Equal(expected);
                    }
                }
            }
        }

        [TestFixture]
        public class NotifyingNotifiablePipes
        {
            [Test]
            public void ShouldNotifyNotifiablePipesWithPossibleResumeSizes()
            {
                // Arrange
                using (var arena = new TestArena())
                {
                    var missingData = GetRandomBytes(100);
                    arena.CreateSourceResource("missing", missingData);
                    var partialFileAllData = "hello world".AsBytes();
                    var partialTargetData = "hello".AsBytes();
//                    var partialTargetData = partialFileAllData
//                        .Take(GetRandomInt(50, 70))
//                        .ToArray();
                    arena.CreateSourceResource("partial", partialFileAllData);
                    arena.CreateTargetResource("partial", partialTargetData);
                    var existingData = GetRandomBytes(100);
                    arena.CreateSourceResource("existing", existingData);
                    arena.CreateTargetResource("existing", existingData);
                    var intermediate1 = new GenericPassThrough(
                        (data, count) =>
                        {
                        },
                        () =>
                        {
                        });
                    var notifyable = new NotifiableGenericPassThrough(
                        (data, count) =>
                        {
                        },
                        () =>
                        {
                        });
                    var sut = Create(
                        new AlwaysResumeStrategy(),
                        new IPassThrough[] { notifyable, intermediate1 }
                            .Randomize()
                            .ToArray(),
                        _defaultResourceMatchers);
                    // Act
                    sut.Synchronize(
                        arena.SourceFileSystem,
                        arena.TargetFileSystem);
                    // Assert
                    var partialResultData = arena.TargetFileSystem.ReadAllBytes("partial");
                    Expect(partialResultData)
                        .To.Equal(partialFileAllData, 
                            () => $@"Expected {
                                partialFileAllData.Length
                                } bytes, but got {
                                    partialResultData.Length
                                }: '{partialFileAllData.AsString()}' vs '{partialResultData.AsString()}'");
                    Expect(arena.TargetFileSystem.ReadAllBytes("existing"))
                        .To.Equal(existingData);
                    Expect(arena.TargetFileSystem.ReadAllBytes("missing"))
                        .To.Equal(missingData);
                    Expect(notifyable.InitialNotifications)
                        .To.Contain.Only(1)
                        .Item(); // should only have one initial notification
                    var initial = notifyable.InitialNotifications.Single();
                    Expect(initial)
                        .To.Contain
                        .Only(2)
                        .Items(); // should only have partial and missing in sync queue
                    Expect(initial)
                        .To.Contain.Exactly(1)
                        .Matched.By(
                            resource => resource.RelativePath == "missing"
                        );
                    Expect(initial)
                        .To.Contain.Exactly(1)
                        .Matched.By(
                            resource => resource.RelativePath == "partial"
                        );
                    Expect(notifyable.ResourceNotifications)
                        .To.Contain.Exactly(2).Items();
                    var missingResource = notifyable.ResourceNotifications.First();
                    Expect(missingResource.source.RelativePath)
                        .To.Equal("missing");
                    Expect(missingResource.source.Size)
                        .To.Equal(missingData.Length);
                    Expect(missingResource.target)
                        .To.Be.Null();
                    var partialResource = notifyable.ResourceNotifications.Second();
                    Expect(partialResource.source.RelativePath)
                        .To.Equal("partial");
                    Expect(partialResource.source.Size)
                        .To.Equal(partialFileAllData.Length);
                    Expect(partialResource.target.RelativePath)
                        .To.Equal("partial");
                    Expect(partialResource.target.Size)
                        .To.Equal(partialTargetData.Length);
                }
            }
        }

        public class NotifiableGenericPassThrough
            : GenericPassThrough,
              ISyncQueueNotifiable
        {
            public List<IEnumerable<IFileResource>> InitialNotifications { get; }
                = new List<IEnumerable<IFileResource>>();

            public List<(IFileResource source, IFileResource target)> ResourceNotifications { get; }
                = new List<(IFileResource source, IFileResource target)>();

            public NotifiableGenericPassThrough(
                Action<byte[], int> onWrite,
                Action onEnd)
                : base(onWrite, onEnd)
            {
            }

            public void NotifySyncBatch(IEnumerable<IFileResource> sourceResources)
            {
                InitialNotifications.Add(sourceResources);
            }

            public void NotifyImpendingSync(
                IFileResource sourceResource,
                IFileResource targetResource)
            {
                ResourceNotifications.Add((sourceResource, targetResource));
            }
        }

        public class GenericPassThrough : PassThrough
        {
            private readonly Action<byte[], int> _onWrite;
            private readonly Action _onEnd;

            public GenericPassThrough(
                Action<byte[], int> onWrite,
                Action onEnd)
            {
                _onWrite = onWrite;
                _onEnd = onEnd;
            }

            protected override void OnWrite(byte[] buffer, int count)
            {
                _onWrite(buffer, count);
            }

            protected override void OnEnd()
            {
                _onEnd();
            }
        }

        private static ISynchronizer Create(
            params IPassThrough[] intermediatePipes)
        {
            return Create(null, intermediatePipes);
        }

        private static ISynchronizer Create(
            IResumeStrategy resumeStrategy = null,
            IPassThrough[] intermediatePipes = null,
            IResourceMatcher[] resourceMatchers = null)
        {
            return new Synchronizer(
                resumeStrategy ?? new AlwaysResumeStrategy(),
                intermediatePipes ?? new IPassThrough[0],
                resourceMatchers ?? _defaultResourceMatchers
            );
        }

        private static readonly IResourceMatcher[] _defaultResourceMatchers =
        {
            new SameRelativePathMatcher(),
            new SameSizeMatcher()
        };
    }

    public static class FileSystemExtensionsForTesting
    {
        public static byte[] ReadAllBytes(
            this IFileSystem fileSystem,
            string name)
        {
            using (var stream = fileSystem.Open(name, FileMode.Open))
            {
                return stream.ReadAllBytes();
            }
        }
    }
}