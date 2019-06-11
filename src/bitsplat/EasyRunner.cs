using System;
using System.Linq;
using System.Reflection;
using FluentMigrator.Runner;
using FluentMigrator.Runner.Conventions;
using FluentMigrator.Runner.Initialization;
using FluentMigrator.Runner.Logging;
using FluentMigrator.Runner.Processors;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace bitsplat
{
    public class EasyRunner
    {
        private readonly string _connectionString;
        private readonly Assembly[] _scanAssemblies;

        public EasyRunner(
            string connectionString,
            params Assembly[] scanAssemblies)
        {
            _connectionString = connectionString;
            _scanAssemblies = scanAssemblies;
        }

        public void MigrateUp()
        {
            var services = new ServiceCollection();
            services.AddFluentMigratorCore()
                .ConfigureRunner(
                    builder => builder.AddSQLite()
                )
                .AddScoped<IConnectionStringReader>(
                    _ => new PassThroughConnectionStringReader(_connectionString)
                )
                .Configure<FluentMigratorLoggerOptions>(
                    opts =>
                    {
                        opts.ShowSql = false;
                        opts.ShowElapsedTime = false;
                    })
                .AddSingleton<IConventionSet>(
                    new DefaultConventionSet()
                )
                .Configure<SelectingProcessorAccessorOptions>(
                    opt => opt.ProcessorId = "sqlite")
                .Configure<AssemblySourceOptions>(
                    opt => opt.AssemblyNames = _scanAssemblies.Select(a => a.FullName)
                               .ToArray()
                )
                .Configure<TypeFilterOptions>(opt =>
                {
                    opt.Namespace = null;
                    opt.NestedNamespaces = true;
                })
                .Configure<RunnerOptions>(opts =>
                {
                    opts.Task = "migrate";
                    opts.NoConnection = false;
                })
                .Configure<ProcessorOptions>(opts =>
                {
                    opts.ConnectionString = _connectionString;
                    opts.PreviewOnly = false;
                    opts.StripComments = true;
                })
                .AddSingleton<ILoggerProvider, EasyLoggerProvider>();

            using (var provider = services.BuildServiceProvider())
            {
                var executor = provider.GetRequiredService<TaskExecutor>();
                executor.Execute();
            }
        }
    }

    public class EasyLoggerProvider : ILoggerProvider
    {
        public class EasyLogger : FluentMigratorLogger
        {
            // TODO: allow injection of a logging action
            public EasyLogger()
                : base(new FluentMigratorLoggerOptions()
                {
                    ShowSql = false,
                    ShowElapsedTime = false
                })
            {
            }

            protected override void WriteError(string message)
            {
                Console.Error.WriteLine(message);
                throw new FatalMigrationException(message);
            }

            protected override void WriteError(Exception exception)
            {
            }

            protected override void WriteHeading(string message)
            {
            }

            protected override void WriteEmphasize(string message)
            {
            }

            protected override void WriteSql(string sql)
            {
            }

            protected override void WriteEmptySql()
            {
            }

            protected override void WriteElapsedTime(TimeSpan timeSpan)
            {
            }

            protected override void WriteSay(string message)
            {
            }
        }

        public void Dispose()
        {
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new EasyLogger();
        }
    }

    public class FatalMigrationException
        : Exception
    {
        public FatalMigrationException(string message)
            : base(message)
        {
        }
    }

    public class EasyProcessorOptions
        : IOptionsSnapshot<ProcessorOptions>
    {
        public ProcessorOptions Value { get; }

        public ProcessorOptions Get(string name)
        {
            return Value;
        }

        public EasyProcessorOptions(string connectionString)
            : this(new ProcessorOptions()
            {
                ConnectionString = connectionString,
            })
        {
        }

        public EasyProcessorOptions(ProcessorOptions options)
        {
            Value = options;
        }
    }

    public class EasyRunnerOptions
        : IOptions<RunnerOptions>
    {
        public RunnerOptions Value { get; }

        public EasyRunnerOptions()
            : this(new RunnerOptions()
            {
                Task = "migrate"
            })
        {
        }

        public EasyRunnerOptions(RunnerOptions options)
        {
            Value = options;
        }
    }
}