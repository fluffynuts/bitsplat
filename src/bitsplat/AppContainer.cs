using System;
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
            result.Register<ISynchronizer, Synchronizer>();
            return result;
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
            return container.WithRegistration(
                    typeof(IFileSystem),
                    CachingFileSystem.For(archive),
                    ServiceKeys.ARCHIVE
                )
                // TODO: allow different archivers
                // - eg Kodi, which can also mark media as "watched"
                .WithRegistration<IArchiver, Mede8erArchiver>();
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

        private static IContainer With(
            this IContainer container,
            Action<IContainer> toRun)
        {
            toRun(container);
            return container;
        }
    }
}