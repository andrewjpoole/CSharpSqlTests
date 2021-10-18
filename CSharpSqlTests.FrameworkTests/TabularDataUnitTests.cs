using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace CSharpSqlTests.FrameworkTests
{
    public class TabularDataUnitTests
    {
        private readonly ITestOutputHelper _testOutputHelper;

        public TabularDataUnitTests(ITestOutputHelper testOutputHelper)
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

            var tabularData = TabularData.FromMarkdownTableString(testString);

            tabularData.Columns.Count.Should().Be(2);
            tabularData.Columns[0].Should().Be("column1");
            tabularData.Columns[1].Should().Be("column2");
            tabularData.Rows.Count.Should().Be(3);
            tabularData.Rows[0].ColumnValues[0].Should().Be("valueA");
            tabularData.Rows[0].ColumnValues[1].Should().Be("valueB");
            tabularData.Rows[1].ColumnValues[0].Should().Be("valueC");
            tabularData.Rows[1].ColumnValues[1].Should().Be("valueD");
            tabularData.Rows[2].ColumnValues[0].Should().Be(string.Empty);
            tabularData.Rows[2].ColumnValues[1].Should().Be(null);
        }

        [Fact]
        public void tableData_FromString_method_returns_a_tableData_when_string_is_indented()
        {
            var testString = @" | column1 | column2 |
                                | --- | --- |
                                | valueA | valueB |
                                | valueC | valueD |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            tabularData.Rows.Count.Should().Be(2);
            tabularData.Columns.Count.Should().Be(2);
            tabularData.Columns[0].Should().Be("column1");
            tabularData.Columns[1].Should().Be("column2");
            tabularData.Rows.Count.Should().Be(2);
            tabularData.Rows[0].ColumnValues[0].Should().Be("valueA");
            tabularData.Rows[0].ColumnValues[1].Should().Be("valueB");
            tabularData.Rows[1].ColumnValues[0].Should().Be("valueC");
            tabularData.Rows[1].ColumnValues[1].Should().Be("valueD");
        }

        [Fact]
        public void tableData_roundtrip_works()
        {
            var testString = @"
| column1 | column2 |
| ------- | ------- |
| valueA  | valueB  |
| valueC  | valueD  |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            var roundTripString = tabularData.ToMarkdownTableString();

            var roundTrippedTabularData = TabularData.FromMarkdownTableString(roundTripString);
            
            tabularData.IsEqualTo(roundTrippedTabularData, out var differences)
                .Should().Be(true, $"TabularData objects are not equal:\n{string.Join("\n", differences)}");
        }

        [Fact]
        public void tableData_toSqlString_method_produces_valid_sql_string()
        {
            var testString = @"
| column1 | column2 |
| ------- | ------- |
| valueA  | 12  |
| valueC  | 13  |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            var sqlInsert = tabularData.ToSqlString("Orders");

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
            var tabularData = TabularData
                .CreateWithColumns("column1", "column2")
                .AddRowWithValues("valueA", "valueB")
                .AddRowWithValues("valueC", "valueD");

            tabularData.Rows.Count.Should().Be(2);
            tabularData.Columns.Count.Should().Be(2);
            tabularData.Columns[0].Should().Be("column1");
            tabularData.Columns[1].Should().Be("column2");
            tabularData.Rows.Count.Should().Be(2);
            tabularData.Rows[0].ColumnValues[0].Should().Be("valueA");
            tabularData.Rows[0].ColumnValues[1].Should().Be("valueB");
            tabularData.Rows[1].ColumnValues[0].Should().Be("valueC");
            tabularData.Rows[1].ColumnValues[1].Should().Be("valueD");
        }
    }
}