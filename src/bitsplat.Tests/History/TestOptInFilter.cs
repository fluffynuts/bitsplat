using System.IO;
using System.Linq;
using bitsplat.History;
using bitsplat.Storage;
using NUnit.Framework;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static bitsplat.Tests.RandomValueGen;

namespace bitsplat.Tests.History
{
    [TestFixture]
    public class TestOptInFilter
    {
        [TestFixture]
        public class WhenNoTargetHistoryOrTargetResources
        {
            [Test]
            public void ShouldReturnNoSourceResources()
            {
                // Arrange
                var sources = GetRandomCollection<IFileResource>();
                var targets = new IFileResource[0];
                var targetHistoryRepository = Substitute.For<ITargetHistoryRepository>();
                targetHistoryRepository.FindAll(Arg.Any<string>())
                    .Returns(new HistoryItem[0]);
                var sut = Create();
                // Act
                var result = sut.Filter(
                    sources,
                    targets,
                    targetHistoryRepository);
                // Assert
                Expect(result)
                    .To.Be.Empty();
            }
        }

        [TestFixture]
        public class WhenTargetFolderExists
        {
            [Test]
            public void ShouldIncludeSourcesMatchingTargetFolder()
            {
                // Arrange
                var sourceBase = GetRandomPath();
                var sources = new[]
                {
                    FileResource.For(sourceBase, GetRandomPath(), GetRandomInt()),
                    FileResource.For(sourceBase, GetRandomPath(), GetRandomInt())
                };
                var sourceRelativeBase = sources[0]
                    .RelativePath.Split(
                        Path.DirectorySeparatorChar
                    )
                    .First();
                var targetBase = GetRandomPath();
                var targets = new[]
                {
                    FileResource.For(
                        targetBase,
                        Path.Combine(sourceRelativeBase, GetRandomPath()),
                        GetRandomInt()
                    )
                };
                var targetHistoryRepository = Substitute.For<ITargetHistoryRepository>();
                targetHistoryRepository.FindAll(Arg.Any<string>())
                    .Returns(new HistoryItem[0]);
                var sut = Create();
                // Act
                var result = sut.Filter(
                    sources,
                    targets,
                    targetHistoryRepository);
                // Assert
                Expect(result)
                    .To.Contain(sources.First());
                Expect(result)
                    .Not.To.Contain(sources.Second());
            }
        }

        [TestFixture]
        public class WhenTargetFolderInHistory
        {
            [Test]
            public void ShouldIncludeSourcesMatchingTargetFolder()
            {
                // Arrange
                var sourceBase = GetRandomPath();
                var sources = new[]
                {
                    FileResource.For(sourceBase, GetRandomPath(), GetRandomInt()),
                    FileResource.For(sourceBase, GetRandomPath(), GetRandomInt())
                };
                var sourceRelativeBase = sources[0]
                    .RelativePath.Split(
                        Path.DirectorySeparatorChar
                    )
                    .First();
                var targets = new IFileResource[0];
                var targetHistoryRepository = Substitute.For<ITargetHistoryRepository>();
                targetHistoryRepository.FindAll(Arg.Is<string>(a => a == $"{sourceRelativeBase}/*"))
                    .Returns(new[]
                    {
                        new HistoryItem(
                            Path.Combine(sourceRelativeBase, GetRandomPath()),
                            GetRandomInt()
                        )
                    });
                var sut = Create();
                // Act
                var result = sut.Filter(
                    sources,
                    targets,
                    targetHistoryRepository);
                // Assert
                Expect(result)
                    .To.Contain(sources.First());
                Expect(result)
                    .Not.To.Contain(sources.Second());
            }
        }

        private static IFilter Create()
        {
            return new OptInFilter();
        }
    }

    public class FileResource : IFileResource
    {
        public string Path { get; }
        public long Size { get; }
        public string RelativePath { get; }

        public static FileResource For(
            string basePath,
            string relativePath,
            long size)
        {
            return new FileResource(
                basePath,
                relativePath,
                size);
        }

        public FileResource(
            string basePath,
            string relativePath,
            long size)
        {
            RelativePath = relativePath;
            Path = System.IO.Path.Combine(basePath, relativePath);
            Size = size;
        }
    }
}