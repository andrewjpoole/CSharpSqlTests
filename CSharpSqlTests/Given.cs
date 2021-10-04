using System;
using System.Data;
using Microsoft.Data.SqlClient;

namespace CSharpSqlTests
{
    public class Given
    {
        private LocalDbTestContext _context;

        public Given(LocalDbTestContext context) => _context = context;

        public static Given UsingThe(LocalDbTestContext context) => new Given(context);

        public Given And() => this;

        public Given TheDacpacIsDeployed(SqlConnection connection)
        {
            _context.DeployDacpac(connection);

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

                _context.LastQueryResult = cmd.ExecuteNonQuery();

                return this;
            }
            catch (Exception e)
            {
                _context.TestOutputHelper.WriteLine(e.ToString());
                throw;
            }
        }
    }
}