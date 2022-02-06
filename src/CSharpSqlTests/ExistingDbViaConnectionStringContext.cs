using System.Data;
#pragma warning disable CS1591

namespace CSharpSqlTests
{
    public class ExistingDbViaConnectionStringContext : ISqlDatabaseContext
    {
        private readonly string _connectionString;

        public string ConnectionString => _connectionString;

        public ExistingDbViaConnectionStringContext(string connectionString)
        {
            _connectionString = connectionString;
        }

        public IDbConnection GetNewSqlConnection()
        {
            return new System.Data.SqlClient.SqlConnection(_connectionString);
        }

        public void TearDown()
        {
        }

        public void CreateNewDatabase(string databaseName)
        {
            using var connection = GetNewSqlConnection();
            connection.Open();
            var createDbCmd = connection.CreateCommand();
            createDbCmd.CommandText = @$"CREATE DATABASE [{databaseName}]";
            createDbCmd.CommandType = CommandType.Text;
            createDbCmd.ExecuteNonQuery();
            connection.Close();
        }
    }
}