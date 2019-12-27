using System.IO;
using System.Linq;
using bitsplat.Filters;
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
                var results = sources.Select(
                    s => sut.Filter(
                        s,
                        targets,
                        targetHistoryRepository)
                    ).ToArray();
                // Assert
                Expect(results).To.Contain.All()
                    .Matched.By(o => o == FilterResult.Ambivalent);
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
                var source1 = FileResource.For(sourceBase, GetRandomPath(), GetRandomInt());
                var source2 = FileResource.For(sourceBase, GetRandomPath(), GetRandomInt());
                var sourceRelativeBase = source1
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
                var result1 = sut.Filter(
                    source1,
                    targets,
                    targetHistoryRepository);
                var result2 = sut.Filter(
                    source2,
                    targets,
                    targetHistoryRepository);
                // Assert
                Expect(result1)
                    .To.Equal(FilterResult.Include);
                Expect(result2)
                    .To.Equal(FilterResult.Ambivalent);
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
                var source1 = FileResource.For(sourceBase, GetRandomPath(), GetRandomInt());
                var source2 = FileResource.For(sourceBase, GetRandomPath(), GetRandomInt());
                var sourceRelativeBase = source1
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
                var result1 = sut.Filter(
                    source1,
                    targets,
                    targetHistoryRepository);
                var result2 = sut.Filter(
                    source2,
                    targets,
                    targetHistoryRepository);
                // Assert
                Expect(result1)
                    .To.Equal(FilterResult.Include);
                Expect(result2)
                    .To.Equal(FilterResult.Ambivalent);
            }
        }

        private static IFilter Create()
        {
            return new TargetOptInFilter();
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