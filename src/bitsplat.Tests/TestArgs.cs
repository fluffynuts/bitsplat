using System.Collections.Generic;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using NExpect;
using PeanutButter.Utils;
using static NExpect.Expectations;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestArgs
    {
        private static IList<T> List<T>(
            params T[] values)
        {
            return new List<T>(values);
        }

        [TestFixture]
        public class LowLevelParsing
        {
            [TestFixture]
            public class FindFlag
            {
                [Test]
                public void ShouldReturnFalseWhenNoMatch()
                {
                    // Arrange
                    var args = List("-a", "-b");
                    // Act
                    var result = args.FindFlag("-c", "-d");
                    // Assert
                    Expect(result)
                        .To.Be.False();
                }

                [Test]
                public void ShouldReturnTrueWhenHaveMatchForOneSwitch()
                {
                    // Arrange
                    var args = List("-f");
                    // Act
                    var result = args.FindFlag("-f");
                    // Assert
                    Expect(result).To.Be.True();
                }

                [Test]
                public void ShouldReturnTrueWhenHaveMatchForMultipleSwitches()
                {
                    // Arrange
                    var args = List("-f");
                    // Act
                    var result = args.FindFlag("--force", "-f");
                    // Assert
                    Expect(result).To.Be.True();
                }

                [Test]
                public void ShouldRemoveTheSingleSwitch()
                {
                    // Arrange
                    var args = List("foo", "bar", "-f", "--quux");
                    // Act
                    args.FindFlag("--force", "-f");
                    // Assert
                    Expect(args)
                        .To.Equal(List("foo", "bar", "--quux"));
                }

                [Test]
                public void ShouldRemoveAllMatchedSwitches()
                {
                    // Arrange
                    var args = List("foo", "--force", "bar", "-f", "--qux");
                    // Act
                    args.FindFlag("--force", "-f");
                    // Assert
                    Expect(args)
                        .To.Equal(List("foo", "bar", "--qux"));
                }
            }

            [TestFixture]
            public class FindArgs
            {
                [Test]
                public void ShouldReturnEmptyWhenNoSwitchMatches()
                {
                    // Arrange
                    var args = List("-a", "some-file.txt");
                    // Act
                    var result = args.FindParameters("-f");
                    // Assert
                    Expect(result).To.Be.Empty();
                }

                [Test]
                public void ShouldEjectFirstMatch()
                {
                    // Arrange
                    var args = List("-a", "some-file.txt", "another-arg");
                    // Act
                    var result = args.FindParameters("-a");
                    // Assert
                    Expect(result)
                        .To.Equal(List("some-file.txt"));
                    Expect(args)
                        .To.Equal(List("another-arg"));
                }

                [Test]
                public void ShouldEjectEachSubsequentMatch()
                {
                    // Arrange
                    var args = List("-a", "some-file.txt", "--archive", "another-file.txt", "-b");
                    // Act
                    var result = args.FindParameters("-a", "--archive");
                    // Assert
                    Expect(result)
                        .To.Equal(List("some-file.txt", "another-file.txt"));
                    Expect(args)
                        .To.Equal(List("-b"));
                }
            }

            [TestFixture]
            public class BuildingParser
            {
                [TestFixture]
                public class ParsedArgumentsBuilding
                {
                    [TestFixture]
                    public class WhenNoArgs
                    {
                        [TestFixture]
                        public class Flags
                        {
                            [Test]
                            public void ShouldHaveSingleFlagWithFalseValueWhenNoDefault()
                            {
                                // Arrange
                                var args = new string[0];
                                // Act
                                var result = Args.Configure()
                                    .WithFlag("force", o => o.WithArg("-f")
                                        .WithArg("--force")
                                    ).Parse(args);
                                // Assert
                                Expect(result).Not.To.Be.Null();
                                Expect(result.Parameters).Not.To.Be.Null();
                                Expect(result.Flags)
                                    .To.Contain.Key("force")
                                    .With.Value(false);
                            }

                            [TestCase(true)]
                            [TestCase(false)]
                            public void ShouldHaveSingleFlagWithDefault(bool defaultValue)
                            {
                                // Arrange
                                var args = new string[0];
                                // Act
                                var result = Args.Configure()
                                    .WithFlag("force", o =>
                                        o.WithArg("-f")
                                            .WithArg("--force")
                                            .WithDefault(defaultValue)
                                    ).Parse(args);
                                // Assert
                                Expect(result).Not.To.Be.Null();
                                Expect(result.Flags).Not.To.Be.Null();
                                Expect(result.Flags)
                                    .To.Contain.Key("force")
                                    .With.Value(defaultValue);
                            }
                        }

                        [TestFixture]
                        public class Parameters
                        {
                            [Test]
                            public void ShouldHaveSingleParameterWithEmptyValues()
                            {
                                // Arrange
                                var args = new string[0];
                                // Act
                                var result = Args.Configure()
                                    .WithParameter("source",
                                        o => o.WithArg("-s")
                                            .WithArg("--source")
                                    )
                                    .Parse(args);
                                // Assert
                                Expect(result).Not.To.Be.Null();
                                Expect(result.Parameters).Not.To.Be.Null();
                                Expect(result.Parameters)
                                    .To.Contain.Key("source")
                                    .With.Value(new string[0]);
                            }
                        }
                    }
                }
            }
        }
    }
}