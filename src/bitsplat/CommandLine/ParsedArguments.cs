using System;
using System.Collections.Generic;
using System.Linq;
using PeanutButter.Utils;

namespace bitsplat.CommandLine
{
    public class ParsedArguments
    {
        public IDictionary<string, bool> Flags { get; } 
            = new Dictionary<string, bool>();
        public IDictionary<string, string[]> Parameters { get; } 
            = new Dictionary<string, string[]>();

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