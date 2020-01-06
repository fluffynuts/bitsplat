using System;
using bitsplat.Archivers;
using bitsplat.CommandLine;
using bitsplat.ResumeStrategies;
using DryIoc;
using static bitsplat.Filters.FilterRegistrations;

namespace bitsplat
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Console.CancelKeyPress += OnCancel;
            var opts = ParseOptionsFrom(args);
            using var container = CreateContainerScopeFor(opts);

            var source = container.ResolveSourceFileSystem();
            var target = container.ResolveTargetFileSystem();
            var archive = container.ResolveArchiveFileSystem();
            var archiver = container.Resolve<IArchiver>();

            archiver.RunArchiveOperations(
                target,
                archive);

            var synchronizer = container.Resolve<ISynchronizer>();

            synchronizer.Synchronize(
                $"Start sync: {source.BasePath} => {target.BasePath}",
                source,
                target);

            return 0;
        }

        private static bool _cancelled;

        private static void OnCancel(
            object sender,
            ConsoleCancelEventArgs e)
        {
            if (_cancelled)
            {
                return;
            }

            Console.WriteLine("\n(operation cancelled by SIGINT)");
            _cancelled = true;
        }

        private static IResolverContext CreateContainerScopeFor(Options opts)
        {
            return AppContainer.Create()
                .WithTarget(
                    opts.Target
                )
                .WithHistoryRepositoryFor(opts)
                .WithSource(
                    opts.Source
                )
                .WithResumeStrategy(
                    opts.Resume
                        ? typeof(AlwaysResumeStrategy)
                        : typeof(NeverResumeStrategy)
                )
                .WithFilter(
                    FilterMap[opts.SyncStrategy]
                )
                .WithArchive(opts.Archive)
                .WithProgressReporterFor(opts)
                .WithMessageWriterFor(opts)
                .OpenScope();
        }

        private static Options ParseOptionsFrom(string[] args)
        {
            var opts = Args.Configure()
                .WithParameter(
                    nameof(Options.Archive),
                    o => o.WithArg("-a")
                        .WithArg("--archive")
                )
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
                .WithParameter(
                    nameof(Options.HistoryDatabase),
                    o => o.WithArg("-h")
                        .WithArg("--history-db")
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
                    nameof(Options.SyncStrategy),
                    o => o.WithArg("-s")
                        .WithArg("--sync-strategy")
                )
                .Parse<Options>(args);
            return opts;
        }
    }
}