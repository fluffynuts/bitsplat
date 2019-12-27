using System;
using System.Collections.Generic;
using System.Linq;

namespace bitsplat
{
    public static class Args
    {
        public static bool FindFlag(
            this IList<string> args,
            params string[] switches)
        {
            return args.TryFindFlag(switches)
                ?? false;
        }

        public static bool? TryFindFlag(
            this IList<string> args,
            params string[] switches)
        {
            return switches.Aggregate(
                null as bool?,
                (acc, cur) =>
                {
                    int idx;
                    while ((idx = args.IndexOf(cur)) > -1)
                    {
                        args.RemoveAt(idx);
                        acc = true;
                    }

                    return acc;
                });
        }

        public static string[] FindParameters(
            this IList<string> args,
            params string[] switches)
        {
            var result = new List<string>();
            var toRemove = new List<int>();
            var inSwitch = false;
            args.ForEach((arg, idx) =>
            {
                if (switches.Contains(arg))
                {
                    inSwitch = true;
                    toRemove.Add(idx);
                    return;
                }

                if (!inSwitch)
                {
                    return;
                }

                inSwitch = false;
                result.Add(arg);
                toRemove.Add(idx);
            });

            toRemove.ForEach((removeIndex, idx) =>
            {
                args.RemoveAt(removeIndex - idx);
            });

            return result.ToArray();
        }

        public static ArgumentsBuilder Configure()
        {
            return new ArgumentsBuilder();
        }
    }

    public class ArgumentsBuilder
    {
        private Dictionary<string, Action<FlagParser>> _flags
            = new Dictionary<string, Action<FlagParser>>();

        private Dictionary<string, Action<ParameterParser>>
            _parameters = new Dictionary<string, Action<ParameterParser>>();

        public ArgumentsBuilder WithFlag(
            string name,
            Action<FlagParser> configuration)
        {
            _flags[name] = configuration;
            return this;
        }

        public ArgumentsBuilder WithParameter(
            string name,
            Action<ParameterParser> configuration)
        {
            _parameters[name] = configuration;
            return this;
        }

        public ParsedArguments Parse(string[] args)
        {
            var result = new ParsedArguments();
            var argsList = args.ToList();
            _flags.ForEach(kvp => ParseFlag(result, argsList, kvp.Key, kvp.Value));
            _parameters.ForEach(kvp => ParseParameter(result, argsList, kvp.Key, kvp.Value));
            return result;
        }

        private void ParseParameter(
            ParsedArguments result,
            List<string> args,
            string name,
            Action<ParameterParser> configure)
        {
            var parser =  new ParameterParser(name);
            configure(parser);
            result.Parameters[name] = parser.Parse(args);
        }

        private void ParseFlag(
            ParsedArguments result,
            List<string> args,
            string name,
            Action<FlagParser> configure)
        {
            var parser = new FlagParser(name);
            configure(parser);
            result.Flags[name] = parser.Parse(args);
        }
    }

    public abstract class ParserBase<T> where T : class
    {
        private readonly string _name;

        public string[] Args =>
            _argsArray ?? (_argsArray = _args.ToArray());

        private List<string> _args = new List<string>();
        private string[] _argsArray;

        public ParserBase(string name)
        {
            _name = name;
        }

        public T WithArg(string argument)
        {
            _args.Add(argument);
            _argsArray = null;
            return this as T;
        }
    }

    public class ParameterParser : ParserBase<ParameterParser>
    {
        public ParameterParser(string name) : base(name)
        {
        }

        public string[] Parse(IList<string> args)
        {
            return args.FindParameters(Args);
        }
    }

    public class FlagParser : ParserBase<FlagParser>
    {
        private bool _default;

        public FlagParser(string name) : base(name)
        {
        }

        public FlagParser WithDefault(bool value)
        {
            _default = value;
            return this;
        }

        public bool Parse(IList<string> args)
        {
            return args.TryFindFlag(Args) ?? _default;
        }
    }

    public class ParsedArguments
    {
        public IDictionary<string, bool> Flags { get; } = new Dictionary<string, bool>();
        public IDictionary<string, string[]> Parameters { get; } = new Dictionary<string, string[]>();
    }
}