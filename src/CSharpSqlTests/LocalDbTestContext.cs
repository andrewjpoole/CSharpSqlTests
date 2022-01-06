using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using MartinCostello.SqlLocalDb;
using Microsoft.SqlServer.Dac;
// ReSharper disable InconsistentNaming

namespace CSharpSqlTests
{
    /// <summary>
    /// Interface which abstracts the creation, management and interaction with localDb
    /// </summary>
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
        /// Deploys the latest built DacPac, please not this method does not trigger a build, if you have changes to the dacpac, please manually build them.
        /// </summary>
        /// <param name="dacpacProjectName">Optional string which may contain the name of a DacPac project, if not supplied, the name of the database will be used.
        /// The surrounding directories which be searched for a DacPac project matching the name.
        /// Alternatively, an absolute path to a *.dacpac file can be specified instead.></param>
        /// <param name="maxSearchDepth">The number of directory levels to traverse up before searching for DacPac files,
        /// ideally this should be the number of directory levels between the running test exe and the solution file,
        /// it allows DacPac files to be found from different projects in your sln folder structure.
        /// If this is too high, too many directories will be searched which will take a long time.</param>
        /// <returns></returns>
        LocalDbTestContext DeployDacpac(string dacpacProjectName = "", int maxSearchDepth = 4);
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
        /// This object will contain the result of non DataReader queries made against the connection,
        /// for ExecuteNonQuery() queries it will contain the number of rows affected,
        /// When using the When.TheStoredProcedureIsExecuted() it will contain the return parameter.
        /// </summary>
        object? LastNonReaderQueryResult { get; set; }

        /// <summary>
        /// This object will contain the result of DataReader queries made against the connection
        /// It is basically the state shared between the Given, When and Then helper methods.
        /// </summary>
        IDataReader? CurrentDataReader { get; set; }

