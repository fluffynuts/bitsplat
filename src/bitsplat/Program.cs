using System;

namespace bitsplat
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var opts = Args.Configure()
                .WithParameter(
                    "source",
                    o => o.WithArg("-s")
                        .WithArg("--source")
                )
                .WithParameter(
                    "target",
                    o => o.WithArg("-t")
                        .WithArg("--target")
                )
                .Parse(args);
            // TODO: actually use args
        }
    }

    public class Options : ParsedArguments
    {
        public string Source => SingleParameter("-s", "--source");
        public string Target => SingleParameter("-t", "--target");
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