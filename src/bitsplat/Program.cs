﻿using System;
using System.IO;
using bitsplat.Archivers;
using bitsplat.CommandLine;
using bitsplat.ResumeStrategies;
using bitsplat.StaleFileRemovers;
using bitsplat.Storage;
using DryIoc;
using static bitsplat.Filters.FilterRegistrations;

namespace bitsplat
{
    public class Program
    {
        public static int Main(params string[] args)
        {
            Console.CancelKeyPress += OnCancel;
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            if (!TryParseOptionsFrom(args, out var opts))
            {
                return -1;
            }

            if (opts.ShowedHelp ||
                opts.ShowedVersion)
            {
                return 0;
            }

            // TODO: when more than just local filesystems are handled,
            //       this should move elsewhere
            if (!Directory.Exists(opts.Target) &&
                opts.CreateTargetIfRequired)
            {
                LocalFileSystem.EnsureFolderExists(opts.Target);
            }

            using var container = CreateContainerScopeFor(opts);

            var source = container.ResolveSourceFileSystem();
            var target = container.ResolveTargetFileSystem();
            var archive = container.ResolveArchiveFileSystem();
            var archiver = container.Resolve<IArchiver>();
            var staleFileRemover = container.Resolve<IStaleFileRemover>();

            archiver.RunArchiveOperations(
                target,
                archive,
                source);

            staleFileRemover.RemoveStaleFiles(source, target);

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

        private static IResolverContext CreateContainerScopeFor(IOptions opts)
        {
            return AppContainer.Create()
                .WithOptions(opts)
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
                        : typeof(SimpleResumeStrategy)
                )
                .WithFilter(
                    FilterMap[opts.SyncMode]
                )
                .WithArchive(opts.Archive)
                .WithProgressReporterFor(opts)
                .WithMessageWriterFor(opts)
                .WithStaleFileRemoverFor(opts)
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
                    .WithParameter(
                        nameof(Options.ResumeCheckBytes),
                        o => o.WithArg("--resume-check-bytes")
                            .WithDefault(new[] { SimpleResumeStrategy.DEFAULT_CHECK_BYTES.ToString() })
                            .WithHelp(
                                "How many bytes to check at the end of a partial file when considering resume")
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
                        nameof(Options.SyncMode),
                        o => o.WithArg("-m")
                            .WithArg("--mode")
                            .WithHelp("Set the sync mode to one of: All or Opt-In (case-insensitive)",
                                "All synchronises everything",
                                "OptIn only synchronises folders which already exist or have been recorded in the history database"
                            )
                    )
                    .WithFlag(
                        nameof(Options.KeepStaleFiles),
                        o => o.WithArg("--keep-stale")
                            .WithArg("-k")
                            .WithHelp("Keep stale files (ie files removed from source and still at the target")
                    )
                    .WithFlag(
                        nameof(Options.CreateTargetIfRequired),
                        o => o.WithArg("-c")
                            .WithArg("--create-missing-target")
                            .WithHelp("Create the target folder if it's missing")
                    )
                    .WithParameter(
                        nameof(Options.Retries),
                        o => o.WithArg("-r")
                            .WithArg("--retries")
                            .WithDefault(new[] { "3" }) // TODO: this is a bit ugly -- can it be better?
                            .WithHelp("Retry failed synchronisations at most this many times")
                    )
                    .WithFlag(
                        nameof(Options.DryRun),
                        o => o.WithArg("-d")
                            .WithArg("--dry-run")
                            .WithHelp("Only report what would be done instead of actually doing it")
                            .WithDefault(false)
                    )
                    .WithFlag(
                        nameof(Options.Verbose),
                        o => o.WithArg("-v")
                            .WithArg("--verbose")
                            .WithDefault(false)
                            .WithHelp("Print a lot more logging")
                    )
                    .WithFlag(
                        nameof(Options.Version),
                        o => o.WithArg("--version")
                            .WithDefault(false)
                    )
                    .WithHelp("BitSplat", "A simple file synchroniser aimed at media sync")
                    .Parse<Options>(args);
                return true;
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