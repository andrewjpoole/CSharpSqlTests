using System;
using System.Text;
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
            tabularData.Rows[0].ColumnValues["column1"].Should().Be("valueA");
            tabularData.Rows[0].ColumnValues["column2"].Should().Be("valueB");
            tabularData.Rows[1].ColumnValues["column1"].Should().Be("valueC");
            tabularData.Rows[1].ColumnValues["column2"].Should().Be("valueD");
            tabularData.Rows[2].ColumnValues["column1"].Should().Be(string.Empty);
            tabularData.Rows[2].ColumnValues["column2"].Should().Be(null);
        }

        [Fact]
        public void tableData_FromString_method_handles_quote_numeric_strings()
        {
            var testString = @"
| column1     | column2 |
| ----------- | ------- |
| 123         | ""2""   |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            tabularData.Rows[0].ColumnValues["column1"].Should().Be(123);
            tabularData.Rows[0].ColumnValues["column2"].Should().Be("2");
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
            tabularData.Rows[0].ColumnValues["column1"].Should().Be("valueA");
            tabularData.Rows[0].ColumnValues["column2"].Should().Be("valueB");
            tabularData.Rows[1].ColumnValues["column1"].Should().Be("valueC");
            tabularData.Rows[1].ColumnValues["column2"].Should().Be("valueD");
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
| valueA  | 12      |
| valueC  | 13      |";

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
            tabularData.Rows[0].ColumnValues["column1"].Should().Be("valueA");
            tabularData.Rows[0].ColumnValues["column2"].Should().Be("valueB");
            tabularData.Rows[1].ColumnValues["column1"].Should().Be("valueC");
            tabularData.Rows[1].ColumnValues["column2"].Should().Be("valueD");
        }

        [Fact]
        public void Contains_returns_true_given_a_subset_of_row_data_to_match()
        {
            var testString = @" | column1 | column2 |
                                | --- | --- |
                                | valueA | valueB |
                                | valueC | valueD |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            var subsetString = @" | column1 |
                                | --- |
                                | valueC |";

            var subsetOfTabularData = TabularData.FromMarkdownTableString(subsetString);

            var result = tabularData.Contains(subsetOfTabularData, out var differences);

            result.Should().BeTrue();
        }

        [Fact]
        public void Contains_returns_true_given_a_subset_of_row_data_to_match_with_several_columns()
        {
            var testString = @" | id | state     | created    | ref          |
                                | -- | --------- | ---------- | ------------ |
                                | 1  | created   | 2021/11/02 | 23hgf4hj3gf4 |
                                | 2  | pending   | 2021/11/01 | 623kj4hv6hv4 |
                                | 3  | completed | 2021/10/31 | e0v9736eu476 |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            var subsetString = @" | created    | id |
                                  | ---------- | -- |
                                  | 2021/11/02 | 1  |
                                  | 2021/10/31 | 3  |";

            var subsetOfTabularData = TabularData.FromMarkdownTableString(subsetString);

            var result = tabularData.Contains(subsetOfTabularData, out var differences);

            result.Should().BeTrue();
        }

        [Fact]
        public void Contains_returns_false_given_a_subset_of_row_data_where_some_doesnt_match()
        {
            var testString = @" | id | state     | created    | ref          |
                                | -- | --------- | ---------- | ------------ |
                                | 1  | created   | 2021/11/02 | 23hgf4hj3gf4 |
                                | 2  | pending   | 2021/11/01 | 623kj4hv6hv4 |
                                | 3  | completed | 2021/10/31 | e0v9736eu476 |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            var subsetString = @" | created    | id |
                                  | ---------- | -- |
                                  | 2021/11/02 | 1  |
                                  | 2021/10/31 | 30 |";

            var subsetOfTabularData = TabularData.FromMarkdownTableString(subsetString);

            var result = tabularData.Contains(subsetOfTabularData, out var differences);

            result.Should().BeFalse();
            differences.Should().Contain(difference => difference == "Please ensure the types in the comparisonData match (i.e. a Guid is not the same as a string containing the said Guid) OR set the allowToStringComparison flag.");
        }

        [Fact]
        public void Contains_returns_false_if_column_doesnt_exist()
        {
            var testString = @" | id | state     | created    | ref          |
                                | -- | --------- | ---------- | ------------ |
                                | 1  | created   | 2021/11/02 | 23hgf4hj3gf4 |
                                | 2  | pending   | 2021/11/01 | 623kj4hv6hv4 |
                                | 3  | completed | 2021/10/31 | e0v9736eu476 |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            var subsetString = @" | frogs | id |
                                  | ----- | -- |
                                  | 1     | 30 |";

            var subsetOfTabularData = TabularData.FromMarkdownTableString(subsetString);

            var result = tabularData.Contains(subsetOfTabularData, out var differences);

            result.Should().BeFalse();
            differences.Should().Contain(difference => difference == "TabularData does not contain a column named frogs");
        }

        [Fact]
        public void Contains_returns_false_if_types_dont_match()
        {
            var testString = @" | id | state | created    | ref          |
                                | -- | ----- | ---------- | ------------ |
                                | 1  | 10    | 2021/11/02 | 23hgf4hj3gf4 |
                                | 2  | 20    | 2021/11/01 | 623kj4hv6hv4 |
                                | 3  | 30    | 2021/10/31 | e0v9736eu476 |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            var subsetString = @" | id    | state  |
                                  | ----- | ------ |
                                  | 3     | ""30"" |";

            var subsetOfTabularData = TabularData.FromMarkdownTableString(subsetString);

            var result = tabularData.Contains(subsetOfTabularData, out var differences);

            result.Should().BeFalse();
            differences.Should().Contain(difference => difference == "Please ensure the types in the comparisonData match (i.e. a Guid is not the same as a string containing the said Guid) OR set the allowToStringComparison flag.");
        }

        [Fact]
        public void Contains_returns_true_if_types_dont_match_but_allowToStringComparison_is_true()
        {
            var testString = @" | id | state | created    | ref          |
                                | -- | ----- | ---------- | ------------ |
                                | 1  | 10    | 2021/11/02 | 23hgf4hj3gf4 |
                                | 2  | 20    | 2021/11/01 | 623kj4hv6hv4 |
                                | 3  | 30    | 2021/10/31 | e0v9736eu476 |";

            var tabularData = TabularData.FromMarkdownTableString(testString);

            var subsetString = @" | id    | state  |
                                  | ----- | ------ |
                                  | 3     | ""30"" |";

            var subsetOfTabularData = TabularData.FromMarkdownTableString(subsetString);

            var result = tabularData.Contains(subsetOfTabularData, out var differences, true);

            result.Should().BeTrue();
            differences.Should().BeEmpty();
        }

        [Fact]
        public void TabularData_roundtrips_guids_from_markdown_table_string()
        {
            var id = Guid.NewGuid();

            var sb = new StringBuilder();
            sb.AppendLine("| Id | Name |");
            sb.AppendLine("| --- | --- |");
            sb.AppendLine($"| {id} | Andrew |");

            var tabularData = TabularData.FromMarkdownTableString(sb.ToString());

            tabularData.ValueAt("Id", row: 0).Should().Be(id);

            var roundTrippedString = tabularData.ToMarkdownTableString();

            roundTrippedString.Should().Be(sb.ToString().Trim());

        }

        [Fact]
        public void TabularData_ToSqlString_correctly_handles_guids()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var tabularData = TabularData.CreateWithColumns("Id", "Name")
                .AddRowWithValues(id1, "Andrew")
                .AddRowWithValues(id2, "James");

            var sqlInsert = tabularData.ToSqlString("Test");

            sqlInsert.Should().Be(@$"INSERT INTO Test
(Id,Name)
VALUES
('{id1}','Andrew')
,('{id2}','James')
");
        }

        [Fact]
        public void TabularData_ToSqlString_correctly_handles_quoted_numeric_strings()
        {

            var tabularData = TabularData.CreateWithColumns("Name", "Col2")
                .AddRowWithValues("Andrew", 123)
                .AddRowWithValues("James", "2");

            var sqlInsert = tabularData.ToSqlString("Test");

            sqlInsert.Should().Be(@$"INSERT INTO Test
(Name,Col2)
VALUES
('Andrew',123)
,('James','2')
");
        }

        [Fact]
        public void TabularData_ToDataTable_produces_expected_datatable()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var tabularData = TabularData.CreateWithColumns("Id", "Name")
                .AddRowWithValues(id1, "Andrew")
                .AddRowWithValues(id2, "James");

            var result = tabularData.ToDataTable();

            result.Columns.Count.Should().Be(2);
            result.Columns[0].ColumnName.Should().Be("Id");
            result.Columns[1].ColumnName.Should().Be("Name");

            result.Rows.Count.Should().Be(2);
            result.Rows[0]["Id"].Should().Be(id1.ToString());
            result.Rows[0]["Name"].Should().Be("Andrew");
            result.Rows[1]["Id"].Should().Be(id2.ToString());
            result.Rows[1]["Name"].Should().Be("James");
        }

        [Fact]
        public void TabularData_IndexOfColumnNamed_returns_expected_index()
        {
            var tabularData = TabularData.CreateWithColumns("Id", "Name")
                .AddRowWithValues(Guid.NewGuid(), "Andrew")
                .AddRowWithValues(Guid.NewGuid(), "James")
                .AddRowWithValues(Guid.NewGuid(), "Paul");

            var result = tabularData.IndexOfColumnNamed("Name");

            result.Should().Be(1);
        }

        [Fact]
        public void TabularData_RowWhere_returns_the_first_row_matching_the_constraint()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var tabularData = TabularData.CreateWithColumns("Id", "Name")
                .AddRowWithValues(id1, "Andrew")
                .AddRowWithValues(id2, "James");

            var result = tabularData.RowWhere("Id", id2);

            result.ColumnValues["Name"].Should().Be("James");
        }

        [Fact]
        public void TabularData_RowWhere_plus_TabularDataRow_indexer_can_be_used_for_assertions()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var tabularData = TabularData.CreateWithColumns("Id", "Name")
                .AddRowWithValues(id1, "Andrew")
                .AddRowWithValues(id2, "James");

            var row = tabularData.RowWhere("Id", id2);
                row["Name"].Should().Be("James");
        }

        [Fact]
        public void TabularData_TabularDataRow_indexer_can_be_used_to_update_column_values()
        {
            var id1 = Guid.NewGuid();
            var id2 = Guid.NewGuid();

            var tabularData = TabularData.CreateWithColumns("Id", "Name")
                .AddRowWithValues(id1, "Andrew")
                .AddRowWithValues(id2, "James");

            tabularData.RowWhere("Id", id2)["Name"] = "Jon";
            tabularData.RowWhere("Id", id2)["Name"].Should().Be("Jon");
        }

        [Fact]
        public void TabularData_TabularDataRow_indexer_can_be_used_to_add_column_values()
        {
            var id1 = Guid.NewGuid();

            var row = new TabularDataRow(new TabularData());

            row["Id"] = id1;
            row.ColumnValues.Count.Should().Be(1);
            row["Id"].Should().Be(id1);
        }
    }
}