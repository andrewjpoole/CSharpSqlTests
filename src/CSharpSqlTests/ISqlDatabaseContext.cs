using System.Data;
using System.Threading.Tasks;

namespace CSharpSqlTests
{
    public interface ISqlDatabaseContext 
    {
        /// <summary>
        /// A method which returns a new Connection to the temporary localDb instance
        /// </summary>
        /// <returns></returns>
        IDbConnection GetNewSqlConnection();
        void TearDown();
        void CreateNewDatabase(string databaseName);
    }
}