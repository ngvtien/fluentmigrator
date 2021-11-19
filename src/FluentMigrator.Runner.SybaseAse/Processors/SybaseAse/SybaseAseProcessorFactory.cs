using System;

using FluentMigrator.Runner.Generators.SybaseAse;

namespace FluentMigrator.Runner.Processors.SybaseAse
{
    [Obsolete]
    public class SybaseAseProcessorFactory : MigrationProcessorFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private static readonly string[] _dbTypes = {"SybaseAse"};

        [Obsolete]
        public SybaseAseProcessorFactory()
        {
            
        }

        public SybaseAseProcessorFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        [Obsolete]
        public override IMigrationProcessor Create(string connectionString, IAnnouncer announcer, IMigrationProcessorOptions options)
        {
            var factory = new SybaseAseDbFactory(_serviceProvider);
            var connection = factory.CreateConnection(connectionString);
            return
                new SybaseAseProcessor(_dbTypes, connection, new SybaseAse2000Generator(new SybaseAse2000Quoter()), announcer, options, factory);
        }

        [Obsolete]
        public override bool IsForProvider(string provider)
        {
            return provider.ToLower().Contains("adonetnore.aseclient");
        }
    }
}
