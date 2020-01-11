using System.IO;
using bitsplat.Storage;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests.Storage
{
    [TestFixture]
    public class TestCachingFileSystem
    {
        [TestFixture]
        public class ShouldNotCacheCallsTo : TestCachingFileSystem
        {
            [Test]
            public void Open()
            {
                // Arrange
                var underlying = Substitute.For<IFileSystem>();
                underlying.Open(Arg.Any<string>(), Arg.Any<FileMode>())
                    .Returns(ci => new MemoryStream());
                var sut = Create(underlying);
                var path = GetRandomString();
                var otherPath = GetAnother(path);
                var otherMode = GetRandom<FileMode>();
                // Act
                var result1 = sut.Open(path, FileMode.Append);
                var result2 = sut.Open(path, FileMode.Append);
                var result3 = sut.Open(path, FileMode.Create);
                var result4 = sut.Open(path, FileMode.Open);
                var result5 = sut.Open(path, FileMode.Truncate);
                var result6 = sut.Open(path, FileMode.CreateNew);
                var result7 = sut.Open(path, FileMode.OpenOrCreate);
                var result8 = sut.Open(otherPath, otherMode);
                // Assert
                Expect(new[]
                    {
                        result1,
                        result2,
                        result3,
                        result4,
                        result5,
                        result6,
                        result7,
                        result8
                    }).To.Be.Distinct();
                Received.InOrder(() =>
                {
                    underlying.Open(path, FileMode.Append);
                    underlying.Open(path, FileMode.Append);
                    underlying.Open(path, FileMode.Create);
                    underlying.Open(path, FileMode.Open);
                    underlying.Open(path, FileMode.Truncate);
                    underlying.Open(path, FileMode.CreateNew);
                    underlying.Open(path, FileMode.OpenOrCreate);
                    underlying.Open(otherPath, otherMode);
                });
            }

            [Test]
            public void Delete()
            {
                // Arrange
                var path1 = GetRandomString();
                var path2 = GetAnother(path1);
                var underlying = Substitute.For<IFileSystem>();
                var sut = Create(underlying);
                // Act
                sut.Delete(path1);
                sut.Delete(path1);
                sut.Delete(path2);
                // Assert
                Received.InOrder(() =>
                {
                    underlying.Delete(path1);
                    underlying.Delete(path1);
                    underlying.Delete(path2);
                });
            }

            [Test]
            public void BasePath()
            {
                // Arrange
                var expected1 = GetRandomString();
                var expected2 = GetAnother(expected1);
                var underlying = Substitute.For<IFileSystem>();
                underlying.BasePath.Returns(expected1);
                var sut = Create(underlying);
                // Act
                var result1 = sut.BasePath;
                var result2 = sut.BasePath;
                underlying.BasePath.Returns(expected2);
                var result3 = sut.BasePath;
                // Assert
                Expect(result1)
                    .To.Equal(result2)
                    .And.To.Equal(expected1);
                
                Expect(result3)
                    .To.Equal(expected2);
            }
        }

        [TestFixture]
        public class ShouldCacheCallsTo : TestCachingFileSystem
        {
            [Test]
            public void Exists()
            {
                // Arrange
                var underlying = Substitute.For<IFileSystem>();
                var sut = Create(underlying);
                var path1 = GetRandomString();
                var path2 = GetAnother(path1);
                var expected1 = GetRandomBoolean();
                var expected2 = GetRandomBoolean();
                underlying.Exists(path1)
                    .Returns(expected1);
                underlying.Exists(path2)
                    .Returns(expected2);
                underlying.ClearReceivedCalls();
                // Act
                var result1 = sut.Exists(path1);
                var result2 = sut.Exists(path1);
                var result3 = sut.Exists(path2);
                var result4 = sut.Exists(path2);
                // Assert
                Expect(result1)
                    .To.Equal(result2);
                Expect(result1)
                    .To.Equal(expected1);
                Expect(underlying)
                    .To.Have.Received(1)
                    .Exists(path1);

                Expect(result3)
                    .To.Equal(result4);
                Expect(result4)
                    .To.Equal(expected2);
                Expect(underlying)
                    .To.Have.Received(1)
                    .Exists(path2);
            }

            [Test]
            public void IsFile()
            {
                // Arrange
                var underlying = Substitute.For<IFileSystem>();
                var sut = Create(underlying);
                var path1 = GetRandomString();
                var path2 = GetAnother(path1);
                var expected1 = GetRandomBoolean();
                var expected2 = GetRandomBoolean();
                underlying.IsFile(path1)
                    .Returns(expected1);
                underlying.IsFile(path2)
                    .Returns(expected2);
                underlying.ClearReceivedCalls();
                // Act
                var result1 = sut.IsFile(path1);
                var result2 = sut.IsFile(path1);
                var result3 = sut.IsFile(path2);
                var result4 = sut.IsFile(path2);
                // Assert
                Expect(result1)
                    .To.Equal(result2);
                Expect(result1)
                    .To.Equal(expected1);
                Expect(underlying)
                    .To.Have.Received(1)
                    .IsFile(path1);

                Expect(result3)
                    .To.Equal(result4);
                Expect(result4)
                    .To.Equal(expected2);
                Expect(underlying)
                    .To.Have.Received(1)
                    .IsFile(path2);
            }

            [Test]
            public void IsDirectory()
            {
                // Arrange
                var underlying = Substitute.For<IFileSystem>();
                var sut = Create(underlying);
                var path1 = GetRandomString();
                var path2 = GetAnother(path1);
                var expected1 = GetRandomBoolean();
                var expected2 = GetRandomBoolean();
                underlying.IsDirectory(path1)
                    .Returns(expected1);
                underlying.IsDirectory(path2)
                    .Returns(expected2);
                underlying.ClearReceivedCalls();
                // Act
                var result1 = sut.IsDirectory(path1);
                var result2 = sut.IsDirectory(path1);
                var result3 = sut.IsDirectory(path2);
                var result4 = sut.IsDirectory(path2);
                // Assert
                Expect(result1)
                    .To.Equal(result2);
                Expect(result1)
                    .To.Equal(expected1);
                Expect(underlying)
                    .To.Have.Received(1)
                    .IsDirectory(path1);

                Expect(result3)
                    .To.Equal(result4);
                Expect(result4)
                    .To.Equal(expected2);
                Expect(underlying)
                    .To.Have.Received(1)
                    .IsDirectory(path2);
            }

            [Test]
            public void ListResourcesRecursive()
            {
                // Arrange
                var underlying = Substitute.For<IFileSystem>();
                var expected = underlying.ListResourcesRecursive();
                underlying.ClearReceivedCalls();
                var sut = Create(underlying);
                // Act
                var result1 = sut.ListResourcesRecursive();
                var result2 = sut.ListResourcesRecursive();
                // Assert
                Expect(result1)
                    .To.Deep.Equal(expected);
                Expect(result2)
                    .To.Deep.Equal(expected);
                Expect(underlying)
                    .To.Have.Received(1)
                    .ListResourcesRecursive();
            }

            [Test]
            public void FetchSize()
            {
                // Arrange
                var underlying = Substitute.For<IFileSystem>();
                var sut = Create(underlying);
                var path1 = GetRandomString();
                var path2 = GetAnother(path1);
                var expected1 = GetRandomInt();
                var expected2 = GetRandomInt();
                underlying.FetchSize(path1)
                    .Returns(expected1);
                underlying.FetchSize(path2)
                    .Returns(expected2);
                underlying.ClearReceivedCalls();
                // Act
                var result1 = sut.FetchSize(path1);
                var result2 = sut.FetchSize(path1);
                var result3 = sut.FetchSize(path2);
                var result4 = sut.FetchSize(path2);
                // Assert
                Expect(result1)
                    .To.Equal(result2);
                Expect(result1)
                    .To.Equal(expected1);
                Expect(underlying)
                    .To.Have.Received(1)
                    .FetchSize(path1);

                Expect(result3)
                    .To.Equal(result4);
                Expect(result4)
                    .To.Equal(expected2);
                Expect(underlying)
                    .To.Have.Received(1)
                    .FetchSize(path2);
            }
        }

        [TestFixture]
        public class WhenDeleting: TestCachingFileSystem
        {
            [TestFixture]
            public class WhenNoOptions: WhenDeleting
            {
                [Test]
                public void ShouldRemoveFromCachedList()
                {
                    // Arrange
                    var underlying = Substitute.For<IFileSystem>();
                    var resources = GetRandomArray<IReadWriteFileResource>(2);
                    var toDelete = GetRandomFrom(resources);
                    underlying.ListResourcesRecursive()
                        .Returns(resources);
                    underlying.ListResourcesRecursive(Arg.Any<ListOptions>())
                        .Returns(resources);
                    var sut = Create(underlying);
                    // Act
                    var first = sut.ListResourcesRecursive();
                    sut.Delete(toDelete.RelativePath);
                    var second = sut.ListResourcesRecursive();
                    // Assert
                    Expect(first)
                        .To.Contain.Exactly(1)
                        .Equal.To(toDelete);
                    Expect(second)
                        .Not.To.Contain.Any()
                        .Equal.To(toDelete);
                }
            }

            [TestFixture]
            public class WhenHaveOptions: WhenDeleting
            {
                [Test]
                public void ShouldRemoveFromCachedList()
                {
                    // Arrange
                    var underlying = Substitute.For<IFileSystem>();
                    var resources = GetRandomArray<IReadWriteFileResource>(2);
                    var toDelete = GetRandomFrom(resources);
                    underlying.ListResourcesRecursive()
                        .Returns(resources);
                    underlying.ListResourcesRecursive(Arg.Any<ListOptions>())
                        .Returns(resources);
                    var listOptions = GetRandom<ListOptions>();
                    var sut = Create(underlying);
                    // Act
                    var first = sut.ListResourcesRecursive(listOptions);
                    sut.Delete(toDelete.RelativePath);
                    var second = sut.ListResourcesRecursive(listOptions);
                    // Assert
                    Expect(first)
                        .To.Contain.Exactly(1)
                        .Equal.To(toDelete);
                    Expect(second)
                        .Not.To.Contain.Any()
                        .Equal.To(toDelete);
                }
            }
        }

        private IFileSystem Create(IFileSystem underlying)
        {
            return new CachingFileSystem(underlying);
        }
    }
}