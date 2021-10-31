using System;
using System.Data;
using System.IO;
using MartinCostello.SqlLocalDb;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;

namespace CSharpSqlTests
{
    public interface ILocalDbTestContext
    {
        LocalDbTestContext RunTest(Action<IDbConnection, IDbTransaction> useConnection);
        IDbConnection GetNewSqlConnection();

        /// <summary>
        /// Deploys the latest built dacpac, please not this method does not trigger a build, if you have changes to the dacpac, please manually build them. 
        /// </summary>
        /// <param name="dacpacProjectName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        LocalDbTestContext DeployDacpac(string dacpacProjectName = "");

        void TearDown();

        IDbConnection SqlConnection { get; }
        IDbTransaction? SqlTransaction { get; }
        object? LastQueryResult { get; set; }
    }

    public class LocalDbTestContext : ILocalDbTestContext
    {
        private readonly string _databaseName;
        private readonly Action<string>? _logTiming;
        private readonly SqlLocalDbApi _localDbApi;
        private TemporarySqlLocalDbInstance _instance;
        private ISqlLocalDbInstanceManager _manager;
        private string _instanceName;
        private DateTime _instanceStartedTime;
        private DateTime _lastLogTime;
        
        public IDbConnection SqlConnection { get; private set; }
        public IDbTransaction? SqlTransaction { get; private set; }
        public object? LastQueryResult { get; set; }

        public LocalDbTestContext(string databaseName, Action<string>? logTiming = null)
        {
            _databaseName = databaseName;
            _logTiming = logTiming;
        
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
            var dropCmd = SqlConnection.CreateCommand();
            dropCmd.CommandText = $"DROP DATABASE IF EXISTS {_databaseName}";
            dropCmd.CommandType = CommandType.Text;
            dropCmd.ExecuteNonQuery();

            var createDbCmd = SqlConnection.CreateCommand();
            createDbCmd.CommandText = $"CREATE DATABASE {_databaseName}";
            createDbCmd.CommandType = CommandType.Text;
            createDbCmd.ExecuteNonQuery();
            
            LogTiming("database connected");

            SqlConnection.ChangeDatabase(_databaseName);

            // ready to run tests in individual transactions
        }

        public LocalDbTestContext RunTest(Action<IDbConnection, IDbTransaction> useConnection)
        {
            try
            {
                SqlTransaction = SqlConnection.BeginTransaction();
                useConnection(SqlConnection, SqlTransaction);
            }            
            finally 
            {
                if(LastQueryResult is IDataReader lastQueryResultAsReader)
                    lastQueryResultAsReader.Close(); // close any open datareaders as they are against the connection and will stuff up other tests

                SqlTransaction?.Rollback(); // leave the context untouched for the next test
            }

            return this;
        }

        public IDbConnection GetNewSqlConnection()
        {
            return new SqlConnection(_instance.ConnectionString);
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

        /// <summary>
        /// Deploys the latest built dacpac, please not this method does not trigger a build, if you have changes to the dacpac, please manually build them. 
        /// </summary>
        /// <param name="dacpacProjectName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
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
