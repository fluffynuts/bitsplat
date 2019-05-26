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
            var data = GetRandomBytes(100);
            var source = CreateMemoryStreamContaining(data);
            var target = new MemoryStream();
            var sut = Create(source);
            // Act
            sut.Pipe(target)
                .Drain();
            // Assert
            Expect(target)
                .To.Contain.Only(data);
        }

        [Test]
        public void ShouldDrainThroughIntermediateStream()
        {
            // Arrange
            var data = GetRandomBytes(100);
            var source = CreateMemoryStreamContaining(data);
            source.SetMetadata("streamId", "source");
            var intermediate = new MemoryStream();
            intermediate.SetMetadata("streamId", "intermediate");
            var target = new MemoryStream();
            target.SetMetadata("streamId", "target");
            // Act
            source
                .Pipe(intermediate)
                .Pipe(target)
                .Drain();
            // Assert
            Expect(intermediate).To.Contain.Only(data);
            Expect(target).To.Contain.Only(data);
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

    public class PassThroughStream : Stream
    {
        public override void Flush()
        {
            throw new System.NotImplementedException();
        }

        public override int Read(
            byte[] buffer,
            int offset,
            int count)
        {
            throw new System.NotImplementedException();
        }

        public override long Seek(
            long offset,
            SeekOrigin origin)
        {
            throw new System.NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotImplementedException();
        }

        public override void Write(
            byte[] buffer,
            int offset,
            int count)
        {
            throw new System.NotImplementedException();
        }

        public override bool CanRead { get; }
        public override bool CanSeek { get; }
        public override bool CanWrite { get; }
        public override long Length { get; }
        public override long Position { get; set; }
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