using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.SqlServer.Dac;
// ReSharper disable InconsistentNaming

namespace CSharpSqlTests
{
    /// <summary>
    /// Class which abstracts the creation, management and interaction with localDb
    /// </summary>
    public class DbTestContext : IDbTestContext
    {
        private ISqlDatabaseContext _context;
        private readonly string _databaseName;
        private readonly Action<string>? _logTiming;
        private readonly DateTime _testRunStartedTime;
        private IDataReader? _dataReader;
        
        /// <inheritdoc />
        public IDbConnection? SqlConnection { get; private set; }

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
        /// Constructs a DbTestContext, which will create a temporary localDb instance,
        /// unless runUsingNormalLocalDbInstanceNamed is set or an environment variable named CSharpSqlTests_RunUsingNormalLocalDbInstance is found, where the value is the name of a normal localDb instance to use.
        /// Default localDb instances are named 'MSSQLLocalDB' or 'ProjectsV13' i.e. Server=(localdb)\MSSQLLocalDB;Integrated Security=true
        /// </summary>
        /// <param name="databaseName">A string containing the name of the Database</param>
        /// <param name="mode">The mode, which specifies what db context should be used, options are ExistingDatabaseViaConnectionString, TemporaryLocalDbInstance or ExistingLocalDbInstanceViaInstanceName</param>
        /// <param name="logTiming">An action to log messages.</param>
        /// <param name="existingLocalDbInstanceName">A string containing the name of a locabDb instance to use if mode is ExistingLocalDbInstanceViaInstanceName.</param>
        /// <param name="existingDatabaseConnectionString">A string containing the connection string to an existing db to use if mode is ExistingDatabaseViaConnectionString</param>
        public DbTestContext(
            string databaseName, 
            DbTestContextMode mode,
            Action<string>? logTiming = null, 
            string existingDatabaseConnectionString = "",
            string existingLocalDbInstanceName = "")
        {
            _databaseName = databaseName;
            _logTiming = logTiming;
            _testRunStartedTime = DateTime.Now;

            var (overridenLocalDbInstanceName, overridenConnectionString) = CheckForEnvironmentVariableModeOverride();

            if (!string.IsNullOrEmpty(overridenConnectionString))
                _context = new ExistingDbViaConnectionStringContext(overridenConnectionString);
            else if (!string.IsNullOrEmpty(overridenLocalDbInstanceName))
                _context = new ExistingLocalDbViaInstanceNameContext(overridenLocalDbInstanceName);
            else
            {
                _context = mode switch
                {
                    DbTestContextMode.TemporaryLocalDbInstance => new TemporaryLocalDbContext(Log),
                    DbTestContextMode.ExistingLocalDbInstanceViaInstanceName => new ExistingLocalDbViaInstanceNameContext(existingLocalDbInstanceName, Log),
                    DbTestContextMode.ExistingDatabaseViaConnectionString => new ExistingDbViaConnectionStringContext(existingDatabaseConnectionString),
                    _ => throw new NotImplementedException()
                };
            }

            LogTiming("DbTestContext constructor completed");
        }
        
        /// <inheritdoc />
        public DbTestContext RunTest(Action<IDbConnection, IDbTransaction> useConnection)
        {
            LogTiming("RunTest() method called");
            CloseDataReaderIfOpen();

            SqlConnection?.Close();
            SqlConnection?.Dispose();
            SqlConnection = _context.GetNewSqlConnection();
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
            return _context.GetNewSqlConnection();
        }

        /// <inheritdoc />
        public DbTestContext DeployDacpac(string dacpacProjectName = "", int maxSearchDepth = 4)
        {
            var dacPacInfo = new DacPacInfo(dacpacProjectName != string.Empty ? dacpacProjectName : _databaseName, maxSearchDepth, _logTiming);

            if (!dacPacInfo.DacPacFound)
                throw new Exception($"Cant deploy dacpac, no project found with name {dacpacProjectName}");

            Log($"Deploying {dacPacInfo.DacPacPath}");

            SqlConnection?.Close();

            var svc = new DacServices(_context.ConnectionString);

            var dacOptions = new DacDeployOptions
            {
                CreateNewDatabase = true,
                AllowIncompatiblePlatform = true,
                
            };

            svc.Deploy(
                DacPackage.Load(dacPacInfo.DacPacPath),
                _databaseName,
                true,
                dacOptions
            );

            LogTiming("DacPac deployed");

            return this;
        }

