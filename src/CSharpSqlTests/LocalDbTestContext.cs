using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using MartinCostello.SqlLocalDb;
using Microsoft.Data.SqlClient;
using Microsoft.SqlServer.Dac;

namespace CSharpSqlTests
{
    public interface ILocalDbTestContext
    {
        /// <summary>
        /// A method which runs the specified Action, passing the Connection and the Transaction, the Action should contain the code which performs the test.
        /// A new Transaction is created for the test and will be rolled back afterwards to ensure the tests cannot affect each other.
        /// </summary>
        LocalDbTestContext RunTest(Action<IDbConnection, IDbTransaction> useConnection);
        /// <summary>
        /// A method which returns a new Connection to the temporary localDb instance
        /// </summary>
        /// <returns></returns>
        IDbConnection GetNewSqlConnection();
        /// <summary>
        /// Deploys the latest built dacpac, please not this method does not trigger a build, if you have changes to the dacpac, please manually build them.
        /// </summary>
        /// <param name="dacpacProjectName">Optional string which may contain the name of a dacpac project, if not supplied, the name of the database will be used.
        /// The surrounding directories which be searched for a dacpac project matching the name.
        /// Alternatively, an absolute path to a *.dacpac file can be specified instead.
        /// <returns></returns>
        LocalDbTestContext DeployDacpac(string dacpacProjectName = "");
        /// <summary>
        /// A method which checks if LastQueryResult contains an open IDataReader and closes it.
        /// </summary>
        void CloseDataReaderIfOpen();
        /// <summary>
        /// A method which closes any open DataReader, Transaction and/or Connection, shuts down the temporary localDb instance and removes the residual files and folders.
        /// </summary>
        void TearDown();
        /// <summary>
        /// A connection to the temporary localDb instance
        /// </summary>
        IDbConnection SqlConnection { get; }
        /// <summary>
        /// An IdbTransaction property which will be assigned a new Transaction each time an individual test runs, 
        /// it will be rolled back after the test has run to ensure the tests cannot affect each other.
        /// </summary>
        IDbTransaction? SqlTransaction { get; }
        /// <summary>
        /// The Isolation level used when creating SqlTransactions for individual tests.
        /// Defaults to IsolationLevel.ReadUncommitted, which enables you to connect to the temporary localDb instance using SSMS etc
        /// and query data while debugging a test, however you must also specify the isolation level in SSMS SqlServer defaults to ReadCommitted
        /// <example>
        ///<code>
        ///SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
        ///BEGIN TRAN;
        ///
        /// -- run queries here...
        ///SELECT[Id]
        ///,[Name]
        ///,[Address]
        ///FROM[dbo].[Customers]
        /// </code>
        /// </example>
        /// Be sure to commit or rollback any transactions and disconnect from the temporary localDb instance, otherwise the test will not be able to clean after itself.
        /// </summary>
        IsolationLevel TransactionIsolationLevel { get; set; }
        /// <summary>
        /// This object will contain the result of Queries made against the connection, 
        /// for ExecuteReader() queries it will contain an IDataReader, 
        /// for ExecuteNonQuery() queries it will contain the number of rows affected,
        /// When using the When.TheStoredProcedureIsExecuted() it will contain the return parameter.
        /// </summary>
        object? LastQueryResult { get; set; }
        /// <summary>
        /// This Dictionary can be used to share state for a test between the Given, When and Then classes.
        /// </summary>
        Dictionary<string, object?> State { get; set; }
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

        private string _instancePath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\{_instanceName}";

        /// <inheritdoc />
        public IDbConnection SqlConnection { get; private set; }

        /// <inheritdoc />
        public IDbTransaction? SqlTransaction { get; private set; }
        
        /// <inheritdoc />
        public IsolationLevel TransactionIsolationLevel { get; set; } = IsolationLevel.ReadUncommitted;

        /// <inheritdoc />
        public object? LastQueryResult { get; set; }

        /// <inheritdoc />
        public Dictionary<string, object?> State { get; set; } = new();
        
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
            var createDbCmd = SqlConnection.CreateCommand();
            createDbCmd.CommandText = @$"CREATE DATABASE [{_databaseName}] ON  PRIMARY 
    ( NAME = N'{_databaseName}_Data', FILENAME = N'{_instancePath}\{_databaseName}.mdf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )
     LOG ON 
    ( NAME = N'{_databaseName}_Log', FILENAME = N'{_instancePath}\{_databaseName}.ldf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )";
            createDbCmd.CommandType = CommandType.Text;
            createDbCmd.ExecuteNonQuery();
            
            LogTiming("database connected");

            SqlConnection.ChangeDatabase(_databaseName);

            // ready to run tests in individual transactions
        }

        /// <inheritdoc />
        public LocalDbTestContext RunTest(Action<IDbConnection, IDbTransaction> useConnection)
        {
            try
            {
                SqlTransaction = SqlConnection.BeginTransaction(TransactionIsolationLevel);
                useConnection(SqlConnection, SqlTransaction);
            }            
            finally 
            {
                CloseDataReaderIfOpen();

                SqlTransaction?.Rollback(); // leave the context untouched for the next test
            }

            return this;
        }

        /// <inheritdoc />
        public void CloseDataReaderIfOpen()
        {
            if (LastQueryResult is IDataReader lastQueryResultAsReader)
                lastQueryResultAsReader.Close(); // close any open datareaders as they are against the connection and will stuff up other tests
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public void TearDown()
        {
            LogTiming("tearing down");

            _manager.Stop();
            
            LogTiming("instance manager stopped");
            
            var tempInstanceDir = new DirectoryInfo(_instancePath);

            tempInstanceDir.Delete(true);
            
            LogTiming("instance directory deleted");

            var elapsed = DateTime.Now - _instanceStartedTime;

            if(_logTiming is not null)
                _logTiming($"Test run completed after {Math.Round(elapsed.TotalSeconds, 2)}s");
        }        
    }
}