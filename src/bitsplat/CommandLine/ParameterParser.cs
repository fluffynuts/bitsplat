using System;
using System.Collections.Generic;
using System.Linq;

namespace bitsplat.CommandLine
{
    public class ParameterParser : ParserBase<
        ParameterParser,
        string[]>
    {
        public ParameterParser(string name)
            : base(name)
        {
        }

        public string[] Parse(IList<string> args)
        {
            var result = args.FindParameters(Switches);
            if (result.Length == 0 && 
                Default?.Length > 0)
            {
                // create a copy
                result = Default.ToArray();
            }

            return result;
        }

        public ParameterParser Required()
        {
            IsRequired = true;
            return this;
        }

        public ParameterParser WithHelp(params string[] help)
        {
            Help = help;
            return this;
        }
    }
}