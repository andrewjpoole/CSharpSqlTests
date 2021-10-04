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
        private DacPacInfo _dacPacInfo;

        private SqlLocalDbApi _localDbApi;
        private TemporarySqlLocalDbInstance _instance;
        private string _instanceName;
        public SqlConnection SqlConnection;

        public object LastQueryResult;

        public readonly ITestOutputHelper TestOutputHelper;
        private Stopwatch _stopwatch;

        public LocalDbTestContext(string databaseName, string dacpacProjectName = "", ITestOutputHelper testOutputHelper = null)
        {
            _databaseName = databaseName;
            TestOutputHelper = testOutputHelper;
            _dacPacInfo = new DacPacInfo(dacpacProjectName != string.Empty ? dacpacProjectName : databaseName);
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

        private void LogTiming(string message)
        {
            TestOutputHelper?.WriteLine($"{message} {_stopwatch.ElapsedMilliseconds}ms");
            _stopwatch.Restart();
        }

        public void DeployDacpac(SqlConnection connection)
        {
            if (!_dacPacInfo.DacPacFound)
                throw new Exception($"Cant deploy dacpac, no project found with name {_dacPacInfo.DacPacProjectName}");

            var svc = new DacServices(connection.ConnectionString);

            var dacOptions = new DacDeployOptions
            {
                CreateNewDatabase = true
            };

            svc.Deploy(
                DacPackage.Load(_dacPacInfo.DacPacPath),
                _databaseName,
                true,
                dacOptions
            );

            LogTiming("dacpac deployed");
        }
    }
}
