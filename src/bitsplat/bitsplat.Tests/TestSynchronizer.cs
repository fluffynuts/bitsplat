using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestSynchronizer
    {
        [Test]
        public void ShouldImplement_ISynchronizer()
        {
            // Arrange
            Expect(typeof(Synchronizer))
                .To.Implement<ISynchronizer>();
            // Act
            // Assert
        }

        [TestFixture]
        public class Behavior
        {
            [TestFixture]
            public class WhenPresentedWithTwoEmptyFilesystems
            {
                [Test]
                public void ShouldListRecursiveOnBoth()
                {
                    // Arrange
                    var fs1 = Substitute.For<IFileSystem>();
                    var fs2 = Substitute.For<IFileSystem>();
                    var sut = Create();
                    // Act
                    sut.Synchronize(fs1, fs2);
                    // Assert
                    Expect(fs1).To.Have.Received(1)
                        .ListResourcesRecursive();
                    Expect(fs2).To.Have.Received(1)
                        .ListResourcesRecursive();
                }
            }
        }

        private static ISynchronizer Create()
        {
            return new Synchronizer();
        }
    }
}