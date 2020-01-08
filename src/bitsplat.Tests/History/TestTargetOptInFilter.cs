using System.IO;
using System.Linq;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Storage;
using NUnit.Framework;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static bitsplat.Tests.RandomValueGen;

namespace bitsplat.Tests.History
{
    [TestFixture]
    public class TestTargetOptInFilter
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
                    .Matched.By(o => o == FilterResult.Exclude);
            }
        }

        [TestFixture]
        public class WhenTargetFolderExists
        {
            [Test]
            [Repeat(100)]
            public void ShouldIncludeSourcesMatchingTargetFolder()
            {
                // Arrange
                var sourceBase = GetRandomPath();
                var source1 = FakeFileResource.For(
                    sourceBase, 
                    GetRandomPath(2), 
                    GetRandomInt());
                var source2 = FakeFileResource.For(
                    sourceBase, 
                    // two sources must be in different primary folders
                    GetRandomPath(2), 
                    GetRandomInt());
                var sourceRelativeBaseParts = source1
                    .RelativePath.Split(Path.DirectorySeparatorChar);
                var sourceRelativeBase = 
                    sourceRelativeBaseParts.Length == 1
                    ? ""
                    : sourceRelativeBaseParts.First();
                var targetBase = GetRandomPath();
                var targets = new[]
                {
                    FakeFileResource.For(
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
                    .To.Equal(FilterResult.Exclude);
            }
        }

        [TestFixture]
        public class WhenTargetFolderInHistory
        {
            [Test]
            public void ShouldIncludeSourcesMatchingTargetFolder()
            {
                // Arrange
                var sourceBase = GetRandomPath(2);
                var source1 = FakeFileResource.For(sourceBase, GetRandomPath(2), GetRandomInt());
                var source2 = FakeFileResource.For(sourceBase, GetRandomPath(2), GetRandomInt());
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
                            Path.Combine(sourceRelativeBase, GetRandomPath(2)),
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
                    .To.Equal(FilterResult.Exclude);
            }
        }

        private static IFilter Create()
        {
            return new TargetOptInFilter();
        }
    }
}