using System.IO;
using System.Text;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using static NExpect.Expectations;
using NExpect;
using NExpect.Interfaces;
using NExpect.MatcherLogic;
using PeanutButter.Utils;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestStreamPipe
    {
        [Test]
        public void ShouldDrainOneStreamIntoAnother()
        {
            // Arrange
            var data = Encoding.UTF8.GetBytes("moo-cow");
            var source = CreateMemoryStreamContaining(data);
            var target = new MemoryStream();
            var sut = Create(source);
            // Act
            sut.Pipe(target)
                .Drain();
            // Assert
            var targetData = target.ReadAllBytes();
            Expect(targetData).To.Deep.Equal(data);
            // FIXME: update to use custom matcher once PB update
//            Expect(target)
//                .To.Contain.Only(data);
        }

        private static IPipeline Create(
            Stream source)
        {
            return new Pipeline(source);
        }

        private static MemoryStream CreateMemoryStreamContaining(
            byte[] data)
        {
            var result = new MemoryStream();
            result.Write(data, 0, data.Length);
            result.Rewind();
            return result;
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