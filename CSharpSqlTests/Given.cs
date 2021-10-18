using System;
using System.Data;
// ReSharper disable UnusedMember.Local

namespace CSharpSqlTests
{
    public class Given
    {
        private readonly LocalDbTestContext _context;
        private readonly Action<string> _logAction;

        public Given(LocalDbTestContext context, Action<string> logAction = null)
        {
            _context = context;
            _logAction = logAction;
        }

        public static Given UsingThe(LocalDbTestContext context, Action<string> logAction = null) => new(context, logAction);

        public Given And() => this;

        private void LogMessage(string message) 
        { 
            if(_logAction is not null)
                _logAction(message);
        }

        public Given TheDacpacIsDeployed(string dacpacProjectName = "")
        {
            _context.DeployDacpac(dacpacProjectName);

            return this;
        }

        public Given TheFollowingDataExistsInTheTable(string tableName, string markdownTableString)
        {
            try
            {
                var tableData = TabularData.FromMarkdownTableString(markdownTableString);

                var cmd = _context.SqlConnection.CreateCommand();
                cmd.CommandText = tableData.ToSqlString(tableName);
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _context.SqlTransaction;

                _context.LastQueryResult = cmd.ExecuteNonQuery();

                LogMessage("TheFollowingDataExistsInTheTable executed successfully");

                return this;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception thrown while executing TheFollowingDataExistsInTheTable, {ex}");
                throw;
            }            
        }

        public Given TheForeignKeyConstraintIsRemoved(string tableName, string fkConstraintName) 
        {
            return TheFollowingSqlStatementIsExecuted($"ALTER TABLE {tableName} DROP CONSTRAINT {fkConstraintName};");
        }

        public Given TheFollowingSqlStatementIsExecuted(string sql)
        {
            try
            {
                var cmd = _context.SqlConnection.CreateCommand();
                cmd.CommandText = sql;
                cmd.CommandType = CommandType.Text;
                cmd.Transaction = _context.SqlTransaction;

                _context.LastQueryResult = cmd.ExecuteNonQuery();

                LogMessage("TheFollowingSqlStatementIsExecuted executed successfully");
                return this;
            }
            catch (Exception ex)
            {
                LogMessage($"Exception thrown while executing TheFollowingSqlStatementIsExecuted, {ex}");
                throw;
            }            
        }
    }    
}