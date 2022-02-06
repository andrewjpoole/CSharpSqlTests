using System.Data;

namespace CSharpSqlTests
{
    /// <summary>
    /// Interface representing a database context
    /// </summary>
    public interface ISqlDatabaseContext 
    {
        /// <summary>
        /// A method which returns a new Connection to the temporary localDb instance
        /// </summary>
        /// <returns></returns>
        IDbConnection GetNewSqlConnection();
        /// <summary>
        /// The connection string for the database context
        /// </summary>
        string ConnectionString { get; }
        /// <summary>
        /// A method which tears down the context, depending on the type of the context e.g. existing databases don't get torn down.
        /// </summary>
        void TearDown();
        /// <summary>
        /// Method which creates a new database on the context using the specified name.
        /// </summary>
        void CreateNewDatabase(string databaseName);
    }
}