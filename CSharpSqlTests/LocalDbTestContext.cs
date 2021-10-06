using System;
using System.IO;
using MartinCostello.SqlLocalDb;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;

namespace CSharpSqlTests
{
    public class LocalDbTestContext
    {
        private string _databaseName;
        private readonly Action<string> _logTiming;
        private SqlLocalDbApi _localDbApi;
        private TemporarySqlLocalDbInstance _instance;
        private ISqlLocalDbInstanceManager _manager;
        private string _instanceName;
        private DateTime _instanceStartedTime;
        private DateTime _lastLogTime;

        public SqlConnection SqlConnection;
        public SqlTransaction SqlTransaction;
        public object LastQueryResult;

        public LocalDbTestContext(string databaseName, Action<string> logTiming = null)
        {
            _databaseName = databaseName;
            _logTiming = logTiming;
        }

        public LocalDbTestContext Start()
        {
            _lastLogTime = DateTime.Now;

            _localDbApi = new SqlLocalDbApi();
            _instance = _localDbApi.CreateTemporaryInstance();
            _manager = _instance.Manage();
            
            _instanceStartedTime = DateTime.Now;
            LogTiming("temporary localDb instance created");

            _instanceName = _instance.Name;

            // Open connection (to Master database)
            SqlConnection = new SqlConnection(_instance.ConnectionString);
            SqlConnection.Open();
            LogTiming("connection opened");

            // Create temp database
            using (var command = new SqlCommand($"DROP DATABASE IF EXISTS {_databaseName}", SqlConnection)) command.ExecuteNonQuery();
            using (var command = new SqlCommand($"CREATE DATABASE {_databaseName}", SqlConnection)) command.ExecuteNonQuery();
            LogTiming("database connected");

            SqlConnection.ChangeDatabase(_databaseName);

            // ready to run tests in individual transactions

            return this;
        }

        public LocalDbTestContext RunTest(Action<SqlConnection, SqlTransaction> useConnection)
        {
            SqlTransaction = SqlConnection.BeginTransaction(DateTime.Now.Ticks.ToString());

            useConnection(SqlConnection, SqlTransaction);

            SqlTransaction.Rollback(); // leave the context untouched for the next test

            return this;
        }

        public SqlConnection GetNewSqlConnection()
        {
            return new(_instance.ConnectionString);
        }

        private void LogTiming(string message)
        {
            if (_logTiming is null)
                return;

            var now = DateTime.Now;
            var elapsed = now - _lastLogTime;
            _logTiming($"{message} after {Math.Round(elapsed.TotalSeconds, 2)}s");
            _lastLogTime = DateTime.Now;
        }

        public LocalDbTestContext DeployDacpac(string dacpacProjectName = "")
        {
            var dacPacInfo = new DacPacInfo(dacpacProjectName != string.Empty ? dacpacProjectName : _databaseName);

            if (!dacPacInfo.DacPacFound)
                throw new Exception($"Cant deploy dacpac, no project found with name {dacpacProjectName}");

            var svc = new DacServices(SqlConnection.ConnectionString);

            var dacOptions = new DacDeployOptions
            {
                CreateNewDatabase = true
            };

            svc.Deploy(
                DacPackage.Load(dacPacInfo.DacPacPath),
                _databaseName,
                true,
                dacOptions
            );

            LogTiming("dacpac deployed");

            return this;
        }

        public void TearDown()
        {
            LogTiming("tearing down");

            _manager.Stop();
            
            LogTiming("instance manager stopped");
            
            var tempInstanceDir =
                new DirectoryInfo(
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\{_instanceName}");

            tempInstanceDir.Delete(true);
            
            LogTiming("instance directory deleted");

            var elapsed = DateTime.Now - _instanceStartedTime;

            if(_logTiming is not null)
                _logTiming($"Test run completed after {Math.Round(elapsed.TotalSeconds, 2)}s");
        }        
    }
}
