using System.IO;
using System.Linq;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Pipes;
using bitsplat.ResumeStrategies;
using bitsplat.Tests.TestingSupport;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static NExpect.Expectations;

namespace bitsplat.Tests.ResumeStrategies
{
    [TestFixture]
    public class TestSimpleResumeStrategy
    {
        [TestFixture]
        public class WhenFileExists
        {
            [TestFixture]
            public class AndSmaller
            {
                [TestFixture]
                public class AndTrailingBytesMatch
                {
                    [Test]
                    [Repeat(15)]
                    public void ShouldAllow()
                    {
                        // Arrange
                        using var arena = new TestArena();
                        var sourceData = RandomBytes();
                        var relPath = GetRandomString(10);
                        arena.CreateSourceResource(
                            relPath,
                            sourceData);
                        var targetData = sourceData.Take(GetRandomInt(50, 100))
                            .ToArray();
                        var targetPath = arena.CreateTargetResource(
                            relPath,
                            targetData);
                        var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                        var sut = Create();
                        var synchronizer = CreateSynchronizer(sut);
                        var sourceResource = arena.SourceFileSystem.ListResourcesRecursive()
                            .Single(r => r.RelativePath == relPath);
                        var targetResource = arena.TargetFileSystem.ListResourcesRecursive()
                            .Single(r => r.RelativePath == relPath);
                        var sourceStream = arena.SourceFileSystem.Open(
                            relPath,
                            FileMode.OpenOrCreate,
                            FileAccess.Read
                        );
                        var targetStream = arena.TargetFileSystem.Open(
                            relPath,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite);
                        // Act
                        var result = sut.CanResume(
                            sourceResource,
                            targetResource,
                            sourceStream,
                            targetStream);
                        sourceStream.Dispose();
                        targetStream.Dispose();
                        Expect(result)
                            .To.Be.True();

                        synchronizer.Synchronize(source, target);
                        // Assert
                        var onDisk = File.ReadAllBytes(targetPath);
                        Expect(onDisk)
                            .To.Equal(
                                sourceData,
                                () => "Should concatenated new data onto existing data, skipping existing bytes"
                            );
                    }
                }

                private static byte[] RandomBytes()
                {
                    return GetRandomBytes(
                        (int) (SimpleResumeStrategy.DEFAULT_CHECK_BYTES * 0.5),
                        SimpleResumeStrategy.DEFAULT_CHECK_BYTES * 2
                    );
                }

                [TestFixture]
                public class AndTrailingBytesDoNotMatch
                {
                    [Test]
                    [Repeat(15)]
                    public void ShouldNotAllow()
                    {
                        // Arrange
                        var arena = new TestArena();
                        var expected = RandomBytes();
                        var partial = expected
                            .Take(GetRandomInt(512, 600))
                            .Union(GetRandomBytes(100, 150))
                            .ToArray();
                        var relPath = GetRandomString(10);
                        var targetPath = arena.CreateTargetResource(
                            relPath,
                            partial);
                        arena.CreateSourceFile(
                            relPath,
                            expected);
                        var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                        // Act
                        var sut = Create();
                        var synchronizer = CreateSynchronizer(sut);
                        var sourceResource = arena.SourceFileSystem.ListResourcesRecursive()
                            .Single(r => r.RelativePath == relPath);
                        var targetResource = arena.TargetFileSystem.ListResourcesRecursive()
                            .Single(r => r.RelativePath == relPath);
                        var sourceStream = arena.SourceFileSystem.Open(
                            relPath,
                            FileMode.OpenOrCreate,
                            FileAccess.Read);
                        var targetStream = arena.TargetFileSystem.Open(
                            relPath,
                            FileMode.OpenOrCreate,
                            FileAccess.ReadWrite);
                        // Act
                        var result = sut.CanResume(
                            sourceResource,
                            targetResource,
                            sourceStream,
                            targetStream);
                        sourceStream.Dispose();
                        targetStream.Dispose();

                        synchronizer.Synchronize(source, target);
                        // Assert
                        Expect(result)
                            .To.Be.False(() => "should not be able to resume");
                        var onDisk = File.ReadAllBytes(targetPath);
                        Expect(onDisk)
                            .To.Equal(
                                expected,
                                () => "Should rewrite entire file"
                            );
                    }
                }
            }
        }

        private static IResumeStrategy Create(
            IOptions options = null,
            IMessageWriter messageWriter = null)
        {
            options ??= CreateDefaultOptions();
            return new SimpleResumeStrategy(
                options,
                messageWriter ?? Substitute.For<IMessageWriter>()
            );
        }

        private static IOptions CreateDefaultOptions()
        {
            var opts = Substitute.For<IOptions>();
            opts.ResumeCheckBytes.Returns(SimpleResumeStrategy.DEFAULT_CHECK_BYTES);
            return opts;
        }

        private static ISynchronizer CreateSynchronizer(
            IResumeStrategy resumeStrategy = null,
            ITargetHistoryRepository targetHistoryRepository = null,
            IProgressReporter progressReporter = null,
            params IPassThrough[] intermediatePipes)
        {
            var options = Substitute.For<IOptions>();
            options.ResumeCheckBytes.Returns(512);
            return new Synchronizer(
                Substitute.For<ITargetHistoryRepository>(),
                resumeStrategy ?? new SimpleResumeStrategy(options, Substitute.For<IMessageWriter>()),
                intermediatePipes,
                new IFilter[] { new TargetOptInFilter() },
                progressReporter ?? new FakeProgressReporter(),
                Substitute.For<IOptions>()
            );
        }
    }
}