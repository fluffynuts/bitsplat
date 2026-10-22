using static NExpect.Expectations;
using NExpect;
using NUnit.Framework;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestSyncAttempt
    {
        [Test]
        public void ShouldDescribeABoundedAttempt()
        {
            // Arrange
            var sut = new SyncAttempt(2, 4);
            // Act
            var result = sut.ToString();
            // Assert
            Expect(result)
                .To.Equal("attempt 2 of 4");
        }

        [Test]
        public void ShouldDescribeAnUnboundedAttempt()
        {
            // Arrange
            var sut = new SyncAttempt(14, null);
            // Act
            var result = sut.ToString();
            // Assert
            Expect(result)
                .To.Equal("attempt 14 (retrying forever)");
        }
    }
}
