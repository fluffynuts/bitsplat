using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public void ShouldHaveSourceOption(string opt)
        {
            // Arrange
            var expected = GetRandomString(1);
            var args = ArgsBuilder.Create()
                .WithOption(opt, expected)
                .WithMissingRequiredArgs()
                .Build();
            // Act
            Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o => Expect(o.Source).To.Equal(expected))
                .WithNotParsed(
                    msg => Assert.Fail($"parse failure:\n{msg.JoinWith("\n")}"));
            // Assert
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

        private static readonly (string shortName, string longName, bool isFlag)[] RequiredOptions
            = OptionProperties
                .Select(pi =>
                {
                    var attrib = pi.GetCustomAttributes().OfType<OptionAttribute>().FirstOrDefault();
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
                        var missing = o.None(a => a == $"-{cur.shortName}" ||
                            a == $"--{cur.longName}");
                        return missing
                            ? (cur.isFlag
                                ? o.And(cur.shortName)
                                : o.And(cur.longName).And(GetRandomString(1)))
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