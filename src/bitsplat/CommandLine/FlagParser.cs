using System.Collections.Generic;

namespace bitsplat.CommandLine
{
    public class FlagParser : ParserBase<FlagParser, bool>
    {
        public FlagParser(string name)
            : base(name)
        {
        }

        public FlagParser WithDefault(bool value)
        {
            Default = value;
            return this;
        }

        public bool Parse(IList<string> args)
        {
            return args.TryFindFlag(Args) ?? Default;
        }
    }
}