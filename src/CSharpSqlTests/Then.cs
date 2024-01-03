using System;
using System.Data;
// ReSharper disable UnusedMember.Local
#pragma warning disable CS1591

namespace CSharpSqlTests;

public class Then
{
    public readonly IDbTestContext Context;

    public Then(IDbTestContext context)
    {
        Context = context;
    }

    public static Then UsingThe(IDbTestContext context) => new(context);

    public Then And => this;

    /// <summary>
    /// A method which returns the context's LastQueryResult as a TabularData for assertions etc, provided that a Reader query has previously populated it.
    /// </summary>
    /// <param name="results">A TabularData containing the context's LastQueryResult provided that a Reader query has previously populated it.</param>
    /// <exception cref="Exception">Exceptions are thrown if the LastQueryResult is null or does not contain a DataReader</exception>
    public Then FetchingTheReaderQueryResults(out TabularData results)
    {
        if (Context.CurrentDataReader is null)
            throw new Exception("context.CurrentDataReader is null");
            
        var tableDataResult = TabularData.FromSqlDataReader(Context.CurrentDataReader);
            
        results = tableDataResult;

        return this;
    }

    /// <summary>
    /// A method which asserts that the context's LastQueryResult property should contain certain tabular data.
    /// </summary>
    /// <param name="expectedMarkDownTableString">
    /// A markdown table string containing the data to assert
    /// </param>
    /// <example>
    /// <code>
    ///  | Id | Type | Make | Model   |
    ///  | -- | ---- | ---- | ------- |
    ///  | 1  | Car  | Fiat | 500     |
    ///  | 2  | Van  | Ford | Transit |
    /// this string represents a table with 4 columns and 2 rows.
    /// 
    /// A string value in a column of: 
    /// 2021-11-03  -> will be interpreted as a DateTime, use any parsable date and time string
    /// 234         -> will be interpreted as an int
    /// null        -> will be interpreted as null
    /// emptyString -> will be interpreted as an empty string
    /// true        -> will be interpreted as boolean true
    /// false       -> will be interpreted as boolean false
    /// "2"         -> will be preserved as a string
    /// </code>
    /// </example>
    public Then TheReaderQueryResultsShouldBe(string expectedMarkDownTableString)
    {
        var expectedTableData = TabularData.FromMarkdownTableString(expectedMarkDownTableString);

        TheReaderQueryResultsShouldBe(expectedTableData);

        return this;
    }

    /// <summary>
    /// A method which asserts that the context's LastQueryResult property should contain certain tabular data.
    /// </summary>
    /// <param name="expectedData">A TabularData defining the data to assert.</param>
    public Then TheReaderQueryResultsShouldBe(TabularData expectedData)
    {
        if (Context.CurrentDataReader is null)
            throw new Exception("context.LastQueryResult is null");
            
        var tableDataResult = TabularData.FromSqlDataReader(Context.CurrentDataReader);
            
        var areEqual = tableDataResult.IsEqualTo(expectedData, out var differences);

        if (areEqual) return this;

        var message = $"Differences:\n{string.Join(Environment.NewLine, differences)}";
        throw new Exception(message);
    }

    /// <summary>
    /// A method which asserts that the context's LastQueryResult property should contain certain tabular data.
    /// </summary>
    /// <param name="expectedMarkDownTableString">
    /// A markdown table string containing the data to assert
    /// </param>
    /// <example>
    /// <code>
    ///  | Id | Type | Make | Model   |
    ///  | -- | ---- | ---- | ------- |
    ///  | 1  | Car  | Fiat | 500     |
    ///  | 2  | Van  | Ford | Transit |
    /// this string represents a table with 4 columns and 2 rows.
    /// 
    /// A string value in a column of: 
    /// 2021-11-03  -> will be interpreted as a DateTime, use any parsable date and time string
    /// 234         -> will be interpreted as an int
    /// null        -> will be interpreted as null
    /// emptyString -> will be interpreted as an empty string
    /// true        -> will be interpreted as boolean true
    /// false       -> will be interpreted as boolean false
    /// "2"         -> will be preserved as a string
    /// </code>
    /// </example>
    public Then TheReaderQueryResultsShouldContain(string expectedMarkDownTableString)
    {
        var expectedTableData = TabularData.FromMarkdownTableString(expectedMarkDownTableString);

        TheReaderQueryResultsShouldContain(expectedTableData);

        return this;
    }

