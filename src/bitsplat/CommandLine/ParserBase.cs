using System.Collections.Generic;

namespace bitsplat.CommandLine
{
    public abstract class ParserBase<
        TParser,
        TValue> where TParser : class
    {
        public string Name { get; }
        public TValue Default { get; set; }
        public bool IsRequired { get; set; }

        public string[] Args =>
            _argsArray ??= _args.ToArray();

        private List<string> _args = new List<string>();
        private string[] _argsArray;

        public ParserBase(string name)
        {
            Name = name;
        }

        public TParser WithArg(string argument)
        {
            _args.Add(argument);
            _argsArray = null;
            return this as TParser;
        }
    }
}