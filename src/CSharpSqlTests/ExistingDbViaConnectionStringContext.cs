using System;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using MartinCostello.SqlLocalDb;

// ReSharper disable InconsistentNaming

namespace CSharpSqlTests
{
    public class ExistingDbViaConnectionStringContext : ISqlDatabaseContext
    {
        private readonly string _connectionString;

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
            var createDbCmd = connection.CreateCommand();
            createDbCmd.CommandText = @$"CREATE DATABASE [{databaseName}] ON  PRIMARY 
    ( NAME = N'{databaseName}_Data', FILENAME = N'{databaseName}.mdf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )
     LOG ON 
    ( NAME = N'{databaseName}_Log', FILENAME = N'{databaseName}.ldf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )";
            createDbCmd.CommandType = CommandType.Text;
            createDbCmd.ExecuteNonQuery();
        }
    }

    public class ExistingLocalDbViaInstanceNameContext : ISqlDatabaseContext
    {
        private readonly Action<string>? _logTiming;
        private readonly bool _stopNormalInstanceAfterwards;
        private readonly ISqlLocalDbInstanceManager _manager;
        private readonly string _instanceName;

        private string _instancePath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\{_instanceName}";

        public ExistingLocalDbViaInstanceNameContext(string localDbInstanceName, Action<string>? logTiming = null)
        {
            _logTiming = logTiming;
            var localDbApi = new SqlLocalDbApi();

            var normalInstance = localDbApi.GetOrCreateInstance(localDbInstanceName);
            _manager = normalInstance.Manage();

            if (!normalInstance.IsRunning)
            {
                _logTiming?.Invoke($"Starting localDb instance {_instanceName}");
                _stopNormalInstanceAfterwards = true;
                _manager.Start();
            }

            _instanceName = normalInstance.Name;
        }

        public IDbConnection GetNewSqlConnection()
        {
            return _manager.CreateConnection();
        }

        public void TearDown()
        {
            if (_stopNormalInstanceAfterwards)
            {
                _logTiming?.Invoke("Stopping localDb instance (because it wasn't running beforehand)");
                _manager.Stop();
            }
        }

        public void CreateNewDatabase(string databaseName)
        {
            using var connection = GetNewSqlConnection();
            var createDbCmd = connection.CreateCommand();
            createDbCmd.CommandText = @$"CREATE DATABASE [{databaseName}] ON  PRIMARY 
    ( NAME = N'{databaseName}_Data', FILENAME = N'{_instancePath}\{databaseName}.mdf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )
     LOG ON 
    ( NAME = N'{databaseName}_Log', FILENAME = N'{_instancePath}\{databaseName}.ldf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )";
            createDbCmd.CommandType = CommandType.Text;
            createDbCmd.ExecuteNonQuery();
        }
    }

    public class TemporaryLocalDbContext : ISqlDatabaseContext
    {
        private readonly Action<string>? _logTiming;
        private readonly ISqlLocalDbInstanceManager _manager;
        private readonly string _instanceName;
        private readonly TemporarySqlLocalDbInstance _temporaryInstance;

        private string _instancePath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\{_instanceName}";

        public TemporaryLocalDbContext(Action<string>? logTiming = null)
        {
            _logTiming = logTiming;
            var localDbApi = new SqlLocalDbApi();

            _temporaryInstance = localDbApi.CreateTemporaryInstance();
            _manager = _temporaryInstance.Manage();

            _logTiming?.Invoke($"Temporary localDb instance created named {_temporaryInstance.Name}");

            _instanceName = _temporaryInstance.Name;
            
        }

        public IDbConnection GetNewSqlConnection()
        {
            return _manager.CreateConnection();
        }

        public void TearDown()
        {
            _manager.Stop();

            _logTiming?.Invoke("Instance stopped");

            var tempInstanceDir = new DirectoryInfo(_instancePath);

            tempInstanceDir.Delete(true);

            _logTiming?.Invoke("Instance directory deleted");
        }

        public void CreateNewDatabase(string databaseName)
        {
            using var connection = GetNewSqlConnection();
            var createDbCmd = connection.CreateCommand();
            createDbCmd.CommandText = @$"CREATE DATABASE [{databaseName}] ON  PRIMARY 
    ( NAME = N'{databaseName}_Data', FILENAME = N'{_instancePath}\{databaseName}.mdf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )
     LOG ON 
    ( NAME = N'{databaseName}_Log', FILENAME = N'{_instancePath}\{databaseName}.ldf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )";
            createDbCmd.CommandType = CommandType.Text;
            createDbCmd.ExecuteNonQuery();
        }
    }
}