    /// <summary>
    /// A method which asserts that the context's LastQueryResult property should contain certain tabular data.
    /// </summary>
    /// <param name="expectedData">A TabularData defining the data to assert.</param>
    public Then TheReaderQueryResultsShouldContain(TabularData expectedData)
    {
        if (Context.CurrentDataReader is null)
            throw new Exception("context.CurrentDataReader is null");
            
        var tableDataResult = TabularData.FromSqlDataReader(Context.CurrentDataReader);
            
        var areEqual = tableDataResult.Contains(expectedData, out var differences);

        if (areEqual) return this;

        var message = $"Differences:\n{string.Join(Environment.NewLine, differences)}";
        throw new Exception(message);
    }
        
    /// <summary>
    /// A method which executes a Sql scalar query and returns the result to be asserted against.
    /// </summary>
    /// <param name="cmdText">A string containing the Sql query</param>
    /// <param name="returnValue">An object containing the query result.</param>
    public Then TheScalarQueryIsExecuted(string cmdText, out object? returnValue)
    {
        var cmd = Context.SqlConnection.CreateCommand();
        cmd.CommandText = cmdText;
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = Context.SqlTransaction;
            
        returnValue = cmd.ExecuteScalar();
            
        return this;
    }

        

    /// <summary>
    /// A method which executes a Sql reader query and returns the resulting TabularData to be asserted against.
    /// </summary>
    /// <param name="cmdText">A string containing the Sql query</param>
    /// <param name="returnValue">A TabularData containing the results of the query to assert</param>
    public Then TheReaderQueryIsExecuted(string cmdText, out TabularData returnValue)
    {
        var cmd = Context.SqlConnection.CreateCommand();
        cmd.CommandText = cmdText;
        cmd.CommandType = CommandType.Text;
        cmd.Transaction = Context.SqlTransaction;
            
        returnValue = TabularData.FromSqlDataReader(cmd.ExecuteReader());
            
        return this;
    }

    /// <summary>
    /// A method which executes a reader query and asserts that result should be equal to the supplied tabular data.
    /// </summary>
    /// <param name="cmdText">A string containing the Sql query</param>
    /// <param name="expectedMarkDownTableString">
    /// A markdown table string containing the data to assert
    /// </param>
    /// <example>
    /// <code>
    ///  | Id | Type | Make | Model   |
    ///  | -- | ---- | ---- | ------- |
    ///  | 1  | Car  | Fiat | 500     |
    ///  | 2  | Van  | Ford | Transit |
    /// this string represents a table with 4 columns and 2 rows.
    /// 
    /// A string value in a column of: 
    /// 2021-11-03  -> will be interpreted as a DateTime, use any parsable date and time string
    /// 234         -> will be interpreted as an int
    /// null        -> will be interpreted as null
    /// emptyString -> will be interpreted as an empty string
    /// true        -> will be interpreted as boolean true
    /// false       -> will be interpreted as boolean false
    /// "2"         -> will be preserved as a string
    /// </code>
    /// </example>
    public Then TheReaderQueryIsExecutedAndIsEqualTo(string cmdText, string expectedMarkDownTableString)
    {
        TheReaderQueryIsExecuted(cmdText, out var returnValue);

        var expected = TabularData.FromMarkdownTableString(expectedMarkDownTableString);

        var areEqual = returnValue.IsEqualTo(expected, out var differences);

        if (areEqual) return this;

        var message = $"Differences:\n{string.Join(Environment.NewLine, differences)}";
        throw new Exception(message);
    }

    /// <summary>
    /// A method which executes a reader query and asserts that result should contain the supplied tabular data.
    /// </summary>
    /// <param name="cmdText">A string containing the Sql query</param>
    /// <param name="expectedMarkDownTableString">
    /// A markdown table string containing the data to assert
    /// </param>
    /// <example>
    /// <code>
    ///  | Id | Type | Make | Model   |
    ///  | -- | ---- | ---- | ------- |
    ///  | 1  | Car  | Fiat | 500     |
    ///  | 2  | Van  | Ford | Transit |
    /// this string represents a table with 4 columns and 2 rows.
    /// 
    /// A string value in a column of: 
    /// 2021-11-03  -> will be interpreted as a DateTime, use any parsable date and time string
    /// 234         -> will be interpreted as an int
    /// null        -> will be interpreted as null
    /// emptyString -> will be interpreted as an empty string
    /// true        -> will be interpreted as boolean true
    /// false       -> will be interpreted as boolean false
    /// "2"         -> will be preserved as a string
    /// </code>
    /// </example>
    public Then TheReaderQueryIsExecutedAndContains(string cmdText, string expectedMarkDownTableString)
    {
        TheReaderQueryIsExecuted(cmdText, out var returnValue);

        var expected = TabularData.FromMarkdownTableString(expectedMarkDownTableString);

        var contains = returnValue.Contains(expected, out var differences);

        if (contains) return this;

        var message = $"Missing data:\n{string.Join(Environment.NewLine, differences)}";
        throw new Exception(message);
    }
}