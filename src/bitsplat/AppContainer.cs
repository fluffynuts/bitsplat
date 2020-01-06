using System;
using System.IO;
using bitsplat.Archivers;
using bitsplat.Filters;
using bitsplat.History;
using bitsplat.Pipes;
using bitsplat.ResumeStrategies;
using bitsplat.Storage;
using DryIoc;

namespace bitsplat
{
    public static class AppContainer
    {
        public static IContainer Create()
        {
            var result = new Container();
            return result
                .WithRegistration<ISynchronizer, Synchronizer>()
                .WithRegistration<IFileSystemFactory, FileSystemFactory>(
                    Reuse.Singleton
                );
        }

        public static IContainer WithArchive(
            this IContainer container,
            string archive)
        {
            return string.IsNullOrWhiteSpace(archive)
                       ? container.WithNullArchiver()
                       : container.WithMede8erArchiverTargeting(archive);

            // TODO: allow different archivers
            // - eg Kodi, which can also mark media as "watched"
        }

        private static IContainer WithNullArchiver(
            this IContainer container)
        {
            return container.WithRegistration(
                    typeof(IFileSystem),
                    new NullFileSystem(),
                    ServiceKeys.ARCHIVE
                )
                .WithRegistration<IArchiver, NullArchiver>();
        }

        private static IContainer WithMede8erArchiverTargeting(
            this IContainer container,
            string archive)
        {
            return container.WithArchiveFileSystem(archive)
                // TODO: allow different archivers
                // - eg Kodi, which can also mark media as "watched"
                .WithRegistration<IArchiver, Mede8erArchiver>();
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
                r => r.Resolve<IFileSystemFactory>()
                    .CachingFileSystemFor(uri),
                ServiceKeys.SOURCE,
                Reuse.Singleton
            );
        }

        public static IContainer WithTarget(
            this IContainer container,
            string uri)
        {
            return container.WithRegistration(
                typeof(IFileSystem),
                r => r.Resolve<IFileSystemFactory>()
                    .CachingFileSystemFor(uri),
                ServiceKeys.TARGET,
                Reuse.Singleton
            );
        }

        private static IContainer WithArchiveFileSystem(
            this IContainer container,
            string uri)
        {
            return container.WithRegistration(
                typeof(IFileSystem),
                r => r.Resolve<IFileSystemFactory>()
                    .CachingFileSystemFor(uri),
                ServiceKeys.ARCHIVE,
                Reuse.Singleton
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

            return container.WithRegistration<ITargetHistoryRepository>(
                typeof(ITargetHistoryRepository),
                r =>
                {
                    if (opts.NoHistory)
                    {
                        return new NullTargetHistoryRepository();
                    }

                    var targetFolder = opts.Target;
                    if (string.IsNullOrWhiteSpace(opts.HistoryDatabase))
                    {
                        opts.HistoryDatabase = TargetHistoryRepository.DB_NAME;
                    }
                    else
                    {
                        targetFolder = Path.GetDirectoryName(opts.HistoryDatabase);
                        opts.HistoryDatabase = Path.GetFileName(opts.HistoryDatabase);
                    }

                    LocalFileSystem.EnsureFolderExists(
                        Path.GetDirectoryName(
                            targetFolder
                        )
                    );

                    return new TargetHistoryRepository(
                        r.Resolve<IMessageWriter>(),
                        targetFolder,
                        opts.HistoryDatabase
                    );
                },
                null,
                Reuse.Singleton
            );
        }

        public static IContainer WithRegistration<TService, TImpl>(
            this IContainer container
        ) where TImpl : TService
        {
            return container.WithRegistration<TService, TImpl>(
                Reuse.Transient
            );
        }

        public static IContainer WithRegistration<TService, TImpl>(
            this IContainer container,
            IReuse reuse
        ) where TImpl : TService
        {
            return container.WithRegistration(
                typeof(TService),
                typeof(TImpl),
                reuse
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

        public static IContainer WithMessageWriterFor(
            this IContainer container,
            Options opts)
        {
            return container.WithRegistration<IMessageWriter, ConsoleMessageWriter>(
                Reuse.Singleton
            );
        }

        public static IContainer WithProgressReporterFor(
            this IContainer container,
            Options opts)
        {
            return container.WithRegistration<IPassThrough, SynchronisationProgressPipe>(
                    Reuse.Singleton
                )
                .WithConsoleReporterFor(opts);
        }

        private static IContainer WithConsoleReporterFor(
            this IContainer container,
            Options opts)
        {
            return opts.Quiet
                       ? container.WithRegistration<IProgressReporter, SimpleConsoleProgressReporter>()
                       : container.WithRegistration<IProgressReporter, SimplePercentageConsoleProgressReporter>();
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

        private static IContainer WithRegistration<T>(
            this IContainer container,
            Type serviceType,
            Func<IResolverContext, T> factory,
            string serviceKey,
            IReuse reuse
        )
        {
            return container.With(
                o => o.RegisterDelegate(
                    serviceType,
                    c => factory(c),
                    reuse,
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