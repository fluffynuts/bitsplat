using System.Collections.Generic;

namespace bitsplat.CommandLine
{
    public class ParsedArgument<T>
    {
        public T Value { get; set; }
        public bool Optional { get; set; }
    }

    public class ParsedArguments
    {
        public string[] RawArguments { get; set; }

        public IDictionary<string, ParsedArgument<bool>> Flags { get; } 
            = new Dictionary<string, ParsedArgument<bool>>();
        public IDictionary<string, ParsedArgument<string[]>> Parameters { get; } 
            = new Dictionary<string, ParsedArgument<string[]>>();
    }
}