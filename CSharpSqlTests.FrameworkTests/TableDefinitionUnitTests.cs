using CSharpSqlTests;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace SampleDatabaseTests
{
    public class TableDefinitionUnitTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TableDefinitionUnitTests(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
        }

        [Fact]
        public void tableData_FromString_method_returns_a_tableData()
        {
            var testString = @"
| column1     | column2 |
| ----------- | ------- |
| valueA      | valueB  |
| valueC      | valueD  |
| emptyString | null    |";

            var tableDefinition = TableDefinition.FromMarkdownTableString(testString);

            tableDefinition.Columns.Count.Should().Be(2);
            tableDefinition.Columns[0].Should().Be("column1");
            tableDefinition.Columns[1].Should().Be("column2");
            tableDefinition.Rows.Count.Should().Be(3);
            tableDefinition.Rows[0].ColumnValues[0].Should().Be("valueA");
            tableDefinition.Rows[0].ColumnValues[1].Should().Be("valueB");
            tableDefinition.Rows[1].ColumnValues[0].Should().Be("valueC");
            tableDefinition.Rows[1].ColumnValues[1].Should().Be("valueD");
            tableDefinition.Rows[2].ColumnValues[0].Should().Be(string.Empty);
            tableDefinition.Rows[2].ColumnValues[1].Should().Be(null);
        }

        [Fact]
        public void tableData_FromString_method_returns_a_tableData_when_string_is_indented()
        {
            var testString = @" | column1 | column2 |
                                | --- | --- |
                                | valueA | valueB |
                                | valueC | valueD |";

            var tableDefinition = TableDefinition.FromMarkdownTableString(testString);

            tableDefinition.Rows.Count.Should().Be(2);
            tableDefinition.Columns.Count.Should().Be(2);
            tableDefinition.Columns[0].Should().Be("column1");
            tableDefinition.Columns[1].Should().Be("column2");
            tableDefinition.Rows.Count.Should().Be(2);
            tableDefinition.Rows[0].ColumnValues[0].Should().Be("valueA");
            tableDefinition.Rows[0].ColumnValues[1].Should().Be("valueB");
            tableDefinition.Rows[1].ColumnValues[0].Should().Be("valueC");
            tableDefinition.Rows[1].ColumnValues[1].Should().Be("valueD");
        }

        [Fact]
        public void tableData_roundtrip_works()
        {
            var testString = @"
| column1 | column2 |
| ------- | ------- |
| valueA  | valueB  |
| valueC  | valueD  |";

            var tableDefinition = TableDefinition.FromMarkdownTableString(testString);

            var roundTrip = tableDefinition.ToMarkdownTableString();

            _testOutputHelper.WriteLine(roundTrip);
            _testOutputHelper.WriteLine("");

            tableDefinition.IsEqualTo(roundTrip).Should().Be(true);
        }

        [Fact]
        public void tableData_toSqlString_method_produces_valid_sql_string()
        {
            var testString = @"
| column1 | column2 |
| ------- | ------- |
| valueA  | 12  |
| valueC  | 13  |";

            var tableDefinition = TableDefinition.FromMarkdownTableString(testString);

            var sqlInsert = tableDefinition.ToSqlString("Orders");

            sqlInsert.Should().Be(@"INSERT INTO Orders
(column1,column2)
VALUES
('valueA',12)
,('valueC',13)
");
        }

        [Fact]
        public void tableData_From_fluent_api_method_returns_a_tableData()
        {
            var tableDefinition = TableDefinition
                .CreateWithColumns("column1", "column2")
                .AddRowWithValues("valueA", "valueB")
                .AddRowWithValues("valueC", "valueD");

            tableDefinition.Rows.Count.Should().Be(2);
            tableDefinition.Columns.Count.Should().Be(2);
            tableDefinition.Columns[0].Should().Be("column1");
            tableDefinition.Columns[1].Should().Be("column2");
            tableDefinition.Rows.Count.Should().Be(2);
            tableDefinition.Rows[0].ColumnValues[0].Should().Be("valueA");
            tableDefinition.Rows[0].ColumnValues[1].Should().Be("valueB");
            tableDefinition.Rows[1].ColumnValues[0].Should().Be("valueC");
            tableDefinition.Rows[1].ColumnValues[1].Should().Be("valueD");
        }
    }
}