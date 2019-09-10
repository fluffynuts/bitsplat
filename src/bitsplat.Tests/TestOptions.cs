using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using bitsplat.Tests.History;
using CommandLine;
using NUnit.Framework;
using static PeanutButter.RandomGenerators.RandomValueGen;
using NExpect;
using PeanutButter.Utils;
using static NExpect.Expectations;

namespace bitsplat.Tests
{
    [TestFixture]
    public class TestOptions
    {
        [TestCase("-s")]
        [TestCase("--source")]
        public void ShouldHaveRequiredSourceOption(string opt)
        {
            // Arrange
            var expected = GetRandomString(1);
            var args = ArgsBuilder.Create()
                .WithOption(opt, expected)
                .WithMissingRequiredArgs()
                .Build();
            // Act
            Expect(typeof(Options))
                .To.Have.Property(nameof(Options.Source))
                .Required();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => Expect(o.Source).To.Equal(expected))
                .ThrowOnParseError();
            // Assert
        }

        [TestCase("-t")]
        [TestCase("--target")]
        public void ShouldHaveRequiredTargetOption(string opt)
        {
            // Arrange
            var expected = GetRandomString(1);
            var args = ArgsBuilder.Create()
                .WithOption(opt, expected)
                .WithMissingRequiredArgs()
                .Build();
            // Act
            Expect(typeof(Options))
                .To.Have.Property(nameof(Options.Target))
                .Required();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => Expect(o.Target).To.Equal(expected))
                .ThrowOnParseError();
            // Assert
        }

        [TestCase("-a")]
        [TestCase("--archive")]
        public void ShouldHaveArchiveFlagOption(string opt)
        {
            // Arrange
            var expected = GetRandomString(1);
            var args = ArgsBuilder.Create()
                .WithOption(opt, expected)
                .WithMissingRequiredArgs()
                .Build();
            // Act
            Expect(typeof(Options))
                .To.Have.Property(nameof(Options.Archive))
                .Optional();
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => Expect(o.Archive).To.Equal(expected))
                .ThrowOnParseError();
            // Assert
        }
    }

    public static class ParserResultExtensions
    {
        public static void ThrowOnParseError(
            this ParserResult<Options> result
        )
        {
            result.WithNotParsed(msg =>
                throw new InvalidOperationException(
                    $"arguments parse failure:\n{msg.JoinWith("\n")}"
                )
            );
        }
    }

    public static class OptionsPropertyInfoMatchers
    {
        public static void Required(this WithType t)
        {
            var attrib = t.PropertyInfo.GetCustomAttributes()
                .OfType<OptionAttribute>()
                .FirstOrDefault();
            Expect(attrib).Not.To.Be.Null(
                $"No [Option] attribute on property {t.PropertyInfo.Name}"
            );
            Expect(attrib.Required).To.Be.True(
                $"-{attrib.ShortName}|--{attrib.LongName} should be required"
            );
        }
        public static void Optional(this WithType t)
        {
            var attrib = t.PropertyInfo.GetCustomAttributes()
                .OfType<OptionAttribute>()
                .FirstOrDefault();
            Expect(attrib).Not.To.Be.Null(
                $"No [Option] attribute on property {t.PropertyInfo.Name}"
            );
            Expect(attrib.Required).To.Be.False(
                $"-{attrib.ShortName}|--{attrib.LongName} should be optional"
            );
        }
    }

    public class ArgsBuilder
    {
        public static ArgsBuilder Create()
        {
            return new ArgsBuilder();
        }

        public string[] Build()
        {
            return _appenders.Aggregate(
                new string[0],
                (acc, cur) => cur(acc)
            );
        }

        private static readonly PropertyInfo[] OptionProperties
            = typeof(Options).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        private static readonly OptionAttribute[] OptionAttributes
            = OptionProperties
                .Select(pi => pi.GetCustomAttributes().OfType<OptionAttribute>())
                .Flatten()
                .ToArray();

        private static readonly (string shortName, string longName, bool isFlag)[]
            RequiredOptions = OptionProperties
                .Select(pi =>
                {
                    var attrib = pi.GetCustomAttributes()
                        .OfType<OptionAttribute>()
                        .FirstOrDefault();
                    return
                    (
                        shortName: attrib.ShortName,
                        longName: attrib.LongName,
                        isFlag: pi.PropertyType == typeof(bool)
                    );
                })
                .ToArray();

        public ArgsBuilder WithMissingRequiredArgs()
        {
            return RequiredOptions.Aggregate(
                this, (acc, cur) =>
                    acc.AppendArg(o =>
                    {
                        var shortArg = $"-{cur.shortName}";
                        var longArg = $"--{cur.longName}";
                        var missing = o.None(
                            a => a == shortArg ||
                                a == longArg);
                        if (!missing)
                        {
                            return o;
                        }

                        var toAppend = GetRandomBoolean()
                            ? shortArg
                            : longArg;

                        return missing
                            ? (cur.isFlag
                                ? o.And(toAppend)
                                : o.And(toAppend).And(GetRandomString(1)))
                            : o;
                    })
            );
        }

        public ArgsBuilder WithFlag(string flag)
        {
            return AppendArg(args => args.And(flag));
        }

        public ArgsBuilder WithOption(string option, string value)
        {
            return AppendArg(args => args.And(option).And(value));
        }

        private readonly List<Func<string[], string[]>> _appenders
            = new List<Func<string[], string[]>>();

        private ArgsBuilder AppendArg(Func<string[], string[]> appender)
        {
            _appenders.Add(appender);
            return this;
        }
    }
}