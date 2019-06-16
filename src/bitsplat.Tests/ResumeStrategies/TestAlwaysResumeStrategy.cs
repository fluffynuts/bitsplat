using System.IO;
using System.Linq;
using bitsplat.History;
using bitsplat.Pipes;
using bitsplat.ResourceMatchers;
using bitsplat.ResumeStrategies;
using bitsplat.Tests.TestingSupport;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using PeanutButter.RandomGenerators;
using PeanutButter.Utils;

namespace bitsplat.Tests.ResumeStrategies
{
    [TestFixture]
    public class TestAlwaysResumeStrategy
    {
        [Test]
        public void ShouldAlwaysSayYes()
        {
            // Arrange
            using (var arena = new TestArena())
            {
                var sourceData = RandomValueGen.GetRandomBytes(150, 200);
                var relPath = RandomValueGen.GetRandomString(10);
                arena.CreateSourceResource(
                    relPath,
                    sourceData);
                var targetData = RandomValueGen.GetRandomBytes(50, 100);
                var targetPath = arena.CreateTargetResource(
                    relPath,
                    targetData);
                var expected = targetData
                    .And(sourceData.Skip(targetData.Length)
                        .ToArray());
                var (source, target) = (arena.SourceFileSystem, arena.TargetFileSystem);
                var sut = Create(new AlwaysResumeStrategy());
                // Act
                sut.Synchronize(source, target);
                // Assert
                var result = File.ReadAllBytes(targetPath);
                Expectations.Expect(result)
                    .To.Equal(
                        expected,
                        "Should concatenated new data onto existing data, skipping existing bytes");
            }
        }

        private static ISynchronizer Create(
            IResumeStrategy resumeStrategy = null,
            ITargetHistoryRepository targetHistoryRepository = null,
            params IPassThrough[] intermediatePipes)
        {
            return new Synchronizer(
                Substitute.For<ITargetHistoryRepository>(),
                resumeStrategy ?? new AlwaysResumeStrategy(),
                intermediatePipes,
                new IResourceMatcher[]
                {
                    new SameRelativePathMatcher(),
                    new SameSizeMatcher()
                }
            );
        }
    }
}