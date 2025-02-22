#region License
// Copyright (c) 2007-2018, Sean Chambers and the FluentMigrator Project
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

using System.Collections.Generic;
using System.Linq;

using FluentMigrator.Model;
using FluentMigrator.Runner.Generators.Base;

namespace FluentMigrator.Runner.Generators.SybaseAse
{
    internal class SybaseAseColumn : ColumnBase
    {
        public SybaseAseColumn(ITypeMap typeMap, IQuoter quoter) : base(typeMap, quoter)
        {
        }

        /// <inheritdoc />
        protected override string FormatDefaultValue(ColumnDefinition column)
        {
            if (DefaultValueIsSqlFunction(column.DefaultValue))
                return "DEFAULT " + column.DefaultValue;

            var defaultValue = base.FormatDefaultValue(column);

            if (column.ModificationType == ColumnModificationType.Create && !string.IsNullOrEmpty(defaultValue))
                return "CONSTRAINT " + Quoter.QuoteConstraintName(GetDefaultConstraintName(column.TableName, column.Name)) + " " + defaultValue;

            return string.Empty;
        }

        private static bool DefaultValueIsSqlFunction(object defaultValue)
        {
            return defaultValue is string && defaultValue.ToString().EndsWith("()");
        }

        /// <inheritdoc />
        protected override string FormatIdentity(ColumnDefinition column)
        {
            return column.IsIdentity ? "IDENTITY" : string.Empty;
        }

        public static string FormatDefaultValue(object defaultValue, IQuoter quoter)
        {
            return DefaultValueIsSqlFunction(defaultValue) ? defaultValue.ToString() : quoter.QuoteValue(defaultValue);
        }

        public static string GetDefaultConstraintName(string tableName, string columnName)
        {
            return $"DF_{tableName}_{columnName}";
        }

        /// <inheritdoc />
        public override string Generate(IEnumerable<ColumnDefinition> columns, string tableName)
        {
            var primaryKeyString = string.Empty;

            //if more than one column is a primary key or the primary key is given a name, then it needs to be added separately

            //CAUTION: this must execute before we set the values of primarykey to false; Beware of yield return
            var colDefs = columns.ToList();
            var primaryKeyColumns = colDefs.Where(x => x.IsPrimaryKey);

            var pkColDefs = primaryKeyColumns.ToList();
            if (ShouldPrimaryKeysBeAddedSeparately(pkColDefs))
            {
                primaryKeyString = AddPrimaryKeyConstraint(tableName, pkColDefs);
                foreach (var column in colDefs)
                {
                    column.IsPrimaryKey = false;
                }
            }

            return string.Join(", ", colDefs.Select(x => Generate(x)).ToArray()) + primaryKeyString;
        }

        /// <summary>
        /// Formats the (not) null constraint
        /// </summary>
        /// <param name="column">The column definition</param>
        /// <returns>The formatted (not) null constraint</returns>
        protected override string FormatNullable(ColumnDefinition column)
        {
            if (column.IsNullable == true)
            {
                return string.Empty;
            }

            return column.IsIdentity ? string.Empty : "NOT NULL";
        }
    }
}
