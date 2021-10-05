using System;
using System.Diagnostics;
using System.IO;
using MartinCostello.SqlLocalDb;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;
using Xunit.Abstractions;

namespace CSharpSqlTests
{
    public class LocalDbTestContext
    {
        private string _databaseName;

        private SqlLocalDbApi _localDbApi;
        private TemporarySqlLocalDbInstance _instance;
        private string _instanceName;
        public SqlConnection SqlConnection;

        public object LastQueryResult;

        public readonly ITestOutputHelper TestOutputHelper;
        private Stopwatch _stopwatch;

        public LocalDbTestContext(string databaseName, ITestOutputHelper testOutputHelper = null)
        {
            _databaseName = databaseName;
            TestOutputHelper = testOutputHelper;
        }

        public void Start(Action<SqlConnection> useConnection)
        {
            _stopwatch = new Stopwatch();
            _stopwatch.Start();

            _localDbApi = new SqlLocalDbApi();
            _instance = _localDbApi.CreateTemporaryInstance();
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

            // Pass the connection to the test for use
            useConnection(SqlConnection);
            LogTiming("useConnection returned");

            // Tidy up
            SqlConnection.Dispose();
            _instance.Dispose();
            _localDbApi.Dispose();
            LogTiming("connection, instance and localDbApi objects disposed");

            var tempInstanceDir =
                new DirectoryInfo(
                    $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\{_instanceName}");

            tempInstanceDir.Delete(true);

            _stopwatch.Stop();
        }

        public SqlConnection GetSqlConnection()
        {
            return SqlConnection; // todo this should return a new connection
        }

        private void LogTiming(string message)
        {
            TestOutputHelper?.WriteLine($"{message} {_stopwatch.ElapsedMilliseconds}ms");
            _stopwatch.Restart();
        }

        public void DeployDacpac(string dacpacProjectName = "")
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
        }
    }

    public class LocalDbTestContext2
    {
        private string _databaseName;

        private SqlLocalDbApi _localDbApi;
        private TemporarySqlLocalDbInstance _instance;
        private ISqlLocalDbInstanceManager _manager;
        private string _instanceName;
        public SqlConnection SqlConnection;
        public SqlTransaction SqlTransaction;

        public object LastQueryResult;

        public readonly ITestOutputHelper TestOutputHelper;

        private DateTime _instanceStartedTime;
        private DateTime _lastLogTime;

        public LocalDbTestContext2(string databaseName, ITestOutputHelper testOutputHelper = null)
        {
            _databaseName = databaseName;
            TestOutputHelper = testOutputHelper;
        }

        public void Start()
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
            
        }

        public void RunTest(Action<SqlConnection, SqlTransaction> useConnection)
        {
            SqlTransaction = SqlConnection.BeginTransaction(DateTime.Now.Ticks.ToString());

            useConnection(SqlConnection, SqlTransaction);

            SqlTransaction.Rollback(); // leave the context untouched for the next test
        }

        public SqlConnection GetNewSqlConnection()
        {
            return new(_instance.ConnectionString);
        }

        private void LogTiming(string message)
        {
            var now = DateTime.Now;
            var elapsed = now - _lastLogTime;
            TestOutputHelper?.WriteLine($"{message} after {Math.Round(elapsed.TotalSeconds, 2)}s");
            _lastLogTime = DateTime.Now;
        }

        public void DeployDacpac(string dacpacProjectName = "")
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
            TestOutputHelper?.WriteLine($"Test run completed after {Math.Round(elapsed.TotalSeconds, 2)}s");
        }
    }
}