        /// <summary>
        /// This Dictionary can be used to share state for a test between the Given, When and Then classes.
        /// </summary>
        Dictionary<string, object?> State { get; set; }
    }

    /// <summary>
    /// Class which abstracts the creation, management and interaction with localDb
    /// </summary>
    public class LocalDbTestContext : ILocalDbTestContext
    {
        private readonly string _databaseName;
        private readonly Action<string>? _logTiming;
        private readonly bool _runUsingTemporaryLocalDbInstance;
        private readonly ISqlLocalDbInstanceManager _manager;
        private readonly string _instanceName;
        private readonly DateTime _instanceStartedTime;
        private DateTime _lastLogTime;
        private const string RunUsingNormalLocalDbInstanceEnvironmentVariableName = "CSharpSqlTests_RunUsingNormalLocalDbInstance";
        private IDataReader? _dataReader;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable, reference required when using temporary localDb instance
        private readonly TemporarySqlLocalDbInstance? _temporaryInstance;

        private string _instancePath =>
            $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\{_instanceName}";

        /// <inheritdoc />
        public IDbConnection SqlConnection { get; private set; }

        /// <inheritdoc />
        public IDbTransaction? SqlTransaction { get; private set; }
        
        /// <inheritdoc />
        public IsolationLevel TransactionIsolationLevel { get; set; } = IsolationLevel.ReadUncommitted;

        /// <inheritdoc />
        public object? LastNonReaderQueryResult { get; set; }

        /// <inheritdoc />
        public IDataReader? CurrentDataReader
        {
            get => _dataReader;
            set
            {
                if(_dataReader is not null &&  !_dataReader.IsClosed)
                    _dataReader.Close();

                _dataReader = value;
            }
        }

        /// <inheritdoc />
        public Dictionary<string, object?> State { get; set; } = new();

        /// <summary>
        /// Constructs a LocalDbTestContext, which will create a temporary localDb instance,
        /// unless runUsingNormalLocalDbInstanceNamed is set or an environment variable named CSharpSqlTests_RunUsingNormalLocalDbInstance is found, where the value is the name of a normal localDb instance to use.
        /// Default localDb instances are named 'MSSQLLocalDB' or 'ProjectsV13' i.e. Server=(localdb)\MSSQLLocalDB;Integrated Security=true
        /// </summary>
        /// <param name="databaseName">A string containing the name of the Database</param>
        /// <param name="logTiming">An action to log messages.</param>
        /// <param name="runUsingNormalLocalDbInstanceNamed">A string containing the name of a locabDb instance to use, if empty a  temporary instance will be created.</param>
        public LocalDbTestContext(string databaseName, Action<string>? logTiming = null, string runUsingNormalLocalDbInstanceNamed = "")
        {
            _databaseName = databaseName;
            _logTiming = logTiming;

            var (runUsingTemporaryInstance, normalLocalDbInstanceName) = CheckForRunUsingNormalLocalDbInstanceEnvironmentVariable(runUsingNormalLocalDbInstanceNamed);
            if (runUsingTemporaryInstance)
            {
                _runUsingTemporaryLocalDbInstance = true;
                runUsingNormalLocalDbInstanceNamed = normalLocalDbInstanceName;
            }

            _lastLogTime = DateTime.Now;

            var localDbApi = new SqlLocalDbApi();
            
            if (_runUsingTemporaryLocalDbInstance)
            {
                _temporaryInstance = localDbApi.CreateTemporaryInstance();
                _manager = _temporaryInstance.Manage();
                
                _instanceStartedTime = DateTime.Now;
                LogTiming($"temporary localDb instance created named {_temporaryInstance.Name}");

                _instanceName = _temporaryInstance.Name;
            }
            else
            {
                var normalInstance = localDbApi.GetOrCreateInstance(runUsingNormalLocalDbInstanceNamed);
                _manager = normalInstance.Manage();
                _manager.Start();

                _instanceName = normalInstance.Name;

                LogTiming($"Using normal persistent localDb instance named {runUsingNormalLocalDbInstanceNamed}, if the database already exists in this instance it will be overwritten.");
            }

            // Open connection (to Master database)
            SqlConnection = GetNewSqlConnection();
            SqlConnection.Open();
            LogTiming("connection opened");

            if(!_runUsingTemporaryLocalDbInstance)
                DropDatabaseIfExists();

            // Create temp database
            var createDbCmd = SqlConnection.CreateCommand();
            createDbCmd.CommandText = @$"CREATE DATABASE [{_databaseName}] ON  PRIMARY 
    ( NAME = N'{_databaseName}_Data', FILENAME = N'{_instancePath}\{_databaseName}.mdf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )
     LOG ON 
    ( NAME = N'{_databaseName}_Log', FILENAME = N'{_instancePath}\{_databaseName}.ldf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )";
            createDbCmd.CommandType = CommandType.Text;
            createDbCmd.ExecuteNonQuery();
            
            LogTiming("database connected");
            
            // ready to run tests in individual transactions
        }
        
        /// <inheritdoc />
        public LocalDbTestContext RunTest(Action<IDbConnection, IDbTransaction> useConnection)
        {
            CloseDataReaderIfOpen();

            SqlConnection.Close();
            SqlConnection.Dispose();
            SqlConnection = GetNewSqlConnection();
            SqlConnection.Open();
            SqlConnection.ChangeDatabase(_databaseName);

            try
            {
                SqlTransaction = SqlConnection.BeginTransaction(TransactionIsolationLevel);
                useConnection(SqlConnection, SqlTransaction);
            }            
            finally 
            {
                CloseDataReaderIfOpen();

                SqlTransaction?.Rollback(); // leave the context untouched for the next test
                SqlConnection.Close();
                SqlConnection.Dispose();
            }

            return this;
        }

        /// <inheritdoc />
        public void CloseDataReaderIfOpen()
        {
            if (_dataReader is not null && !_dataReader.IsClosed)
                _dataReader.Close(); // ensure datareader is closed, otherwise transaction.RollBack will throw
        }

        /// <inheritdoc />
        public IDbConnection GetNewSqlConnection()
        {
            return _manager.CreateConnection();
        }

        /// <inheritdoc />
        public LocalDbTestContext DeployDacpac(string dacpacProjectName = "", int maxSearchDepth = 4)
        {
            var dacPacInfo = new DacPacInfo(dacpacProjectName != string.Empty ? dacpacProjectName : _databaseName, maxSearchDepth, _logTiming);

            if (!dacPacInfo.DacPacFound)
                throw new Exception($"Cant deploy dacpac, no project found with name {dacpacProjectName}");

            LogTiming($"Deploying {dacPacInfo.DacPacPath}");

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

            if (_runUsingTemporaryLocalDbInstance)
            {
                LogTiming("instance manager stopped");

                var tempInstanceDir = new DirectoryInfo(_instancePath);

                tempInstanceDir.Delete(true);

                LogTiming("instance directory deleted");
            }

            var elapsed = DateTime.Now - _instanceStartedTime;

            if(_logTiming is not null)
                _logTiming($"Test run completed after {Math.Round(elapsed.TotalSeconds, 2)}s");
        }

        private (bool RunUsingTemporaryInstance, string NormalLocalDbInstanceName) CheckForRunUsingNormalLocalDbInstanceEnvironmentVariable(string runUsingNormalLocalDbInstanceNamed)
        {
            var runUsingTemporaryInstance = true;
            var normalLocalDbInstanceName = "";

            if (!string.IsNullOrEmpty(runUsingNormalLocalDbInstanceNamed))
            {
                runUsingTemporaryInstance = false;
                normalLocalDbInstanceName = runUsingNormalLocalDbInstanceNamed;
            }

            // Environment variable may override parameter
            var envVar = Environment.GetEnvironmentVariable(RunUsingNormalLocalDbInstanceEnvironmentVariableName);
            if (envVar != null)
            {
                runUsingTemporaryInstance = false;
                normalLocalDbInstanceName = envVar;
            }

            return (runUsingTemporaryInstance, normalLocalDbInstanceName);
        }

        private void DropDatabaseIfExists()
        {
            var dropDbCmd = SqlConnection.CreateCommand();
            dropDbCmd.CommandText = @$"
USE [master] 
IF EXISTS(SELECT * FROM sys.databases WHERE name = '{_databaseName}')
BEGIN
    ALTER DATABASE [{_databaseName}] SET single_user WITH ROLLBACK IMMEDIATE
    DROP DATABASE IF EXISTS [{_databaseName}]
END";
            dropDbCmd.CommandType = CommandType.Text;
            dropDbCmd.ExecuteNonQuery();
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
    }
}