        /// <inheritdoc />
        public void TearDown()
        {
            LogTiming("Tearing down");

            _context.TearDown();

            LogTiming($"Test run completed");
        }

        private (string ExistingLocalDbInstanceName, string ExistingDatabaseConnectionString) CheckForEnvironmentVariableModeOverride()
        {
            var envMode = Environment.GetEnvironmentVariable("CSharpSqlTests_Mode");
            var envConnectionString = Environment.GetEnvironmentVariable("CSharpSqlTests_ConnectionString");
            var envExistingLocalDbInstanceName = Environment.GetEnvironmentVariable("CSharpSqlTests_ExistingLocalDbInstanceName");
            if (envMode is null)
                return (string.Empty, string.Empty);
            
            if (envMode == DbTestContextMode.ExistingDatabaseViaConnectionString.ToString())
            {
                if (envConnectionString is null)
                    throw new ApplicationException(
                        $"Found environment variable named CSharpSqlTests_Mode which overrides the mode BUT an additional environment variable named CSharpSqlTests_ConnectionString is required to define the connection string");
                
                Log($"Found environment variable named CSharpSqlTests_Mode which overrides the mode to {envMode} and CSharpSqlTests_ConnectionString which sets the connection string to {envConnectionString}");
                return (string.Empty, envConnectionString);

            }

            if (envMode == DbTestContextMode.ExistingLocalDbInstanceViaInstanceName.ToString())
            {
                if (envConnectionString is null)
                    throw new ApplicationException(
                        $"Found environment variable named CSharpSqlTests_Mode which overrides the mode BUT an additional environment variable named CSharpSqlTests_ExistingLocalDbInstanceName is required to specify an existing localDb instance name");
                
                Log($"Found environment variable named CSharpSqlTests_Mode which overrides the mode to {envMode} and CSharpSqlTests_ExistingLocalDbInstanceName which will attempt to use an existing localDb instance name of {envExistingLocalDbInstanceName}");
                return (envExistingLocalDbInstanceName, string.Empty);

            }

            throw new ApplicationException($"Found environment variable named CSharpSqlTests_Mode BUT value of {envMode} is not valid;\nValid values are 'ExistingDatabaseViaConnectionString' and 'ExistingLocalDbInstanceViaInstanceName'");
        }

        /// <inheritdoc />
        public DbTestContext DropDatabaseIfExists()
        {
            using var connection = _context.GetNewSqlConnection();
            connection.Open();
            var dropDbCmd = connection.CreateCommand();
            dropDbCmd.CommandText = @$"
USE [master] 
IF EXISTS(SELECT * FROM sys.databases WHERE name = '{_databaseName}')
BEGIN
    ALTER DATABASE [{_databaseName}] SET single_user WITH ROLLBACK IMMEDIATE
    DROP DATABASE IF EXISTS [{_databaseName}]
END";
            dropDbCmd.CommandType = CommandType.Text;
            dropDbCmd.ExecuteNonQuery();
            connection.Close();

            return this;
        }

        /// <inheritdoc />
        public DbTestContext CreateNewDatabase()
        {
            _context.CreateNewDatabase(_databaseName);

            return this;
        }

        private void LogTiming(string message)
        {
            if (_logTiming is null)
                return;

            var now = DateTime.Now;
            var elapsedTotal = now - _testRunStartedTime;
            _logTiming($"{Math.Round(elapsedTotal.TotalSeconds, 2)}s {message}");
        }

        private void Log(string message)
        {
            _logTiming?.Invoke($"{message}");
        }
    }
}