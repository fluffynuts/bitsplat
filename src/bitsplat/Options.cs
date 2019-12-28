using bitsplat.CommandLine;

namespace bitsplat
{
    public class Options : ParsedArguments
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public string Archive { get; set; }

        public bool Resume { get; set; }
        public bool Quiet { get; set; }
        public bool NoHistory { get; set; }
    }
}