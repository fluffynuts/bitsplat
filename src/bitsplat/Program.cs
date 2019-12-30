using System;
using System.Collections.Generic;
using bitsplat.Archivers;
using bitsplat.CommandLine;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.ResumeStrategies;
using bitsplat.Storage;
using DryIoc;
using static bitsplat.Filters.FilterRegistrations;

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

            var container = AppContainer.Create()
                .WithHistoryRepository(
                    opts.NoHistory
                        ? typeof(NullTargetHistoryRepository)
                        : typeof(TargetHistoryRepository)
                )
                .WithSource(
                    opts.Source
                )
                .WithTarget(
                    opts.Target
                )
                .WithResumeStrategy(
                    opts.Resume
                        ? typeof(AlwaysResumeStrategy)
                        : typeof(NeverResumeStrategy)
                )
                .WithFilter(
                    FilterMap[opts.SyncStrategy]
                )
                .WithArchiver(
                    string.IsNullOrWhiteSpace(opts.Archive)
                        ? typeof(NullArchiver)
                        : typeof(Mede8erArchiver)
                )
                .WithArchive(opts.Archive);

            var source = container.Resolve<ISourceFileSystem>();
            var target = container.Resolve<ITargetFileSystem>();
            var archive = container.Resolve<IArchiveFileSystem>();
            var archiver = container.Resolve<IArchiver>();
            archiver.RunArchiveOperations(
                source,
                archive);
            
            var synchronizer = container.Resolve<ISynchronizer>();
            synchronizer.Synchronize(
                source,
                target);
        }

    }

    public static class AppContainer
    {
        public static IContainer Create()
        {
            var result = new Container();
            result.Register<ISynchronizer, Synchronizer>();
            return result;
        }

        public static IContainer WithArchive(
            this IContainer container,
            string archive)
        {
            if (string.IsNullOrWhiteSpace(archive))
            {
                return container.WithRegistration(
                        typeof(IArchiveFileSystem),
                        CachingFileSystem.For(archive)
                    )
                    .WithRegistration(
                        typeof(IArchiver),
                        // TODO: allow different archivers
                        // - eg Kodi, which can also mark media as "watched"
                        typeof(Mede8erArchiver)
                    );
            }

            return container.WithRegistration(
                    typeof(IArchiveFileSystem),
                    new NullFileSystem()
                )
                .WithRegistration(
                    typeof(IArchiver),
                    typeof(NullArchiver)
                );
        }

        public static IContainer WithArchiver(
            this IContainer container,
            Type implementation)
        {
            return container.WithRegistration(
                typeof(IArchiver),
                implementation
            );
        }

        public static IContainer WithFilter(
            this IContainer container,
            Type impl)
        {
            return container.WithRegistration(
                typeof(IFilter),
                impl
            );
        }

        public static IContainer WithResumeStrategy(
            this IContainer container,
            Type implementation)
        {
            return container.WithRegistration(
                typeof(IResumeStrategy),
                implementation
            );
        }

        public static IContainer WithSource(
            this IContainer container,
            string uri)
        {
            return container.WithRegistration(
                typeof(ISourceFileSystem),
                CachingFileSystem.For(uri)
            );
        }

        public static IContainer WithTarget(
            this IContainer container,
            string uri)
        {
            return container.WithRegistration(
                typeof(ITargetFileSystem),
                CachingFileSystem.For(uri)
            );
        }

        public static IContainer WithHistoryRepository(
            this IContainer container,
            Type implementing
        )
        {
            return container.WithRegistration(
                typeof(ITargetHistoryRepository),
                implementing
            );
        }

        public static IContainer WithRegistration(
            this IContainer container,
            Type serviceType,
            Type implementationType
        )
        {
            return container.WithRegistration(
                serviceType,
                implementationType,
                Reuse.Transient
            );
        }

        private static IContainer WithRegistration(
            this IContainer container,
            Type serviceType,
            Type implementationType,
            IReuse reuse
        )
        {
            return container.With(
                o => o.Register(
                    serviceType,
                    implementationType,
                    reuse)
            );
        }

        private static IContainer WithRegistration<T>(
            this IContainer container,
            Type serviceType,
            T implementation
        )
        {
            return container.With(
                o => o.Register(
                    serviceType,
                    typeof(T),
                    Reuse.Singleton
                )
            );
        }

        private static IContainer With(
            this IContainer container,
            Action<IContainer> toRun)
        {
            toRun(container);
            return container;
        }
    }
}