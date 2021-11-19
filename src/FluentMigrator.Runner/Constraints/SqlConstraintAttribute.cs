#region License
// Copyright (c) 2021, FluentMigrator Project
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

namespace FluentMigrator.Runner.Constraints
{
    /// <summary>
    /// Can be used to apply conditions when migrations will be run.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SqlConstraintAttribute : Attribute
    {
        /// <summary>
        /// Validate SQL script
        /// </summary>
        public string ValidationScript { get; }

        /// <inheritdoc />
        public SqlConstraintAttribute(string validationScript)
        {
            ValidationScript = validationScript;
        }
    }
}
