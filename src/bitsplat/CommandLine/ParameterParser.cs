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
            var result = args.FindParameters(Args);
            if (result.Length == 0 && 
                Default?.Length > 0)
            {
                // create a copy
                result = Default.ToArray();
            }

            if (result.Length == 0 &&
                IsRequired)
            {
                throw new ArgumentException(
                    $"{Name} is required"
                );
            }

            return result;
        }

        public ParameterParser Required()
        {
            base.IsRequired = true;
            return this;
        }
    }
}