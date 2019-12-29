using bitsplat.CommandLine;

namespace bitsplat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var opts = Args.Configure()
                .WithParameter(
                    nameof(Options.Source),
                    o => o.WithArg("-s")
                        .WithArg("--source")
                        .Required()
                )
                .WithParameter(
                    nameof(Options.Target),
                    o => o.WithArg("-t")
                        .WithArg("--target")
                )
                .WithFlag(
                    nameof(Options.Resume),
                    o => o.WithArg("-r")
                        .WithArg("--resume")
                )
                .WithFlag(
                    nameof(Options.Quiet),
                    o => o.WithArg("-q")
                        .WithArg("--quiet")
                )
                .WithFlag(
                    nameof(Options.NoHistory),
                    o => o.WithArg("-n")
                        .WithArg("--no-history")
                )
                .WithParameter(
                    nameof(Options.Archive),
                    o => o.WithArg("-a")
                        .WithArg("--archive")
                )
                .WithParameter(
                    nameof(Options.SyncStrategy),
                    o => o.WithArg("-s")
                        .WithArg("--sync-strategy")
                )
                .Parse<Options>(args);
        }
    }
}