using System;
using System.Collections.Generic;
using System.Data;
// ReSharper disable InconsistentNaming

namespace CSharpSqlTests;

/// <summary>
/// Interface which abstracts the creation, management and interaction with localDb
/// </summary>
public interface IDbTestContext
{
    /// <summary>
    /// A method which runs the specified Action, passing the Connection and the Transaction, the Action should contain the code which performs the test.
    /// A new Transaction is created for the test and will be rolled back afterwards to ensure the tests cannot affect each other.
    /// </summary>
    DbTestContext RunTest(Action<IDbConnection, IDbTransaction> useConnection);

    /// <summary>
    /// A method which runs the specified Action, passing the Connection, the Action should contain the code which performs the test.
    /// No Transactions are used, so you can test repository code which creates its own transactions. You will need to tear down any data yourself to ensure the tests cannot affect each other.
    /// </summary>
    DbTestContext RunTest(Action<IDbConnection> useConnection);

    /// <summary>
    /// Deploys the latest built DacPac, please not this method does not trigger a build, if you have changes to the dacpac, please manually build them.
    /// The deployment will create the database if it doesn't exist.
    /// This method does not seem to work against sql running in containers.
    /// </summary>
    /// <param name="dacpacProjectName">Optional string which may contain the name of a DacPac project, if not supplied, the name of the database will be used.
    /// The surrounding directories which be searched for a DacPac project matching the name.
    /// Alternatively, an absolute path to a *.dacpac file can be specified instead.></param>
    /// <param name="maxSearchDepth">The number of directory levels to traverse up before searching for DacPac files,
    /// ideally this should be the number of directory levels between the running test exe and the solution file,
    /// it allows DacPac files to be found from different projects in your sln folder structure.
    /// If this is too high, too many directories will be searched which will take a long time.</param>
    /// <returns></returns>
    DbTestContext DeployDacpac(string dacpacProjectName = "", int maxSearchDepth = 4);
    /// <summary>
    /// A method which checks if LastQueryResult contains an open IDataReader and closes it.
    /// </summary>
    void CloseDataReaderIfOpen();
    /// <summary>
    /// A method which closes any open DataReader, Transaction and/or Connection, shuts down the temporary localDb instance and removes the residual files and folders.
    /// </summary>
    void TearDown();
    /// <summary>
    /// A method which returns a new Connection, note this will be connected to the Master database, connection.ChangeDatabase("databaseName") will need to be called.
    /// </summary>
    /// <returns></returns>
    IDbConnection GetNewSqlConnection();
    /// <summary>
    /// Closes and reopens the main db connection and changes to a database named directly here or the one specified in the constructor.
    /// </summary>
    /// <param name="namedDatabase">The name of the database to connect to</param>
    void OpenConnectionAndChangeToNamedDatabase(string? namedDatabase = null);
    /// <summary>
    /// A property which exposes a connection string to the database context.
    /// </summary>
    string ConnectionString { get; }
    /// <summary>
    /// A connection to the database context, public so that extension methods of the Given, When and Then can access it.
    /// Maybe null if called outside of a test, in which case use GetNewSqlConnection() instead.
    /// </summary>
    IDbConnection? SqlConnection { get; }
    /// <summary>
    /// An IdbTransaction property which will be assigned a new Transaction each time an individual test runs, 
    /// it will be rolled back after the test has run to ensure the tests cannot affect each other.
    /// This is public so that extension methods of the Given, When and Then can access it.
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
        
    /// <summary>
    /// Method which drops the database with the name specified in the constructor, if it exists.
    /// </summary>
    DbTestContext DropDatabaseIfExists();
        
    /// <summary>
    /// Method which creates a new database using the name specified in the constructor, the location of the files depends on the specified mode.
    /// </summary>
    DbTestContext CreateNewDatabase();
}