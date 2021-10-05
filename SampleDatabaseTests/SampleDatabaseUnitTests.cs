using System.Data;
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
        public void Connection_can_be_used_to_deploy_dacpac_and_run_stored_procedure_from_it()
        {
            var context = new LocalDbTestContext(DataBaseName, _testOutputHelper);
            context.Start(connection =>
            {
                context.DeployDacpac();

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
    }
}
