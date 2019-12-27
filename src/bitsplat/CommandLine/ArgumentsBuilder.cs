using System;
using System.Collections.Generic;
using System.Linq;

namespace bitsplat.CommandLine
{
    public class ArgumentsBuilder
    {
        private readonly Dictionary<string, Action<FlagParser>> _flags
            = new Dictionary<string, Action<FlagParser>>();

        private readonly Dictionary<string, Action<ParameterParser>>
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

        public ParsedArguments Parse(
            string[] args
        )
        {
            return Parse<ParsedArguments>(args);
        }

        public T Parse<T>(
            string[] args
        ) where T : ParsedArguments, new()
        {
            var result = new T();
            var argsList = args.ToList();
            _flags.ForEach(kvp => ParseFlag(result, argsList, kvp.Key, kvp.Value));
            _parameters.ForEach(kvp => ParseParameter(result, argsList, kvp.Key, kvp.Value));
            Decorate(result);
            return result;
        }

        private void Decorate<T>(T result) 
            where T : ParsedArguments
        {
        }

        private void ParseParameter(
            ParsedArguments result,
            IList<string> args,
            string name,
            Action<ParameterParser> configure)
        {
            var parser = new ParameterParser(name);
            configure(parser);
            result.Parameters[name] = parser.Parse(args);
        }

        private void ParseFlag(
            ParsedArguments result,
            IList<string> args,
            string name,
            Action<FlagParser> configure)
        {
            var parser = new FlagParser(name);
            configure(parser);
            result.Flags[name] = parser.Parse(args);
        }
    }
}