using System;
using System.Collections.Generic;
using System.Linq;
using bitsplat.CommandLine;
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
                    Expect(result)
                        .To.Be.True();
                }

                [Test]
                public void ShouldReturnTrueWhenHaveMatchForMultipleSwitches()
                {
                    // Arrange
                    var args = List("-f");
                    // Act
                    var result = args.FindFlag("--force", "-f");
                    // Assert
                    Expect(result)
                        .To.Be.True();
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
                    Expect(result)
                        .To.Be.Empty();
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
                                    .WithFlag("force",
                                        o => o.WithArg("-f")
                                            .WithArg("--force")
                                    )
                                    .Parse(args);
                                // Assert
                                Expect(result)
                                    .Not.To.Be.Null();
                                Expect(result.Parameters)
                                    .Not.To.Be.Null();
                                Expect(result.Flags)
                                    .To.Contain.Key("force")
                                    .With.Value.Intersection.Equal.To(
                                        new { Value = false }
                                    );
                            }

                            [TestCase(true)]
                            [TestCase(false)]
                            public void ShouldHaveSingleFlagWithDefault(bool defaultValue)
                            {
                                // Arrange
                                var args = new string[0];
                                // Act
                                var result = Args.Configure()
                                    .WithFlag("force",
                                        o =>
                                            o.WithArg("-f")
                                                .WithArg("--force")
                                                .WithDefault(defaultValue)
                                    )
                                    .Parse(args);
                                // Assert
                                Expect(result)
                                    .Not.To.Be.Null();
                                Expect(result.Flags)
                                    .Not.To.Be.Null();
                                Expect(result.Flags)
                                    .To.Contain.Key("force")
                                    .With.Value.Intersection.Equal.To(
                                        new { Value = defaultValue }
                                    );
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
                                Expect(result)
                                    .Not.To.Be.Null();
                                Expect(result.Parameters)
                                    .Not.To.Be.Null();
                                Expect(result.Parameters)
                                    .To.Contain.Key("source")
                                    .With.Value.Intersection.Equal.To(
                                        new { Value = new string[0] }
                                    );
                            }
                        }

                        [TestFixture]
                        public class ParsingOntoCustomType
                        {
                            [Test]
                            public void ShouldStringMapParameterByName()
                            {
                                // Arrange
                                var args = new[] { "-s", GetRandomString() };
                                var expected = args[1];
                                // Act
                                var result = Args.Configure()
                                    .WithParameter(
                                        nameof(Opts.Source),
                                        o => o.WithArg("-s")
                                    )
                                    .Parse<Opts>(args);
                                // Assert
                                Expect(result.Source)
                                    .To.Equal(expected);
                            }

                            [Test]
                            public void ShouldEnforceRequiredParameters()
                            {
                                // Arrange
                                var args = new string[0];
                                // Act
                                Expect(() =>
                                        Args.Configure()
                                            .WithParameter(
                                                nameof(Opts.Source),
                                                o => o.WithArg("-s")
                                                    .Required()
                                            )
                                            .Parse<Opts>(args)
                                    )
                                    .To.Throw<ArgumentException>()
                                    .With.Message.Containing(
                                        "Source is required"
                                    );
                                // Assert
                            }

                            [Test]
                            public void ShouldMapMultiValueParametersToArray()
                            {
                                // Arrange
                                var args = new[] { "-m", "one", "-m", "two", "-m", "three" };
                                var expected = new[] { "one", "two", "three" };
                                // Act
                                var result = Args.Configure()
                                    .WithParameter(
                                        nameof(Opts.MultiValue),
                                        o => o.WithArg("-m")
                                            .WithArg("--multi")
                                    )
                                    .Parse<Opts>(args);
                                // Assert
                                Expect(result.MultiValue)
                                    .To.Equal(expected);
                            }

                            [Test]
                            public void ShouldMapMultiValueParametersToEnumerable()
                            {
                                // Arrange
                                var args = new[] { "-m", "one", "-m", "two", "-m", "three" };
                                var expected = new[] { "one", "two", "three" };
                                // Act
                                var result = Args.Configure()
                                    .WithParameter(
                                        nameof(Opts.MultiValueEnumerable),
                                        o => o.WithArg("-m")
                                            .WithArg("--multi")
                                    )
                                    .Parse<Opts>(args);
                                // Assert
                                Expect(result.MultiValueEnumerable)
                                    .To.Equal(expected);
                            }

                            [Test]
                            public void ShouldMapMultiValueParametersToList()
                            {
                                // Arrange
                                var args = new[] { "-m", "one", "-m", "two", "-m", "three" };
                                var expected = new[] { "one", "two", "three" };
                                // Act
                                var result = Args.Configure()
                                    .WithParameter(
                                        nameof(Opts.MultiValueList),
                                        o => o.WithArg("-m")
                                            .WithArg("--multi")
                                    )
                                    .Parse<Opts>(args);
                                // Assert
                                Expect(result.MultiValueList)
                                    .To.Equal(expected);
                            }

                            [Test]
                            public void ShouldMapSingleEnum()
                            {
                                // Arrange
                                var expected = GetRandom<EnumValue>(e => e != EnumValue.One);
                                var args = new[] { GetRandomFrom(new[] { "-e", "--enum-value" }), expected.ToString() };
                                // Act
                                var result = Args.Configure()
                                    .WithParameter(
                                        nameof(Opts.EnumValue),
                                        o => o.WithArg("-e")
                                            .WithArg("--enum-value")
                                            .Required()
                                    )
                                    .Parse<Opts>(args);
                                // Assert
                                Expect(result.EnumValue)
                                    .To.Equal(expected);
                            }

                            [Test]
                            public void ShouldMapConvertable()
                            {
                                // Arrange
                                var expected = GetRandomCollection<int>(2);
                                var args = expected.Select(i => new[] { "-i", i.ToString() })
                                    .SelectMany(o => o)
                                    .ToArray();
                                // Act
                                var result = Args.Configure()
                                    .WithParameter(
                                        nameof(Opts.Ints),
                                        o => o.WithArg("-i")
                                    )
                                    .Parse<Opts>(args);
                                // Assert
                                Expect(result.Ints)
                                    .To.Equal(expected);
                            }

                            public enum EnumValue
                            {
                                One,
                                Two,
                                Three,
                                Four
                            }

                            public class Opts : ParsedArguments
                            {
                                public string Source { get; set; }
                                public string[] MultiValue { get; set; }
                                public IEnumerable<string> MultiValueEnumerable { get; set; }
                                public IList<string> MultiValueList { get; set; }
                                public EnumValue EnumValue { get; set; }
                                public int[] Ints { get; set; }
                            }
                        }
                    }
                }
            }
        }
    }
}