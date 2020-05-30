using bitsplat.CommandLine;

namespace bitsplat
{
    public enum SyncMode
    {
        All,
        OptIn
    }

    public interface IOptions
    {
        SyncMode SyncMode { get; set; }
        string Source { get; set; }
        string Target { get; set; }
        string HistoryDatabase { get; set; }
        string Archive { get; set; }
        bool NoResume { get; set; }
        bool Quiet { get; set; }
        bool NoHistory { get; set; }
        bool KeepStaleFiles { get; set; }
        int Retries { get; set; }
        int ResumeCheckBytes { get; set; }
        bool CreateTargetIfRequired { get; set; }
        bool DryRun { get; set; }
        bool Verbose { get; set; }
        public bool Version { get; set; }
    }

    public class Options : ParsedArguments, IOptions
    {
        public int Retries { get; set; }
        public int ResumeCheckBytes { get; set; }
        public SyncMode SyncMode { get; set; }
        public string Source { get; set; }
        public string Target { get; set; }
        public string HistoryDatabase { get; set; }
        public string Archive { get; set; }

        public bool NoResume { get; set; }
        public bool Quiet { get; set; }
        public bool NoHistory { get; set; }
        public bool KeepStaleFiles { get; set; }
        public bool CreateTargetIfRequired { get; set; }

        public bool DryRun { get; set; }
        public bool Verbose { get; set; }
        public bool Version { get; set; }
    }
}