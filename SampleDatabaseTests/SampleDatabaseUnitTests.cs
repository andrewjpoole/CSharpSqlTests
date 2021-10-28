using System.Data;
using CSharpSqlTests;
using FluentAssertions;
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
            new LocalDbTestContext(DataBaseName, message => _testOutputHelper.WriteLine(message))
                .DeployDacpac()
                .RunTest((connection, transaction) =>
                {
                    var cmd = connection.CreateCommand();
                    cmd.CommandText = "spAddTwoNumbers";
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.AddParameterWithValue("@param1", 2);
                    cmd.AddParameterWithValue("@param2", 3);
                    cmd.Transaction = transaction;

                    var returnParameter = cmd.AddReturnParameter("@ReturnVal");

                    cmd.ExecuteNonQuery();
                    var result = returnParameter.Value;

                    result.Should().NotBeNull();
                    result.Should().Be(5);
                }).TearDown();
        }
    }

    public static class DatabaseExtensions
    {
        public static IDbDataParameter AddParameterWithValue(this IDbCommand cmd, string name, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            cmd.Parameters.Add(param);

            return param;
        }

        public static IDbDataParameter AddReturnParameter(this IDbCommand cmd, string name)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Direction = ParameterDirection.ReturnValue;
            cmd.Parameters.Add(param);

            return param;
        }
    }
}
