using System.Collections.Generic;

namespace bitsplat.CommandLine
{
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
}