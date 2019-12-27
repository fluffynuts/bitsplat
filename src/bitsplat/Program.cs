using System;
using bitsplat.CommandLine;
using bitsplat.Filters;
using DryIoc;

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
                .Parse<Options>(args);
        }
    }

    public class Options : ParsedArguments
    {
        public string Source { get; set; }
        public string Target { get; set; }
        public string Archive { get; set; }

        public bool Resume { get; set; }
        public bool Quiet { get; set; }
        public bool NoHistory { get; set; }
    }

//    public class Options
//    {
//        [Option(
//            's', "source",
//            Required = true,
//            HelpText = "Source folder")]
//        public string Source { get; set; }
//
//        [Option(
//            't', "target",
//            Required = true,
//            HelpText = "Target folder")]
//        public string Target { get; set; }
//
//        [Option(
//            'a', "archive",
//            Required = false,
//            HelpText = "Archive folder")]
//        public string Archive { get; set; }
//
//        [Option(
//            'n', "no-history",
//            Required = false,
//            HelpText = "No history -- just sync existing")]
//        public bool NoHistory { get; set; }
//
//    }
}