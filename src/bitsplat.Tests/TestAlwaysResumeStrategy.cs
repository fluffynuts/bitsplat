using System.IO;
using System.Linq;
using bitsplat.History;
using bitsplat.Pipes;
using bitsplat.ResourceMatchers;
using bitsplat.ResumeStrategies;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using PeanutButter.Utils;
using static NExpect.Expectations;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
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
                var sourceData = GetRandomBytes(150, 200);
                var relPath = GetRandomString(10);
                arena.CreateSourceResource(
                    relPath,
                    sourceData);
                var targetData = GetRandomBytes(50, 100);
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
                Expect(result)
                    .To.Equal(
                        expected,
                        "Should concatenated new data onto existing data, skipping existing bytes");
            }
        }

        private static ISynchronizer Create(
            IResumeStrategy resumeStrategy = null,
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