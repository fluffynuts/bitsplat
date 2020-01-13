using System.Linq;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Storage;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using PeanutButter.RandomGenerators;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests.History
{
    [TestFixture]
    public class TestGreedyFilter
    {
        [TestFixture]
        public class WhenNoHistoryOfTarget : TestGreedyFilter
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
                        historyRepo,
                        Substitute.For<IFileSystem>(),
                        Substitute.For<IFileSystem>()
                    );
                    // Assert
                    Expect(result)
                        .To.Equal(FilterResult.Include);
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
                        historyRepo,
                        Substitute.For<IFileSystem>(),
                        Substitute.For<IFileSystem>()
                    );
                    // Assert
                    Expect(result)
                        .To.Equal(FilterResult.Include);
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
                        historyRepo,
                        Substitute.For<IFileSystem>(),
                        Substitute.For<IFileSystem>()
                    );
                    // Assert
                    Expect(result)
                        .To.Equal(FilterResult.Ambivalent);
                }
            }
        }

        [TestFixture]
        public class WhenHaveHistoryOfTarget
        {
            [TestFixture]
            public class WhenResourceSizeMismatchInHistory : TestGreedyFilter
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
                    historyRepo.Find(similar.RelativePath)
                        .Returns(ci => similar.AsHistoryItem());
                    targetResources = targetResources.Except(new[] { similar });
                    var sut = Create();
                    // Act
                    var result = sut.Filter(
                        sourceResource,
                        targetResources,
                        historyRepo,
                        Substitute.For<IFileSystem>(),
                        Substitute.For<IFileSystem>()
                    );
                    // Assert
                    Expect(result)
                        .To.Equal(FilterResult.Include);
                }
            }

            [TestFixture]
            public class WhenResourceFoundAndSizeMatchesInHistory : TestGreedyFilter
            {
                [Test]
                public void ShouldBeAmbivalent()
                {
                    // Arrange
                    var targetResources = GetRandomCollection<IFileResource>(3);
                    var sourceResource = GetRandomFrom(targetResources);
                    targetResources = targetResources.Except(new[] { sourceResource });
                    var currentPath = sourceResource.Path;
                    // Path should be different between them
                    sourceResource.Path.Returns($"{GetRandomString(1)}/{currentPath}");
                    var historyRepo = Substitute.For<ITargetHistoryRepository>();
                    historyRepo.Find(sourceResource.RelativePath)
                        .Returns(ci => sourceResource.AsHistoryItem());
                    var sut = Create();
                    // Act
                    var result = sut.Filter(
                        sourceResource,
                        targetResources,
                        historyRepo,
                        Substitute.For<IFileSystem>(),
                        Substitute.For<IFileSystem>()
                    );
                    // Assert
                    Expect(result)
                        .To.Equal(FilterResult.Ambivalent);
                }
            }
        }

        private IFilter Create()
        {
            return new GreedyFilter();
        }
    }

    public static class IFileResourceExtensionsForTesting
    {
        public static HistoryItem AsHistoryItem(
            this IFileResource resource
        )
        {
            return HistoryItemBuilder.Create()
                .WithRandomProps()
                .ForFileResource(resource)
                .Build();
        }
    }

    public class HistoryItemBuilder
        : GenericBuilder<HistoryItemBuilder, HistoryItem>
    {
        public HistoryItemBuilder ForFileResource(
            IFileResource resource)
        {
            return WithPath(resource.RelativePath)
                .WithSize(resource.Size);
        }

        public HistoryItemBuilder WithPath(string path)
        {
            return WithProp(o => o.Path = path);
        }

        public HistoryItemBuilder WithSize(long size)
        {
            return WithProp(o => o.Size = size);
        }
    }
}