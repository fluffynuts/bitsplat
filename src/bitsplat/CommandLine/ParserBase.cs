using System;
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
        public string[] Help { get; set; }

        public string[] Switches =>
            _switchesArray ??= _switches.ToArray();

        private readonly List<string> _switches = new List<string>();
        private string[] _switchesArray;

        public ParserBase(string name)
        {
            Name = name;
        }

        public virtual TParser WithDefault(TValue value)
        {
            Default = value;
            return this as TParser;
        }

        public TParser WithArg(string argument)
        {
            _switches.Add(argument);
            _switchesArray = null;
            return this as TParser;
        }
    }
}