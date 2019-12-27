using System.IO;
using NExpect;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    [TestFixture]
    [Explicit("Discovery")]
    public class DiscoveryTests
    {
        [TestFixture]
        public class MemoryStreams: DiscoveryTests
        {
            [Test]
            public void WriteMemStreamWithInitialData()
            {
                // Arrange
                var memStream = CreateMemoryStreamContaining(new byte[0]);
                var expected = GetRandomBytes(10);
                // Act
                memStream.Write(expected, 0, expected.Length);
                // Assert
                Expectations.Expect(memStream.ToArray())
                    .To.Equal(expected);
            }

            private static MemoryStream CreateMemoryStreamContaining(
                byte[] data)
            {
                var result = new MemoryStream();
                result.Write(data, 0, data.Length);
                return result;
            }
        }
    }
}