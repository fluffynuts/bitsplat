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

            using var container = AppContainer.Create()
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
                .OpenScope();

            var source = container.ResolveSourceFileSystem();
            var target = container.ResolveTargetFileSystem();
            var archive = container.ResolveArchiveFileSystem();
            var archiver = container.Resolve<IArchiver>();

            archiver.RunArchiveOperations(
                target,
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
            if (!string.IsNullOrWhiteSpace(archive))
            {
                return container.WithRegistration(
                        typeof(IFileSystem),
                        CachingFileSystem.For(archive),
                        ServiceKeys.ARCHIVE
                    )
                    .WithRegistration(
                        typeof(IArchiver),
                        // TODO: allow different archivers
                        // - eg Kodi, which can also mark media as "watched"
                        typeof(Mede8erArchiver)
                    );
            }

            return container.WithRegistration(
                    typeof(IFileSystem),
                    new NullFileSystem(),
                    ServiceKeys.ARCHIVE
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
                typeof(IFileSystem),
                CachingFileSystem.For(uri),
                "source"
            );
        }

        public static IContainer WithTarget(
            this IContainer container,
            string uri)
        {
            return container.WithRegistration(
                typeof(IFileSystem),
                CachingFileSystem.For(uri),
                "target"
            );
        }

        public static IContainer WithHistoryRepositoryFor(
            this IContainer container,
            Options opts)
        {
            if (opts.NoHistory)
            {
                return container.WithRegistration(
                    typeof(ITargetHistoryRepository),
                    new NullTargetHistoryRepository()
                );
            }

            return container.WithRegistration(
                typeof(ITargetHistoryRepository),
                new TargetHistoryRepository(opts.Target)
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

        public static IFileSystem ResolveSourceFileSystem(
            this IResolverContext context)
        {
            return context.Resolve<IFileSystem>(ServiceKeys.SOURCE);
        }

        public static IFileSystem ResolveTargetFileSystem(
            this IResolverContext context)
        {
            return context.Resolve<IFileSystem>(ServiceKeys.TARGET);
        }

        public static IFileSystem ResolveArchiveFileSystem(
            this IResolverContext context)
        {
            return context.Resolve<IFileSystem>(ServiceKeys.ARCHIVE);
        }

        private static class ServiceKeys
        {
            public const string SOURCE = "source";
            public const string TARGET = "target";
            public const string ARCHIVE = "archive";
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
            T implementation,
            string serviceKey = null
        )
        {
            return container.With(
                o => o.RegisterDelegate(
                    serviceType,
                    c => implementation,
                    Reuse.Singleton,
                    serviceKey: serviceKey
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