using System.Linq;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Storage;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests.History
{
    [TestFixture]
    public class TestGreedyFilter
    {
        [TestFixture]
        public class WhenResourceNotFoundAtTarget : TestGreedyFilter
        {
            [Test]
            public void ShouldInclude()
            {
                // Arrange
                var targetResources = GetRandomCollection<IFileResource>(3);
                var sourceResource = GetAnother(targetResources);
                var historyRepo = Substitute.For<ITargetHistoryRepository>();
                var sut = Create();
                // Act
                var result = sut.Filter(
                    sourceResource,
                    targetResources,
                    historyRepo
                );
                // Assert
                Expect(result)
                    .To.Equal(FilterResult.Include);
                Expect(historyRepo.ReceivedCalls())
                    .To.Be.Empty();
            }
        }

        [TestFixture]
        public class WhenResourceSizeMismatchAtTarget : TestGreedyFilter
        {
            [Test]
            public void ShouldInclude()
            {
                // Arrange
                var targetResources = GetRandomCollection<IFileResource>(3);
                var similar = GetRandomFrom(targetResources);
                var sourceResource = Substitute.For<IFileResource>();
                sourceResource.Name.Returns(similar.Name);
                // Path should be different between them
                sourceResource.Path.Returns($"{GetRandomString()}/{similar.Path}");
                sourceResource.RelativePath.Returns(similar.RelativePath);
                var delta = GetRandomCollection<int>(
                        () => GetRandomInt(-10, 10),
                        20
                    )
                    .FirstOrDefault(i => i != 0);
                var sourceSize = similar.Size + delta;
                sourceResource.Size.Returns(sourceSize);
                var historyRepo = Substitute.For<ITargetHistoryRepository>();
                var sut = Create();
                // Act
                var result = sut.Filter(
                    sourceResource,
                    targetResources,
                    historyRepo
                );
                // Assert
                Expect(result)
                    .To.Equal(FilterResult.Include);
                Expect(historyRepo.ReceivedCalls())
                    .To.Be.Empty();
            }
        }

        [TestFixture]
        public class WhenResourceFoundAndSizeMatchesAtTarget : TestGreedyFilter
        {
            [Test]
            public void ShouldBeAmbivalent()
            {
                // Arrange
                var targetResources = GetRandomCollection<IFileResource>(3);
                var sourceResource = GetRandomFrom(targetResources);
                var currentPath = sourceResource.Path;
                // Path should be different between them
                sourceResource.Path.Returns($"{GetRandomString(1)}/{currentPath}");
                var historyRepo = Substitute.For<ITargetHistoryRepository>();
                var sut = Create();
                // Act
                var result = sut.Filter(
                    sourceResource,
                    targetResources,
                    historyRepo
                );
                // Assert
                Expect(result)
                    .To.Equal(FilterResult.Ambivalent);
                Expect(historyRepo.ReceivedCalls())
                    .To.Be.Empty();
            }
        }

        private IFilter Create()
        {
            return new GreedyFilter();
        }
    }
}