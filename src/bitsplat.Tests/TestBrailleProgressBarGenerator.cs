using bitsplat.Pipes;
using NExpect;
using NUnit.Framework;
using static NExpect.Expectations;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestBrailleProgressBarGenerator
    {
        [TestCase(0, 0, "     ")]
        [TestCase(8, 0, "⠁    ")]
        [TestCase(11, 0, "⠃    ")]
        [TestCase(15, 0, "⠋    ")]
        [TestCase(20, 0, "⠛    ")]
        [TestCase(25, 0, "⠛⠁   ")]
        public void ShouldConvertPercent(
            int itemPercentage,
            int totalPercentage,
            string expected)
        {
            // Arrange
            // Act
            var result = BrailleProgressBarGenerator.CompositeProgressBarFor(
                itemPercentage,
                totalPercentage);
            // Assert
            Expect(result)
                .To.Equal(expected);
        }
    }
}