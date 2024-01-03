using NUnit.Framework;

namespace CSharpSqlTests.NUnit;

public static class TabularDataRowExtensions
{
    /// <summary>
    /// A method which enables an assertion to be made against a value based on its column name and a supplied expected value.
    /// </summary>
    /// <param name="columnName">The name of the column used to select the actual value to assert against.</param>
    /// <param name="expected">An object contain the expected value to assert.</param>
    public static TabularDataRow AssertHasValue(this TabularDataRow row, string columnName, object? expected)
    {
        var actual = row.ColumnValues[columnName];
        Assert.True(actual.Equals(expected), $"The actual ({actual}) was not equal to the expected ({expected})");

        return row;
    }
}