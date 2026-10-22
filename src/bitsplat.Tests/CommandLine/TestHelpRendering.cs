using System;
using System.IO;
using bitsplat.CommandLine;
using static NExpect.Expectations;
using NExpect;
using NUnit.Framework;

namespace bitsplat.Tests.CommandLine
{
    [TestFixture]
    public class TestHelpRendering
    {
        private const string LONG_HELP = "Print a lot more logging about everything that happens";

        [Test]
        public void ShouldRenderHelpUnwrappedWhenConsoleWidthIsUnknown()
        {
            // Arrange
            var sut = CreateBuilder(consoleWidth: 0);
            // Act
            var output = RenderHelp(sut);
            // Assert
            Expect(output)
                .To.Contain("BitSplat")
                .And
                .To.Contain("  -v, --verbose")
                .And
                .To.Contain($"    {LONG_HELP}");
        }

        [Test]
        public void ShouldWrapHelpToTheConsoleWidth()
        {
            // Arrange
            var sut = CreateBuilder(consoleWidth: 26);
            // Act
            var output = RenderHelp(sut);
            // Assert
            // 26 wide, less 4 lead and 2 trailing padding
            Expect(output)
                .To.Contain($"    {LONG_HELP.Substring(0, 20)}{Environment.NewLine}")
                .And
                .To.Contain($"    {LONG_HELP.Substring(20, 20)}{Environment.NewLine}");
        }

        [Test]
        public void SplitLinesShouldRejectWidthsWhichCannotHoldAnything()
        {
            // Arrange
            // Act
            Expect(() => "abc".SplitLines(0))
                .To.Throw<ArgumentOutOfRangeException>();
            // Assert
        }

        private static ArgumentsBuilder CreateBuilder(int consoleWidth)
        {
            return new ArgumentsBuilder(() => consoleWidth)
                .WithFlag(
                    "Verbose",
                    o => o.WithArg("-v")
                        .WithArg("--verbose")
                        .WithHelp(LONG_HELP))
                .WithHelp("BitSplat");
        }

        private static string RenderHelp(ArgumentsBuilder builder)
        {
            using var output = new StringWriter();
            var originalOut = Console.Out;
            Console.SetOut(output);
            try
            {
                builder.Parse(new[] { "--help" });
                return output.ToString();
            }
            finally
            {
                Console.SetOut(originalOut);
            }
        }
    }
}
