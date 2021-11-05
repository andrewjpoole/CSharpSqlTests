using CSharpSqlTests;
using Xunit;
using Xunit.Abstractions;

namespace SampleDatabaseTests
{
    public class SampleDatabaseTestsUsingASingleContext : IClassFixture<LocalDbContextFixture>
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly LocalDbTestContext _context;

        public SampleDatabaseTestsUsingASingleContext(ITestOutputHelper testOutputHelper, LocalDbContextFixture localDbContextFixture)
        {
            _testOutputHelper = testOutputHelper;
            _context = localDbContextFixture.Context;
        }
        
        [Fact]
        public void helper_classes_can_be_used_to_deploy_dacpac_and_run_stored_procedure_from_it()
        {
            _context.RunTest((connection, transaction) =>
            {
                Given.UsingThe(_context);

                When.UsingThe(_context)
                    .TheStoredProcedureIsExecutedWithReturnParameter("spAddTwoNumbers", out var returnValue, ("@param1", 5), ("@param2", 12));

                Then.UsingThe(_context)
                    .TheNonReaderQueryResultShouldBe(17);
            });
        }

        [Fact]
        public void helper_classes_can_be_used_to_insert_rows_from_markdown_table_and_query()
        {
            _context.RunTest((connection, transaction) =>
            {
                var tempData = @"
                | Id | Name   | Address     |
                | -- | ------ | ----------- |
                | 1  | Andrew | emptyString |
                | 2  | Jo     | null        |";

                Given.UsingThe(_context, message => _testOutputHelper.WriteLine(message))
                    .And().TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheReaderQueryIsExecuted("SELECT * FROM Customers", out var table1Rows);

                Then.UsingThe(_context)
                    .TheReaderQueryResultsShouldBe(tempData);

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
                .And().TheFollowingDataExistsInTheTable("Orders", expectedOrder);

                When.UsingThe(_context)
                .TheStoredProcedureIsExecutedWithReader("spFetchOrderById", ("OrderId", 23));

                Then.UsingThe(_context)
                .TheReaderQueryResultsShouldBe(expectedOrder);

            });
        }

        [Fact]
        public void test_asserting_using_contains()
        {
            _context.RunTest((connection, transaction) =>
            {
                var order = @"
                    | Id | Customers_Id | DateCreated | DateFulfilled  | DatePaid | ProductName | Quantity | QuotedPrice | Notes       |
                    | -- | ------------ | ----------- | -------------- | -------- | ----------- | -------- | ----------- | ----------- |
                    | 23 | 1            | 2021/07/21  | 2021/08/02     | null     | Apples      | 21       | 5.29        | emptyString |";

                Given.UsingThe(_context)
                    .TheFollowingSqlStatementIsExecuted("ALTER TABLE Orders DROP CONSTRAINT FK_Orders_Customers;")
                    .And().TheFollowingDataExistsInTheTable("Orders", order);

                When.UsingThe(_context)
                    .TheStoredProcedureIsExecutedWithReader("spFetchOrderById", ("OrderId", 23));

                Then.UsingThe(_context)
                    .TheReaderQueryResultsShouldContain(@"| Id |
                                                          | -- |
                                                          | 23 |");

            });
        }

        [Fact]
        public void tabular_data_can_be_specified_using_fluent_api()
        {
            _context.RunTest((connection, transaction) =>
            {
                var tempData = TabularData
                    .CreateWithColumns("Id", "Name", "Address")
                    .AddRowWithValues(1, "Andrew", "emptyString")
                    .AddRowWithValues(2, "Jo", "null");

                Given.UsingThe(_context, message => _testOutputHelper.WriteLine(message))
                    .And().TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheReaderQueryIsExecuted("SELECT * FROM Customers", out var table1Rows);

                Then.UsingThe(_context)
                    .TheReaderQueryResultsShouldBe(tempData);

            });
        }
    }
}