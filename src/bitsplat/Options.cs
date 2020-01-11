using bitsplat.CommandLine;

namespace bitsplat
{
    public enum SyncStrategy
    {
        Greedy,
        TargetOptIn
    }

    public class Options : ParsedArguments
    {
        public SyncStrategy SyncStrategy { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public string HistoryDatabase { get; set; }
        public string Archive { get; set; }

        public bool NoResume { get; set; }
        public bool Quiet { get; set; }
        public bool NoHistory { get; set; }
        public bool KeepStaleFiles { get; set; }
    }
}