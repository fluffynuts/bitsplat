using System.Collections.Generic;

namespace bitsplat.CommandLine
{
    public abstract class ParserBase<T> where T : class
    {
        public string Name { get; }

        public string[] Args =>
            _argsArray ?? (_argsArray = _args.ToArray());

        private List<string> _args = new List<string>();
        private string[] _argsArray;

        public ParserBase(string name)
        {
            Name = name;
        }

        public T WithArg(string argument)
        {
            _args.Add(argument);
            _argsArray = null;
            return this as T;
        }
    }
}