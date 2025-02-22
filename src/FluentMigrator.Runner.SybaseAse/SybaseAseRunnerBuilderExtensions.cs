#region License
// Copyright (c) 2018, FluentMigrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using System.Linq;

using FluentMigrator.Runner.BatchParser;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Generators.Generic;
using FluentMigrator.Runner.Generators.SybaseAse;
using FluentMigrator.Runner.Processors;
using FluentMigrator.Runner.Processors.SybaseAse;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner
{
    /// <summary>
    /// Extension methods for <see cref="IMigrationRunnerBuilder"/>
    /// </summary>
    public static class SybaseAseRunnerBuilderExtensions
    {
        /// <summary>
        /// Adds SQL Server support
        /// </summary>
        /// <remarks>
        /// This always selects the latest supported SQL server version.
        /// </remarks>
        /// <param name="builder">The builder to add the SQL Server-specific services to</param>
        /// <returns>The migration runner builder</returns>
        public static IMigrationRunnerBuilder AddSybaseAse(this IMigrationRunnerBuilder builder)
        {
            builder.Services.TryAddTransient<SybaseAseBatchParser>();
            builder.Services.TryAddScoped<SybaseAseQuoter>();
            builder.Services
                .AddScoped<SybaseAseProcessor>()
                .AddScoped<IMigrationProcessor>(sp => sp.GetRequiredService<SybaseAseProcessor>())
                .AddScoped<SybaseAseGenerator>()
                .AddScoped<IMigrationGenerator>(sp => sp.GetRequiredService<SybaseAseGenerator>());
            return builder.AddCommonSybaseAseServices();
        }

        /// <summary>
        /// Add common Postgres services.
        /// </summary>
        /// <param name="builder">The builder to add the Postgres-specific services to</param>
        /// <returns>The migration runner builder</returns>
        private static IMigrationRunnerBuilder AddCommonSybaseAseServices(this IMigrationRunnerBuilder builder)
        {
            //var opt = new OptionsManager<ProcessorOptions>(new OptionsFactory<ProcessorOptions>(
            //    Enumerable.Empty<IConfigureOptions<ProcessorOptions>>(),
            //    Enumerable.Empty<IPostConfigureOptions<ProcessorOptions>>()));
            builder.Services
                //.AddScoped<IOptionsSnapshot<ProcessorOptions>>()
                //.AddScoped(
                //    sp => new OptionsManager<ProcessorOptions>(new OptionsFactory<ProcessorOptions>(
                //        Enumerable.Empty<IConfigureOptions<ProcessorOptions>>(),
                //        Enumerable.Empty<IPostConfigureOptions<ProcessorOptions>>())))
                .AddScoped<IQuoter, GenericQuoter>()
                .AddScoped<SybaseAseDbFactory>()
                .AddScoped<SybaseAseQuoter>();
            return builder;
        }
    }
}
