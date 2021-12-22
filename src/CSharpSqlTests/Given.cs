﻿using System;
using System.Data;
// ReSharper disable UnusedMember.Local

namespace CSharpSqlTests
{
    public class Given
    {
        public readonly ILocalDbTestContext Context;

        public Given(ILocalDbTestContext context)
        {
            Context = context;
        }

        public static Given UsingThe(ILocalDbTestContext context) => new(context);

        public Given And => this;
        
        /// <summary>
        /// A method which deploys a DacPac into the localDb context.
        /// </summary>
        /// <param name="dacpacProjectName">A string containing the name of a DacPac file to deploy, this can be an absolute path or a name to search for minus the extension.</param>
        public Given TheDacpacIsDeployed(string dacpacProjectName = "")
        {
            Context.DeployDacpac(dacpacProjectName);

            return this;
        }

        /// <summary>
        /// A method which inserts some data into the database.
        /// </summary>
        /// <param name="tableName">A string containing the name of the table to insert data into.</param>
        /// <param name="markdownTableString">
        /// A markdown table string containing the data to insert
        /// <example>
        /// <code>
        ///  | Id | Type | Make | Model   |
        ///  | -- | ---- | ---- | ------- |
        ///  | 1  | Car  | Fiat | 500     |
        ///  | 2  | Van  | Ford | Transit |
        /// this string represents a table with 4 columns and 2 rows.
        /// 
        /// A string value in a column of: 
        /// 2021-11-03  -> will be interpreted as a DateTime, use any parsable date and time string
        /// 234         -> will be interpreted as an int
        /// null        -> will be interpreted as null
        /// emptyString -> will be interpreted as an empty string
        /// true        -> will be interpreted as boolean true
        /// false       -> will be interpreted as boolean false
        /// </code>
        /// </example>
        /// </param>
        public Given TheFollowingDataExistsInTheTable(string tableName, string markdownTableString)
        {
            var tabularData = TabularData.FromMarkdownTableString(markdownTableString);
            return TheFollowingDataExistsInTheTable(tableName, tabularData);
        }

        /// <summary>
        /// A method which inserts some data into the database.
        /// </summary>
        /// <param name="tableName">A string containing the name of the table to insert data into.</param>
        /// <param name="tabularData">A TabularData containing the data to insert.</param>
        public Given TheFollowingDataExistsInTheTable(string tableName, TabularData tabularData)
        {
            var cmd = Context.SqlConnection.CreateCommand();
            cmd.CommandText = tabularData.ToSqlString(tableName);
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = Context.SqlTransaction;

            Context.LastQueryResult = cmd.ExecuteNonQuery();

            return this;
        }

        /// <summary>
        /// A method which removes a Foreign Key constraint, for instance so you don't have to setup data that you are not immediately testing.
        /// </summary>
        /// <param name="tableName">A string containing the name of the table.</param>
        /// <param name="fkConstraintName">A string containing the name of the FK to remove.</param>
        public Given TheForeignKeyConstraintIsRemoved(string tableName, string fkConstraintName)
        {
            return TheFollowingSqlStatementIsExecuted($"ALTER TABLE {tableName} DROP CONSTRAINT {fkConstraintName};");
        }

        /// <summary>
        /// A method which executes a Sql statement.
        /// </summary>
        /// <param name="sql">A string containing the Sql statement to execute.</param>
        public Given TheFollowingSqlStatementIsExecuted(string sql)
        {
            var cmd = Context.SqlConnection.CreateCommand();
            cmd.CommandText = sql;
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = Context.SqlTransaction;

            Context.LastQueryResult = cmd.ExecuteNonQuery();
            return this;
        }
    }
}