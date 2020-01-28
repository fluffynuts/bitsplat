using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Pipes;
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
                        using var arena = new TestArena();
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

                    [Test]
                    public void ShouldUpsertHistory()
                    {
                        // Arrange
                        using var arena = new TestArena();
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

                [TestFixture]
                public class WhenSourceHasFileAndTargetHasSameFile
                {
                    [Test]
                    public void ShouldNotReWriteTheFile()
                    {
                        // Arrange
                        using var arena = new TestArena();
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

                    [Test]
                    public void ShouldNotReWriteTheFileWhenInHistoryButNotOnDisk()
                    {
                        // Arrange
                        using var arena = new TestArena();
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

                    [Test]
                    public void ShouldUpsertHistory()
                    {
                        // Arrange
                        using var arena = new TestArena();
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
                            .Upsert(Arg.Is<IEnumerable<IHistoryItem>>(
                                o => o.Single()
                                         .Path ==
                                     relPath &&
                                     o.Single()
                                         .Size ==
                                     data.Length
                            ));
                    }
                }

                [TestFixture]
                public class WhenSourceHasFileAndTargetHasPartialFileWithNoErrors
                {
                    [Test]
                    public void ShouldResumeWhenResumeStrategySaysYes()
                    {
                        // Arrange
                        using var arena = new TestArena();
                        var resumeStrategy = Substitute.For<IResumeStrategy>();
                        resumeStrategy.CanResume(
                                Arg.Any<IFileResource>(),
                                Arg.Any<IFileResource>(),
                                Arg.Any<Stream>(),
                                Arg.Any<Stream>()
                            )
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

                    [Test]
                    public void ShouldNotResumeIfResumeStrategySaysNo()
                    {
                        // Arrange
                        using var arena = new TestArena();
                        var resumeStrategy = Substitute.For<IResumeStrategy>();
                        resumeStrategy.CanResume(
                                Arg.Any<IFileResource>(),
                                Arg.Any<IFileResource>(),
                                Arg.Any<Stream>(),
                                Arg.Any<Stream>())
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

                    [Test]
                    public void ShouldPassThroughAllProvidedIntermediatesInOrder()
                    {
                        // Arrange
                        using var arena = new TestArena();
                        var resumeStrategy = Substitute.For<IResumeStrategy>();
                        resumeStrategy.CanResume(
                                Arg.Any<IFileResource>(),
                                Arg.Any<IFileResource>(),
                                Arg.Any<Stream>(),
                                Arg.Any<Stream>())
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

        [TestFixture]
        public class ErrorHandling
        {
            [Test]
            public void ShouldRetryOnError()
            {
                // Arrange
                using var arena = new TestArena();
                var options = Substitute.For<IOptions>();
                options.Retries.Returns(3);
                
                using var resetter = new AutoResetter<int>(() =>
                    {
                        var original = StreamSource.MaxBuffer;
                        StreamSource.MaxBuffer = 1;
                        return original;
                    },
                    original => StreamSource.MaxBuffer = original);
                var haveErrored = false;
                var errorPassThrough = new GenericPassThrough(
                    (data, size) =>
                    {
                        if (!haveErrored)
                        {
                            haveErrored = true;
                            throw new DirectoryNotFoundException("foo");
                        }
                    },
                    () =>
                    {
                    }
                );
                // keep the size small since we're forcing a flush at every byte
                var sourceFile = arena.CreateSourceFile(data: GetRandomBytes(10, 20));
                var expected = arena.TargetPathFor(sourceFile);
                var sut = Create(
                    intermediatePipes: new IPassThrough[] { errorPassThrough }, 
                    options: options);
                // Act
                sut.Synchronize(arena.SourceFileSystem, arena.TargetFileSystem);
                // Assert
                Expect(expected)
                    .To.Exist();
                Expect(File.ReadAllBytes(expected))
                    .To.Equal(sourceFile.Data);
            }
        }

        [TestFixture]
        public class NotifyingNotifiablePipes
        {
            [Test]
            public void ShouldNotifyNotifiablePipesWithPossibleResumeSizes()
            {
                // Arrange
                using var arena = new TestArena();
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
                var notifiable = new NotifiableGenericPassThrough(
                    (data, count) =>
                    {
                    },
                    () =>
                    {
                    });
                var options = Substitute.For<IOptions>();
                options.ResumeCheckBytes.Returns(512);
                var sut = Create(
                    new SimpleResumeStrategy(options),
                    new IPassThrough[] { notifiable, intermediate1 }
                        .Randomize()
                        .ToArray()
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
                Expect(notifiable.BatchStartedNotifications)
                    .To.Contain.Only(1)
                    .Item(); // should only have one initial notification
                var initial = notifiable.BatchStartedNotifications.Single();
                Expect(initial.resources)
                    .To.Contain
                    .Only(2)
                    .Items(); // should only have partial and missing in sync queue
                Expect(initial.resources)
                    .To.Contain.Exactly(1)
                    .Matched.By(
                        resource => resource.RelativePath == "missing"
                    );
                Expect(initial.resources)
                    .To.Contain.Exactly(1)
                    .Matched.By(
                        resource => resource.RelativePath == "partial"
                    );

                var batchComplete = notifiable.BatchCompletedNotifications.Single();
                Expect(batchComplete.resources)
                    .To.Contain
                    .Only(2)
                    .Items(); // should only have partial and missing in sync queue
                Expect(batchComplete.resources)
                    .To.Contain.Exactly(1)
                    .Matched.By(
                        resource => resource.RelativePath == "missing"
                    );
                Expect(batchComplete.resources)
                    .To.Contain.Exactly(1)
                    .Matched.By(
                        resource => resource.RelativePath == "partial"
                    );

                Expect(notifiable.ResourceNotifications)
                    .To.Contain.Exactly(2)
                    .Items();
                var missingResource = notifiable.ResourceNotifications.First();
                Expect(missingResource.source.RelativePath)
                    .To.Equal("missing");
                Expect(missingResource.source.Size)
                    .To.Equal(missingData.Length);
                Expect(missingResource.target)
                    .To.Be.Null();
                var partialResource = notifiable.ResourceNotifications.Second();
                Expect(partialResource.source.RelativePath)
                    .To.Equal("partial");
                Expect(partialResource.source.Size)
                    .To.Equal(partialFileAllData.Length);
                Expect(partialResource.target.RelativePath)
                    .To.Equal("partial");
                Expect(partialResource.target.Size)
                    .To.Equal(partialTargetData.Length);

                Expect(notifiable.CompletedNotifications)
                    .To.Contain.Exactly(2)
                    .Items();
                missingResource = notifiable.CompletedNotifications.First();
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
                partialResource = notifiable.CompletedNotifications.Second();
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

        public class NotifiableGenericPassThrough
            : GenericPassThrough,
              ISyncQueueNotifiable
        {
            public List<(string label, IEnumerable<IFileResource> resources)> BatchStartedNotifications { get; }
                = new List<(string label, IEnumerable<IFileResource> resources)>();

            public List<(string label, IEnumerable<IFileResource> resources)> BatchCompletedNotifications { get; }
                = new List<(string label, IEnumerable<IFileResource> resources)>();

            public List<(IFileResource source, IFileResource target)> ResourceNotifications { get; }
                = new List<(IFileResource source, IFileResource target)>();

            public List<(IFileResource source, IFileResource target)> CompletedNotifications { get; }
                = new List<(IFileResource source, IFileResource target)>();

            public List<(IFileResource source, IFileResource target, Exception ex)> ErrorNotifications { get; }
                = new List<(IFileResource source, IFileResource target, Exception ex)>();

            public NotifiableGenericPassThrough(
                Action<byte[], int> onWrite,
                Action onEnd)
                : base(onWrite, onEnd)
            {
            }

            public void NotifySyncBatchStart(
                string label,
                IEnumerable<IFileResource> sourceResources)
            {
                BatchStartedNotifications.Add((label, sourceResources));
            }

            public void NotifySyncBatchComplete(
                string label,
                IEnumerable<IFileResource> sourceResources)
            {
                BatchCompletedNotifications.Add((label, sourceResources));
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

            public void NotifyError(
                IFileResource sourceResource,
                IFileResource targetResource,
                Exception ex)
            {
                // TODO: test me
                ErrorNotifications.Add((sourceResource, targetResource, ex));
            }

            public void NotifyNoWork(
                IFileSystem source,
                IFileSystem target)
            {
            }
        }

        public class GenericFilter : IFilter
        {
            private Func<IFileResource, IEnumerable<IFileResource>, ITargetHistoryRepository, IFileSystem, IFileSystem,
                FilterResult> _filter;

            public GenericFilter(
                Func<
                    IFileResource,
                    IEnumerable<IFileResource>,
                    ITargetHistoryRepository,
                    IFileSystem,
                    IFileSystem,
                    FilterResult> filter)
            {
                _filter = filter;
            }

            public FilterResult Filter(
                IFileResource sourceResource,
                IEnumerable<IFileResource> targetResources,
                ITargetHistoryRepository targetHistoryRepository,
                IFileSystem source,
                IFileSystem target)
            {
                return _filter(
                    sourceResource,
                    targetResources,
                    targetHistoryRepository,
                    source,
                    target);
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
                    Arg.Any<ITargetHistoryRepository>(),
                    Arg.Any<IFileSystem>(),
                    Arg.Any<IFileSystem>()
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
            ITargetHistoryRepository targetHistoryRepository = null,
            IFilter[] filters = null,
            IProgressReporter progressReporter = null,
            IOptions options = null)
        {
            options ??= CreateDefaultOptions();
            return new Synchronizer(
                targetHistoryRepository ?? Substitute.For<ITargetHistoryRepository>(),
                resumeStrategy ?? new SimpleResumeStrategy(options),
                intermediatePipes ?? new IPassThrough[0],
                filters ?? DefaultFilters,
                progressReporter ?? new FakeProgressReporter(),
                options ?? Substitute.For<IOptions>()
            );
        }

        private static IOptions CreateDefaultOptions()
        {
            var opts = Substitute.For<IOptions>();
            opts.ResumeCheckBytes.Returns(512);
            return opts;
        }

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
            using var stream = fileSystem.Open(name, FileMode.Open);
            return stream.ReadAllBytes();
        }
    }
}