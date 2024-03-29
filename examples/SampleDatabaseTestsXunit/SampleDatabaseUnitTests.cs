using System.Data;
using CSharpSqlTests;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace SampleDatabaseTestsXunit;

public class SampleDatabaseUnitTests
{
    private readonly ITestOutputHelper _testOutputHelper;
    private const string DatabaseName = "SampleDb2";

    public SampleDatabaseUnitTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void Connection_can_be_used_to_deploy_dacpac_and_run_stored_procedure_against_temporary_localdb()
    {
        new DbTestContext(DatabaseName, 
                DbTestContextMode.TemporaryLocalDbInstance,  
                writeToOutput: message => _testOutputHelper.WriteLine(message))
            .DeployDacpac("SampleDb", maxSearchDepth:6)
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

    [Fact]
    public void Connection_can_be_used_to_deploy_dacpac_and_run_stored_procedure_against_existing_localdb()
    {
        new DbTestContext(DatabaseName, 
                DbTestContextMode.ExistingLocalDbInstanceViaInstanceName, 
                existingLocalDbInstanceName: "MSSQLLocalDB", 
                writeToOutput: message => _testOutputHelper.WriteLine(message))
            .DropDatabaseIfExists()
            .DeployDacpac("SampleDb", maxSearchDepth: 6)
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

    [Fact]
    public void Connection_can_be_used_to_deploy_dacpac_and_run_stored_procedure_against_sqlexpress()
    {
        new DbTestContext(DatabaseName, 
                DbTestContextMode.ExistingDatabaseViaConnectionString, 
                existingDatabaseConnectionString: @"Server=.\SQLExpress; Integrated Security=true; TrustServerCertificate=True;", 
                writeToOutput: message => _testOutputHelper.WriteLine(message))
            .DeployDacpac("SampleDb", maxSearchDepth: 6)
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