using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using bitsplat.Pipes;
using bitsplat.Storage;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using NUnit.Framework;
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
                        var sut = Create(resumeStrategy, intermediate);
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
                        var sut = Create(resumeStrategy, intermediate);
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
                        var sut = Create(resumeStrategy, intermediate1, intermediate2, intermediate3);
                        // Act
                        sut.Synchronize(source, target);
                        // Assert
                        Expect(intermediateCalls).To.Equal(expected);
                        Expect(endCalls).To.Equal(expected);
                    }
                }
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
            params IPassThrough[] intermediatePipes)
        {
            return new Synchronizer(
                resumeStrategy ?? new AlwaysResumeStrategy(),
                intermediatePipes
            );
        }
    }
}