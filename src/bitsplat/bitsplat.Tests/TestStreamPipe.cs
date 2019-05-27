using System.IO;
using bitsplat.Pipes;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static NExpect.Expectations;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using PeanutButter.Utils;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestPipeline
    {
        [Test]
        public void SimplePipeline()
        {
            // Arrange
            var data = GetRandomBytes(100);
            var sourceStream = new MemoryStream(data);
            var targetStream = new MemoryStream();
            using (var source = new StreamSource(sourceStream, false))
            using (var target = new StreamSink(targetStream, false))
            {
                // Act
                source
                    .Pipe(target)
                    .Drain();
                // Assert
                Expect(targetStream)
                    .To.Contain.Only(data);
            }
        }

        [Test]
        public void ThreeLevelPipeline()
        {
            // Arrange
            var intermediate = new NullPassThrough();
            var data = GetRandomBytes(100);
            var sourceStream = new MemoryStream(data);
            var targetStream = new MemoryStream();
            using (var source = new StreamSource(sourceStream, false))
            using (var target = new StreamSink(targetStream, false))
            {
                // Act
                source
                    .Pipe(intermediate)
                    .Pipe(target)
                    .Drain();
                // Assert
                Expect(targetStream)
                    .To.Contain.Only(data);
            }
        }
    }

    public static class StreamMatchers
    {
        public static void Only(
            this IContain<MemoryStream> contain,
            byte[] data)
        {
            contain.AddMatcher(actual =>
            {
                var actualData = actual.ReadAllBytes();
                var lengthsMatch = actualData.Length == data.Length;
                var passed = lengthsMatch &&
                             actualData.DeepEquals(data);
                return new MatcherResult(
                    passed,
                    () => lengthsMatch
                              ? "Stream data matches expected length, but not expected content"
                              : "Stream data does not match expected content at all"
                );
            });
        }
    }
}