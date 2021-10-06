using CSharpSqlTests;
using Xunit;
using Xunit.Abstractions;

namespace SampleDatabaseTests
{
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
                    .TheStoredProcedureIsExecuted("spFetchRecords", out var returnValue, ("@param1", 5), ("@param2", 12));

                Then.UsingThe(_context)
                    .TheLastQueryResultShouldBe(17);
            });
        }

        [Fact]
        public void helper_classes_can_be_used_to_insert_rows_from_markdown_table_and_query()
        {
            _context.RunTest((connection, transaction) =>
            {
                var tempData = @"
                | Id | Name   |
                | -- | ----   |
                | 1  | Andrew |
                | 2  | Jo     |";

                Given.UsingThe(_context, message => _testOutputHelper.WriteLine(message))
                    .And().TheFollowingDataExistsInTheTable("Table1", tempData);

                When.UsingThe(_context)
                    .TheQueryIsExecuted("SELECT * FROM Table1", out var table1Rows);

                Then.UsingThe(_context)
                    .TheQueryResultsShouldBe(tempData);

            });
        }
    }
}