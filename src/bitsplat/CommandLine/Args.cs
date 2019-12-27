using System;
using System.Collections.Generic;
using System.Linq;
using PeanutButter.Utils;

namespace bitsplat.CommandLine
{
    public static class Args
    {
        public static bool FindFlag(
            this IList<string> args,
            params string[] switches)
        {
            return args.TryFindFlag(switches) ?? false;
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
        public ParameterParser(string name)
            : base(name)
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

        public FlagParser(string name)
            : base(name)
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

        public string[] Parameter(
            string[] fallback,
            params string[] names)
        {
            if (names.Length == 0)
            {
                throw new InvalidOperationException(
                    "Unable to determine parameter value when no parameter name(s) supplied"
                );
            }

            return names.Aggregate(
                       null as string[],
                       (acc, cur) => acc ??
                                     (Parameters.TryGetValue(cur, out var result)
                                          ? result
                                          : null)
                   ) ??
                   fallback;
        }

        public string[] Parameter(
            params string[] names)
        {
            return Parameter(null, names);
        }

        public string SingleParameter(
            params string[] names)
        {
            var result = Parameter(names);
            if (result.Length == 1)
            {
                return result[0];
            }

            if (result.Length == 0)
            {
                throw new ArgumentException(
                    names.Length == 1
                        ? $"{names.First()} is required"
                        : $"One of the following parameters is required: {names.JoinWith(", ")}"
                );
            }

            throw new ArgumentException(
                names.Length == 1
                    ? $"{names.First()} may only be specified once"
                    : $"Only one of the following parameters may be specified, and only once: {names.JoinWith(", ")}"
            );
        }

        public bool Flag(params string[] name)
        {
            return Flag(false, name);
        }

        public bool Flag(bool fallback, params string[] names)
        {
            return names.Aggregate(
                       null as bool?,
                       (acc, cur) => acc ??
                                     (Flags.TryGetValue(cur, out var result)
                                          ? result as bool?
                                          : null)
                   ) ??
                   fallback;
        }
    }
}