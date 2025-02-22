#region License
// Copyright (c) 2018, Fluent Migrator Project
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

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

using FluentMigrator.Expressions;
using FluentMigrator.Runner.BatchParser;
using FluentMigrator.Runner.BatchParser.Sources;
using FluentMigrator.Runner.BatchParser.SpecialTokenSearchers;
using FluentMigrator.Runner.Generators;
using FluentMigrator.Runner.Generators.Generic;
using FluentMigrator.Runner.Generators.SybaseAse;
using FluentMigrator.Runner.Helpers;
using FluentMigrator.Runner.Initialization;

using JetBrains.Annotations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FluentMigrator.Runner.Processors.SybaseAse
{
    /// <summary>
    ///
    /// </summary>
    public class SybaseAseProcessor : GenericProcessorBase
    {
        [CanBeNull]
        private readonly IServiceProvider _serviceProvider;

        /// <inheritdoc />
        public override IList<string> DatabaseTypeAliases { get; } = new List<string>();

        /// <summary>
        ///
        /// </summary>
        public IQuoter Quoter { get; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="databaseTypes"></param>
        /// <param name="connection"></param>
        /// <param name="generator"></param>
        /// <param name="announcer"></param>
        /// <param name="options"></param>
        /// <param name="factory"></param>
        [Obsolete]
        public SybaseAseProcessor(
            IEnumerable<string> databaseTypes,
            IDbConnection connection,
            IMigrationGenerator generator,
            IAnnouncer announcer,
            IMigrationProcessorOptions options,
            IDbFactory factory) : base(connection, factory, generator, announcer, options)
        {
            var dbTypes = databaseTypes.ToList();
            DatabaseTypeAliases = dbTypes.Skip(1).ToList();
            Quoter = ((GenericGenerator)generator)?.Quoter;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="generator"></param>
        /// <param name="options"></param>
        /// <param name="connectionStringAccessor"></param>
        /// <param name="factory"></param>
        /// <param name="quoter"></param>
        /// <param name="serviceProvider"></param>
        public SybaseAseProcessor(
            [NotNull] ILogger<SybaseAseProcessor> logger,
            [NotNull] SybaseAseGenerator generator,
            [NotNull] IOptionsSnapshot<ProcessorOptions> options,
            [NotNull] IConnectionStringAccessor connectionStringAccessor,
            [NotNull] SybaseAseDbFactory factory,
            [NotNull] IQuoter quoter,
            [NotNull] IServiceProvider serviceProvider)
            : base(() => factory.Factory, generator, logger, options?.Value, connectionStringAccessor)
        {
            _serviceProvider = serviceProvider;
            Quoter = quoter;
        }

        public override string DatabaseType => ProcessorId.SQLite;

        private static string SafeSchemaName(string schemaName)
        {
            return string.IsNullOrEmpty(schemaName) ? "dbo" : FormatHelper.FormatSqlEscape(schemaName);
        }

        /// <inheritdoc />
        public override void BeginTransaction()
        {
            base.BeginTransaction();
            Logger.LogSql("BEGIN TRANSACTION");
        }

        /// <inheritdoc />
        public override void CommitTransaction()
        {
            base.CommitTransaction();
            Logger.LogSql("COMMIT TRANSACTION");
        }

        /// <inheritdoc />
        public override void RollbackTransaction()
        {
            if (Transaction == null)
            {
                return;
            }

            base.RollbackTransaction();
            Logger.LogSql("ROLLBACK TRANSACTION");
        }

        /// <inheritdoc />
        public override bool SchemaExists(string schemaName)
        {
            return Exists($"SELECT 1 WHERE EXISTS (select 1 from sysobjects o where user_name(o.uid) = '{SafeSchemaName(schemaName)}')");
        }

        /// <inheritdoc />
        public override bool TableExists(string schemaName, string tableName)
        {
            try
            {
                return Exists($"SELECT 1 WHERE EXISTS (select 1 from sysobjects o where user_name(o.uid) = '{SafeSchemaName(schemaName)}' and o.name = '{FormatHelper.FormatSqlEscape(tableName)}')");
            }
            catch (Exception e)
            {
                Logger.LogError(e, "There was an exception checking if table {Table} in {Schema} exists", tableName, schemaName);
            }
            return false;
        }

        /// <inheritdoc />
        public override bool ColumnExists(string schemaName, string tableName, string columnName)
        {
            return Exists($"SELECT 1 WHERE EXISTS (SELECT 1 FROM sysobjects so INNER JOIN syscolumns sc ON sc.id = so.id inner join systypes st on st.usertype = sc.usertype where user_name(so.uid) = '{SafeSchemaName(schemaName)}' and so.name = '{FormatHelper.FormatSqlEscape(tableName)}' and sc.name = '{FormatHelper.FormatSqlEscape(columnName)}')");
        }

        /// <inheritdoc />
        public override bool ConstraintExists(string schemaName, string tableName, string constraintName)
        {
            return Exists($"SELECT 1 WHERE EXISTS (SELECT 1 FROM from sysconstraints c inner join sysobjects tab on tab.id = c.tableid WHERE user_name(tab.uid) = '{SafeSchemaName(schemaName)}' AND tab.name = '{FormatHelper.FormatSqlEscape(tableName)}' AND object_name(c.constrid) = '{FormatHelper.FormatSqlEscape(constraintName)}')");
        }

        /// <inheritdoc />
        public override bool IndexExists(string schemaName, string tableName, string indexName)
        {
            return Exists(
                $"SELECT 1 WHERE EXISTS (SELECT 1 from sysindexes idx inner join sysobjects tab on tab.id = idx.id WHERE tab.type = 'U' and idx.indid > 0 and idx.status & 2 = 2 and idx.name='{0}' and user_name(tab.uid) = '{SafeSchemaName(schemaName)}' AND tab.name = '{FormatHelper.FormatSqlEscape(tableName)}'");
        }

        /// <inheritdoc />
        public override bool SequenceExists(string schemaName, string sequenceName)
        {
            return false;
        }

        /// <inheritdoc />
        public override bool DefaultValueExists(string schemaName, string tableName, string columnName, object defaultValue)
        {
            var defaultValueAsString = $"%{FormatHelper.FormatSqlEscape(defaultValue.ToString())}%";

            return Exists($@"SELECT 1 WHERE EXISTS (
                                         SELECT 1
                                         FROM syscolumns c JOIN sysobjects o ON c.id = o.id
                                                LEFT JOIN systypes t ON c.type = t.type AND c.usertype = t.usertype
                                                    LEFT JOIN syscomments cm ON cm.id = CASE WHEN c.cdefault = 0 THEN c.computedcol ELSE c.cdefault END
                                         WHERE o.type = 'U' AND
                                               user_name(o.uid) = '{SafeSchemaName(schemaName)}' AND
                                               o.name = '{FormatHelper.FormatSqlEscape(tableName)}' AND
                                               c.name = '{FormatHelper.FormatSqlEscape(columnName)}' AND
                                               cm.text LIKE '{defaultValueAsString}')");
        }

        /// <inheritdoc />
        //public new IDbConnection Connection => base.Connection;

        public override void Execute(string template, params object[] args)
        {
            Process(string.Format(template, args));
        }

        /// <inheritdoc />
        public override bool Exists(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using var command = CreateCommand(string.Format(template, args));
            var result = command.ExecuteScalar();
            return DBNull.Value != result && Convert.ToInt32(result) == 1;
        }

        /// <inheritdoc />
        public override DataSet ReadTableData(string schemaName, string tableName)
        {
            return Read("SELECT * FROM [{0}].[{1}]", SafeSchemaName(schemaName), tableName);
        }

        /// <inheritdoc />
        public override DataSet Read(string template, params object[] args)
        {
            EnsureConnectionIsOpen();

            using var command = CreateCommand(string.Format(template, args));
            using var reader = command.ExecuteReader();
            return reader.ReadDataSet();
        }

        /// <inheritdoc />
        protected override void Process(string sql)
        {
            Logger.LogSql(sql);

            if (Options.PreviewOnly || string.IsNullOrEmpty(sql))
            {
                return;
            }

            EnsureConnectionIsOpen();

            if (ContainsGo(sql))
            {
                ExecuteBatchNonQuery(sql);
            }
            else
            {
                ExecuteNonQuery(sql);
            }
        }

        private bool ContainsGo(string sql)
        {
            var containsGo = false;
            var parser = _serviceProvider?.GetService<SybaseAseBatchParser>() ?? new SybaseAseBatchParser();
            parser.SpecialToken += (sender, args) => containsGo = true;
            using (var source = new TextReaderSource(new StringReader(sql), true))
            {
                parser.Process(source);
            }

            return containsGo;
        }

        private void ExecuteNonQuery(string sql)
        {
            using (var command = CreateCommand(sql))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    using (var message = new StringWriter())
                    {
                        ReThrowWithSql(ex, sql);
                    }
                }
            }
        }

        private void ExecuteBatchNonQuery(string sql)
        {
            var sqlBatch = string.Empty;

            try
            {
                var parser = _serviceProvider?.GetService<SybaseAseBatchParser>() ?? new SybaseAseBatchParser();
                parser.SqlText += (sender, args) => sqlBatch = args.SqlText.Trim();
                parser.SpecialToken += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(sqlBatch))
                    {
                        return;
                    }

                    if (args.Opaque is GoSearcher.GoSearcherParameters goParams)
                    {
                        using (var command = CreateCommand(sqlBatch))
                        {
                            for (var i = 0; i != goParams.Count; ++i)
                            {
                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    sqlBatch = null;
                };

                using (var source = new TextReaderSource(new StringReader(sql), true))
                {
                    parser.Process(source, stripComments: Options.StripComments);
                }

                if (!string.IsNullOrEmpty(sqlBatch))
                {
                    using (var command = CreateCommand(sqlBatch))
                    {
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception ex)
            {
                ReThrowWithSql(ex, string.IsNullOrEmpty(sqlBatch) ? sql : sqlBatch);
            }
        }

        /// <inheritdoc />
        public override void Process(PerformDBOperationExpression expression)
        {
            Logger.LogSay("Performing DB Operation");

            if (Options.PreviewOnly)
            {
                return;
            }

            EnsureConnectionIsOpen();

            expression.Operation?.Invoke(Connection, Transaction);
        }
    }
}
