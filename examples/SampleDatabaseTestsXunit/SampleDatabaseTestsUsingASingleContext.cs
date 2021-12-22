using System.Linq;
using CSharpSqlTests;
using CSharpSqlTests.xUnit;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace SampleDatabaseTestsXunit
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

                Given.UsingThe(_context)
                    .And.TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheReaderQueryIsExecuted("SELECT * FROM Customers");

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
                    .And.TheFollowingDataExistsInTheTable("Orders", expectedOrder);

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
                    .And.TheFollowingDataExistsInTheTable("Orders", order);

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

                Given.UsingThe(_context)
                    .And.TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheReaderQueryIsExecuted("SELECT * FROM Customers");

                Then.UsingThe(_context)
                    .TheReaderQueryResultsShouldBe(tempData);

            });
        }

        [Fact]
        public void scalar_queries_can_be_used_for_assertion()
        {
            _context.RunTest((connection, transaction) =>
            {
                var tempData = TabularData
                    .CreateWithColumns("Id", "Name", "Address")
                    .AddRowWithValues(1, "Andrew", "emptyString")
                    .AddRowWithValues(2, "Jo", "null");

                Given.UsingThe(_context)
                    .And.TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheScalarQueryIsExecuted("DELETE FROM Customers WHERE Id = 1");

                Then.UsingThe(_context)
                    .TheScalarQueryIsExecuted("SELECT COUNT(*) FROM Customers", result => (int)result! == 1);

            });
        }

        [Fact]
        public void scalar_queries_can_be_used_for_assertion_using_out_var()
        {
            _context.RunTest((connection, transaction) =>
            {
                var tempData = TabularData
                    .CreateWithColumns("Id", "Name", "Address")
                    .AddRowWithValues(1, "Andrew", "emptyString")
                    .AddRowWithValues(2, "Jo", "null");

                Given.UsingThe(_context)
                    .And.TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheScalarQueryIsExecuted("DELETE FROM Customers WHERE Id = 1");

                Then.UsingThe(_context)
                    .TheScalarQueryIsExecuted("SELECT COUNT(*) FROM Customers", out var numberOfCustomers);

                numberOfCustomers.Should().Be(1);

            });
        }

        [Fact]
        public void reader_queries_can_be_used_for_assertion()
        {
            _context.RunTest((connection, transaction) =>
            {
                var tempData = TabularData
                    .CreateWithColumns("Id", "Name", "Address")
                    .AddRowWithValues(1, "Andrew", "emptyString")
                    .AddRowWithValues(2, "Jo", "null");

                Given.UsingThe(_context)
                    .And.TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheScalarQueryIsExecuted("DELETE FROM Customers WHERE Id = 1");

                Then.UsingThe(_context)
                    .TheReaderQueryIsExecuted("SELECT * FROM Customers", result => 
                        result.Contains(TabularData.CreateWithColumns("Id").AddRowWithValues(2), out _));

            });
        }

        [Fact]
        public void reader_queries_can_be_used_for_assertion_using_out_var()
        {
            _context.RunTest((connection, transaction) =>
            {
                var tempData = TabularData
                    .CreateWithColumns("Id", "Name", "Address")
                    .AddRowWithValues(1, "Andrew", "emptyString")
                    .AddRowWithValues(2, "Jo", "null");

                Given.UsingThe(_context)
                    .And.TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheScalarQueryIsExecuted("DELETE FROM Customers WHERE Id = 1");

                Then.UsingThe(_context)
                    .TheReaderQueryIsExecuted("SELECT * FROM Customers", out var result);

                result.Rows.Count.Should().Be(1);
                result.Rows.First().ColumnValues["Id"].Should().Be(2);

            });
        }
        
        [Fact]
        public void reader_queries_can_be_used_for_assertion_using_TheReaderQueryIsExecutedAndIsEqualTo()
        {
            _context.RunTest((connection, transaction) =>
            {
                var tempData = TabularData
                    .CreateWithColumns("Id", "Name", "Address")
                    .AddRowWithValues(1, "Andrew", "emptyString")
                    .AddRowWithValues(2, "Jo", "null");

                Given.UsingThe(_context)
                    .And.TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheScalarQueryIsExecuted("DELETE FROM Customers WHERE Id = 1");

                Then.UsingThe(_context)
                    .TheReaderQueryIsExecutedAndIsEqualTo("SELECT * FROM Customers", TabularData.CreateWithColumns("Id", "Name", "Address").AddRowWithValues(2, "Jo", "null").ToMarkdownTableString());

            });
        }

        [Fact]
        public void reader_queries_can_be_used_for_assertion_using_TheReaderQueryIsExecutedAndContains()
        {
            _context.RunTest((connection, transaction) =>
            {
                var tempData = TabularData
                    .CreateWithColumns("Id", "Name", "Address")
                    .AddRowWithValues(1, "Andrew", "emptyString")
                    .AddRowWithValues(2, "Jo", "null");

                Given.UsingThe(_context)
                    .And.TheFollowingDataExistsInTheTable("Customers", tempData);

                When.UsingThe(_context)
                    .TheScalarQueryIsExecuted("DELETE FROM Customers WHERE Id = 1");

                Then.UsingThe(_context)
                    .TheReaderQueryIsExecutedAndContains("SELECT * FROM Customers", TabularData.CreateWithColumns("Id", "Name").AddRowWithValues(2, "Jo").ToMarkdownTableString());

            });
        }
    }
}