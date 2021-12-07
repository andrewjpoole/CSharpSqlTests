# CSharpSqlTests

A testing framework for sql related tests using a nice fluent C# api

A temporary localDb instance will be spun up, a dacpac will optionally be deployed into it and then tests can be executed each within their own SqlTransaction.

Given, When and Then helper classes are supplied:
- Given: used to seed test data, remove FK constraints etc
- When: used to run a stored procedure/reader query/scalar query etc whatever you are trying to test
- Then: a nice way to do assertions and/or check the result of a query from the When step

Test data can be expressed as markdown/specflow tables in the tests, which are easier to read than plain sql strings

Hopefully the following examples speak for themselves!

```CSharp
public class SampleDatabaseTestsUsingASingleContext : IClassFixture<LocalDbContextFixture>
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly LocalDbContextFixture _localDbContextFixture;
    private readonly LocalDbTestContext _context;

    public SampleDatabaseTestsUsingASingleContext(ITestOutputHelper testOutputHelper, LocalDbContextFixture localDbContextFixture)
    {
        _testOutputHelper = testOutputHelper;
        _localDbContextFixture = localDbContextFixture;
        _context = _localDbContextFixture.Context;
    }
        
    [Fact]
    public void helper_classes_can_be_used_to_deploy_dacpac_and_run_stored_procedure_from_it()
    {
        _context.RunTest((connection, transaction) =>
        {
            Given.UsingThe(_context);

            When.UsingThe(_context)
                .TheStoredProcedureIsExecuted("spAddTwoNumbers", out var returnValue, ("@param1", 5), ("@param2", 12));

            Then.UsingThe(_context)
                .TheLastQueryResultShouldBe(17);

            // Or
            // returnValue.Should().Be(17);
        });
    }

    [Fact]
    public void test_dropping_fk_constring_reduce_seeding_requirements() 
    {
        _context.RunTest((connection, transaction) => 
        {
            var expectedOrder = @"
                | Id | Customers_Id | DateCreated | DateFulfilled  | DatePaid | ProductName | Quantity | QuotedPrice | Notes       |
                | -- | ------------ | ----------- | -------------- | -------- | ----------- | -------- | ----------- | ----------- |
                | 23 | 1            | 2021/07/21  | 2021/08/02     | null     | Apples      | 21       | 5.29        | emptyString |";

            Given.UsingThe(_context)
            .TheFollowingSqlStatementIsExecuted("ALTER TABLE Orders DROP CONSTRAINT FK_Orders_Customers;")
            .And.TheFollowingDataExistsInTheTable("Orders", expectedOrder);

            When.UsingThe(_context)
            .TheStoredProcedureIsExecutedWithReader("spFetchOrderById", ("OrderId", 23));

            // Either assert on the whole result
            Then.UsingThe(_context)
            .TheReaderQueryResultsShouldBe(expectedOrder);

            // Or just assert on a subset
            Then.UsingThe(_context)
                .TheReaderQueryResultsShouldContain(@"| Id |
                                                      | -- |
                                                      | 23 |");

        });
    }    
}

// The above xunit tests use the following IClassFixture class, which enables the localdb instance to be spun up once, to be used by each test and afterwards torn down.
public class LocalDbContextFixture : IDisposable
{
    public LocalDbTestContext Context;

    public LocalDbContextFixture(IMessageSink sink)
    {
        Context = new LocalDbTestContext("SampleDb", log => sink.OnMessage(new DiagnosticMessage(log)));
        Context.DeployDacpac(); // If the DacPac name does not match the database name, pass the DacPac name in here, or an absolute path to the file.
    }       

    public void Dispose()
    {
        Context.TearDown(); // this closes connections and tidies up the temporary localDb instance
    }
}

// Or to run tests without using the IClassFixture use the following:
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
```

This is mainly written to be an improvement in user friendliness over some of the t-SQL based test frameworks available

Feel free to contribute

license is MIT

enjoy :)
