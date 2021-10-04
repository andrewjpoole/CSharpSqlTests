using System.Data;
using System.Threading.Tasks;
using CSharpSqlTests;
using Xunit;
using Xunit.Abstractions;

namespace SampleDatabaseTests
{
    public class SampleDatabaseUnitTests
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private const string DataBaseName = "DatabaseToTest";

        public SampleDatabaseUnitTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public async Task Connection_can_be_used_to_deploy_dacpac_and_run_stored_procedure_from_it()
        {
            var context = new LocalDbTestContext(DataBaseName, DataBaseName, _testOutputHelper);
            context.Start(connection =>
            {
                context.DeployDacpac(connection);

                var cmd = connection.CreateCommand();
                cmd.CommandText = "spFetchRecords";
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@param1", 2);
                cmd.Parameters.AddWithValue("@param2", 3);

                var returnParameter = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
                returnParameter.Direction = ParameterDirection.ReturnValue;

                cmd.ExecuteNonQuery();
                var result = (int) returnParameter.Value;

                Assert.True(result == 5);
            });
        }

        [Fact]
        public void helper_classes_can_be_used_to_deploy_dacpac_and_run_stored_procedure_from_it()
        {
            var context = new LocalDbTestContext(DataBaseName, DataBaseName, _testOutputHelper);
            context.Start(connection =>
            {
                Given.UsingThe(context)
                    .TheDacpacIsDeployed(connection);

                When.UsingThe(context)
                    .TheStoredProcedureIsExecuted("spFetchRecords", out var returnValue, ("@param1", 5), ("@param2", 12));

                Then.UsingThe(context)
                    .TheLastQueryResultShouldBe(17);
            });
        }

        [Fact]
        public void helper_classes_can_be_used_to_insert_rows_from_markdown_table_and_query()
        {
            var context = new LocalDbTestContext(DataBaseName, DataBaseName, _testOutputHelper);
            context.Start(connection =>
            {
                var tempData = @"
                | Id | Name |
                | -- | ---- |
                | 1  | Andrew |
                | 2  | Jo |";

                Given.UsingThe(context)
                    .TheDacpacIsDeployed(connection)
                    .And().TheFollowingDataExistsInTheTable("Table1", tempData);

                When.UsingThe(context)
                    .TheQueryIsExecuted("SELECT * FROM Table1", out var table1Rows);
                
                Then.UsingThe(context)
                    .TheQueryResultsShouldBe(tempData);

            });
        }
    }
}
