using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CSharpSqlTests
{
    public class Given
    {
        private LocalDbTestContext2 _context;

        public Given(LocalDbTestContext2 context) => _context = context;

        public static Given UsingThe(LocalDbTestContext2 context) => new Given(context);

        public Given And() => this;

        public Given TheDacpacIsDeployed(string dacpacProjectName = "")
        {
            _context.DeployDacpac(dacpacProjectName);

            return this;
        }

        public Given TheFollowingDataExistsInTheTable(string tableName, string markdownTableString)
        {
            var tableData = TableDefinition.FromMarkdownTableString(markdownTableString);

            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = tableData.ToSqlString(tableName);
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = _context.SqlTransaction;

            _context.LastQueryResult = cmd.ExecuteNonQuery();

            return this;
        }
    }
}