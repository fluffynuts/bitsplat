using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Pipes;
using bitsplat.ResourceMatchers;
using bitsplat.ResumeStrategies;
using bitsplat.Storage;
using bitsplat.Tests.TestingSupport;
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
            public class WithPermissiveFilterOnly
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

                    [Test]
                    public void ShouldNotRecordAnyHistory()
                    {
                        // Arrange
                        var fs1 = Substitute.For<IFileSystem>();
                        var fs2 = Substitute.For<IFileSystem>();
                        var history = Substitute.For<ITargetHistoryRepository>();
                        var sut = Create(targetHistoryRepository: history);
                        // Act
                        sut.Synchronize(fs1, fs2);
                        // Assert
                        Expect(history)
                            .Not.To.Have.Received()
                            .Upsert(Arg.Any<IHistoryItem>());
                    }
                }

                [TestFixture]
                public class WhenSourceHasOneFileAndTargetIsEmptyAndHavePermissiveFilter
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
                            var sut = Create(filters: CreatePermissiveFilter());
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

                    [Test]
                    public void ShouldUpsertHistory()
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
                            var historyRepo = Substitute.For<ITargetHistoryRepository>();
                            var sut = Create(
                                targetHistoryRepository: historyRepo,
                                filters: CreatePermissiveFilter());
                            // Act
                            sut.Synchronize(source, target);
                            // Assert
                            Expect(historyRepo)
                                .To.Have.Received(1)
                                .Upsert(Arg.Is<IHistoryItem>(
                                    o => o.Path == relPath &&
                                         o.Size == data.Length
                                ));
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

                    [Test]
                    public void ShouldNotReWriteTheFileWhenInHistoryButNotOnDisk()
                    {
                        // Arrange
                        using (var arena = new TestArena())
                        {
                            var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                            var relPath = GetRandomString(2);
                            var data = GetRandomBytes(100);
                            arena.CreateResource(
                                arena.SourcePath,
                                relPath,
                                data);
                            var historyRepo = Substitute.For<ITargetHistoryRepository>();
                            historyRepo.Exists(relPath)
                                .Returns(true);
                            var targetFilePath = Path.Combine(arena.TargetPath, relPath);
                            var historyItem = new HistoryItem(
                                relPath,
                                data.Length);
                            historyRepo.Find(relPath)
                                .Returns(historyItem);

                            var sut = Create(targetHistoryRepository: historyRepo);
                            // Act
                            sut.Synchronize(source, target);
                            // Assert
                            Expect(targetFilePath)
                                .Not.To.Be.A.File();
                        }
                    }

                    [Test]
                    public void ShouldUpsertHistory()
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
                            var historyRepo = Substitute.For<ITargetHistoryRepository>();

                            var sut = Create(targetHistoryRepository: historyRepo);
                            // Act
                            sut.Synchronize(source, target);
                            // Assert
                            Expect(historyRepo)
                                .To.Have.Received(1)
                                .Upsert(Arg.Is<IHistoryItem>(
                                    o => o.Path == relPath &&
                                         o.Size == data.Length
                                ));
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
                                },
                                // this test is about notifiers, so we'll
                                // just pretend that all sources are to be required
                                // at the target
                                filters: CreatePermissiveFilter());
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
                    arena.CreateSourceResource(
                        "missing",
                        missingData);
                    var partialFileAllData = "hello world".AsBytes();
                    var partialTargetData = "hello".AsBytes();
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
                        DefaultResourceMatchers
                    );
                    // Act
                    sut.Synchronize(
                        arena.SourceFileSystem,
                        arena.TargetFileSystem);
                    // Assert
                    var partialResultData = arena.TargetFileSystem.ReadAllBytes(
                        "partial");
                    Expect(partialResultData)
                        .To.Equal(partialFileAllData,
                            () => $@"Expected {
                                    partialFileAllData.Length
                                } bytes, but got {
                                    partialResultData.Length
                                }: '{
                                    partialFileAllData.AsString()
                                }' vs '{
                                    partialResultData.AsString()
                                }'");
                    Expect(arena.TargetFileSystem.ReadAllBytes("existing"))
                        .To.Equal(existingData);
                    Expect(arena.TargetFileSystem.ReadAllBytes("missing"))
                        .To.Equal(missingData);
                    Expect(notifyable.BatchStartedNotifications)
                        .To.Contain.Only(1)
                        .Item(); // should only have one initial notification
                    var initial = notifyable.BatchStartedNotifications.Single();
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

                    var batchComplete = notifyable.BatchCompletedNotifications.Single();
                    Expect(batchComplete)
                        .To.Contain
                        .Only(2)
                        .Items(); // should only have partial and missing in sync queue
                    Expect(batchComplete)
                        .To.Contain.Exactly(1)
                        .Matched.By(
                            resource => resource.RelativePath == "missing"
                        );
                    Expect(batchComplete)
                        .To.Contain.Exactly(1)
                        .Matched.By(
                            resource => resource.RelativePath == "partial"
                        );

                    Expect(notifyable.ResourceNotifications)
                        .To.Contain.Exactly(2)
                        .Items();
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

                    Expect(notifyable.CompletedNotifications)
                        .To.Contain.Exactly(2)
                        .Items();
                    missingResource = notifyable.CompletedNotifications.First();
                    Expect(missingResource.source.RelativePath)
                        .To.Equal("missing");
                    Expect(missingResource.source.Size)
                        .To.Equal(missingData.Length);
                    Expect(missingResource.target)
                        .Not.To.Be.Null();
                    Expect(missingResource.target.RelativePath)
                        .To.Equal("missing");
                    Expect(missingResource.target.Size)
                        .To.Equal(missingData.Length);
                    partialResource = notifyable.CompletedNotifications.Second();
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
            public List<IEnumerable<IFileResource>> BatchStartedNotifications { get; }
                = new List<IEnumerable<IFileResource>>();

            public List<IEnumerable<IFileResource>> BatchCompletedNotifications { get; }
                = new List<IEnumerable<IFileResource>>();

            public List<(IFileResource source, IFileResource target)> ResourceNotifications { get; }
                = new List<(IFileResource source, IFileResource target)>();

            public List<(IFileResource source, IFileResource target)> CompletedNotifications { get; }
                = new List<(IFileResource source, IFileResource target)>();

            public NotifiableGenericPassThrough(
                Action<byte[], int> onWrite,
                Action onEnd)
                : base(onWrite, onEnd)
            {
            }

            public void NotifySyncBatchStart(
                IEnumerable<IFileResource> sourceResources)
            {
                BatchStartedNotifications.Add(sourceResources);
            }

            public void NotifySyncBatchComplete(
                IEnumerable<IFileResource> sourceResources)
            {
                BatchCompletedNotifications.Add(sourceResources);
            }

            public void NotifySyncStart(
                IFileResource sourceResource,
                IFileResource targetResource)
            {
                ResourceNotifications.Add((sourceResource, targetResource));
            }

            public void NotifySyncComplete(
                IFileResource sourceResource,
                IFileResource targetResource)
            {
                CompletedNotifications.Add((sourceResource, targetResource));
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

        private static IFilter[] CreatePermissiveFilter()
        {
            var filter = Substitute.For<IFilter>();
            filter.Filter(
                    Arg.Any<IFileResource>(),
                    Arg.Any<IEnumerable<IFileResource>>(),
                    Arg.Any<ITargetHistoryRepository>()
                )
                .Returns(FilterResult.Include);
            return new[] { filter };
        }

        private static ISynchronizer Create(
            params IPassThrough[] intermediatePipes)
        {
            return Create(null, intermediatePipes);
        }

        private static ISynchronizer Create(
            IResumeStrategy resumeStrategy = null,
            IPassThrough[] intermediatePipes = null,
            IResourceMatcher[] resourceMatchers = null,
            ITargetHistoryRepository targetHistoryRepository = null,
            IFilter[] filters = null)
        {
            return new Synchronizer(
                targetHistoryRepository ?? Substitute.For<ITargetHistoryRepository>(),
                resumeStrategy ?? new AlwaysResumeStrategy(),
                intermediatePipes ?? new IPassThrough[0],
                resourceMatchers ?? DefaultResourceMatchers,
                filters ?? DefaultFilters
            );
        }

        private static readonly IResourceMatcher[] DefaultResourceMatchers =
        {
            new SameRelativePathMatcher(),
            new SameSizeMatcher()
        };

        private static readonly IFilter[] DefaultFilters =
        {
            new TargetOptInFilter(),
            new SimpleTargetExistsFilter(),
            new NoDotFilesFilter()
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