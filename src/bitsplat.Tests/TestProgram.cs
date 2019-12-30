using System;
using System.IO;
using System.Linq;
using bitsplat.Tests.TestingSupport;
using static NExpect.Expectations;
using NExpect;
using NUnit.Framework;
using PeanutButter.Utils;
using static PeanutButter.RandomGenerators.RandomValueGen;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestProgram
    {
        [Test]
        [Explicit("WIP")]
        public void ShouldSyncOneFileInRoot()
        {
            // Arrange
            using (var arena = CreateArena())
            {
                var newFile = arena.CreateSourceFile();
                Expect(newFile.Path)
                    .To.Exist();
                var args = new[]
                {
                    "-s",
                    arena.SourcePath,
                    "-t",
                    arena.TargetPath
                };
                var expected = arena.TargetPathFor(newFile.RelativePath);
                // Act
                Program.Main(args);
                // Assert
                Expect(expected)
                    .To.Exist();
                var data = File.ReadAllBytes(expected);
                Expect(data)
                    .To.Equal(newFile.Data);
            }
        }

        private static TestArena CreateArena()
        {
            return new TestArena();
        }
    }
}