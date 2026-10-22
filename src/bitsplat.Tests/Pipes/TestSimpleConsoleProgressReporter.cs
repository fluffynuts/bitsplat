using System.IO;
using bitsplat.Pipes;
using static NExpect.Expectations;
using NExpect;
using NSubstitute;
using NUnit.Framework;

namespace bitsplat.Tests.Pipes
{
    [TestFixture]
    public class TestSimpleConsoleProgressReporter
    {
        [Test]
        public void ShouldIncludeTheAttemptInErrorOutput()
        {
            // Arrange
            var messageWriter = Substitute.For<IMessageWriter>();
            var sut = new SimpleConsoleProgressReporter(
                messageWriter,
                Substitute.For<IOptions>());
            // Act
            sut.NotifyError(
                new NotificationDetails()
                {
                    Label = "file.bin",
                    Exception = new IOException("network went away"),
                    Attempt = new SyncAttempt(2, 3)
                });
            // Assert
            Expect(messageWriter)
                .To.Have.Received(1)
                .Write(Arg.Is<string>(s => s.Contains("attempt 2 of 3 failed: network went away")));
        }

        [Test]
        public void ShouldReportErrorsOutsideOfAnAttemptWithoutAttemptInfo()
        {
            // Arrange
            var messageWriter = Substitute.For<IMessageWriter>();
            var sut = new SimpleConsoleProgressReporter(
                messageWriter,
                Substitute.For<IOptions>());
            // Act
            sut.NotifyError(
                new NotificationDetails()
                {
                    Label = "file.bin",
                    Exception = new FileNotFoundException("source: file.bin")
                });
            // Assert
            Expect(messageWriter)
                .To.Have.Received(1)
                .Write(Arg.Is<string>(s => s.Contains("source: file.bin")));
            Expect(messageWriter)
                .Not.To.Have.Received()
                .Write(Arg.Is<string>(s => s.Contains("attempt")));
        }
    }
}
