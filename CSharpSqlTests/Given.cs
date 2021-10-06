using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CSharpSqlTests
{
    public class Given
    {
        private LocalDbTestContext2 _context;
        private readonly Action<string> _logAction;

        public Given(LocalDbTestContext2 context, Action<string> logAction = null)
        {
            _context = context;
            _logAction = logAction;
        }

        public static Given UsingThe(LocalDbTestContext2 context, Action<string> logAction = null) => new Given(context, logAction);

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
                var tableData = TableDefinition.FromMarkdownTableString(markdownTableString);

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
                LogMessage($"Exception thrown while executing TheFollowingDataExistsInTheTable, {ex.ToString}");
                throw;
            }
            
        }
    }    
}