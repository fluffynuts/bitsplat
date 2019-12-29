using System;
using System.Collections.Generic;

namespace bitsplat.CommandLine
{
    public class ParameterParser : ParserBase<ParameterParser>
    {
        public bool Optional { get; set; } = true;

        public ParameterParser(string name)
            : base(name)
        {
        }

        public string[] Parse(IList<string> args)
        {
            var result = args.FindParameters(Args);
            if (result.Length == 0 &&
                !Optional)
            {
                throw new ArgumentException(
                    $"{Name} is required"
                );
            }

            return result;
        }

        public ParameterParser Required()
        {
            Optional = false;
            return this;
        }
    }
}