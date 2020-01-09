﻿using System;
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
            if (!TryParseOptionsFrom(args, out var opts))
            {
                return -1;
            }

            using var container = CreateContainerScopeFor(opts);

            var source = container.ResolveSourceFileSystem();
            var target = container.ResolveTargetFileSystem();
            var archive = container.ResolveArchiveFileSystem();
            var archiver = container.Resolve<IArchiver>();

            archiver.RunArchiveOperations(
                target,
                archive,
                source);

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
                    opts.NoResume
                        ? typeof(NeverResumeStrategy)
                        : typeof(AlwaysResumeWhenTargetSmallerStrategy)
                )
                .WithFilter(
                    FilterMap[opts.SyncStrategy]
                )
                .WithArchive(opts.Archive)
                .WithProgressReporterFor(opts)
                .WithMessageWriterFor(opts)
                .OpenScope();
        }

        private static bool TryParseOptionsFrom(
            string[] args,
            out Options opts)
        {
            try
            {
                opts = Args.Configure()
                    .WithParameter(
                        nameof(Options.Archive),
                        o => o.WithArg("-a")
                            .WithArg("--archive")
                            .WithHelp("Path / URI to use for archiving")
                    )
                    .WithParameter(
                        nameof(Options.Source),
                        o => o.WithArg("-s")
                            .WithArg("--source")
                            .Required()
                            .WithHelp("Path / URI to use for source")
                    )
                    .WithParameter(
                        nameof(Options.Target),
                        o => o.WithArg("-t")
                            .WithArg("--target")
                            .Required()
                            .WithHelp("Path / URI to use for target")
                    )
                    .WithParameter(
                        nameof(Options.HistoryDatabase),
                        o => o.WithArg("-h")
                            .WithArg("--history-db")
                            .WithHelp(
                                "Override path to history database",
                                "The default location for the database is at the root of the target"
                            )
                    )
                    .WithFlag(
                        nameof(Options.NoResume),
                        o => o.WithArg("--no-resume")
                            .WithHelp("Disable resume",
                                "The default is to resume from the current target byte offset if less than the source size"
                            )
                    )
                    .WithFlag(
                        nameof(Options.Quiet),
                        o => o.WithArg("-q")
                            .WithArg("--quiet")
                            .WithHelp("Produces less output, good for non-interactive scripts")
                    )
                    .WithFlag(
                        nameof(Options.NoHistory),
                        o => o.WithArg("-n")
                            .WithArg("--no-history")
                            .WithHelp("Disables the history database",
                                "If you don't need to have a sparse target, you can disable to history database to fall back on simpler synchronisation: what's at the source must end up at the target"
                            )
                    )
                    .WithParameter(
                        nameof(Options.SyncStrategy),
                        o => o.WithArg("--sync-strategy")
                            .WithHelp("Set the sync strategy to one of: Greedy or TargetOptIn",
                                "Greedy synchronises everything",
                                "TargetOptIn only synchronises folders which alread exist or have been recorded in the history database"
                            )
                    )
                    .WithHelp("BitSplat", "A simple file synchroniser aimed at media sync")
                    .Parse<Options>(args);
                return !opts.ShowedHelp;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                opts = null;
                return false;
            }
        }
    }
}