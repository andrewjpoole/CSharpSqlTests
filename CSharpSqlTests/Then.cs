using FluentAssertions;
using System;
using System.Data;
using Xunit;
// ReSharper disable UnusedMember.Local

namespace CSharpSqlTests
{
    public partial class Then
    {
        private readonly ILocalDbTestContext _context;

        public Then(ILocalDbTestContext context)
        {
            _context = context;
        }

        public static Then UsingThe(ILocalDbTestContext context) => new Then(context);

        public Then And => this;

        /// <summary>
        /// A method which asserts that the context's LastQueryResult property is equal to a supplied object.
        /// </summary>
        /// <param name="expected">An object containing the value to assert</param>
        public Then TheNonReaderQueryResultShouldBe(object expected)
        {
            Assert.True(_context.LastQueryResult?.Equals(expected));
            return this;
        }

        /// <summary>
        /// A method which asserts that the context's LastQueryResult property should contain certain tabular data.
        /// </summary>
        /// <param name="expectedMarkDownTableString">
        /// A markdown table string containing the data to assert
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
        /// </code>
        /// </example>
        /// </param>
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
            if (_context.LastQueryResult is null)
                throw new Exception("context.LastQueryResult is null");

            var dataReader = (IDataReader)_context.LastQueryResult;

            if (dataReader is null)
                throw new Exception("context.LastQueryResult does not contain a IDataReader object");

            var tableDataResult = TabularData.FromSqlDataReader(dataReader);

            dataReader.Close();

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
        /// </code>
        /// </example>
        /// </param>
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
            if (_context.LastQueryResult is null)
                throw new Exception("context.LastQueryResult is null");

            var dataReader = (IDataReader)_context.LastQueryResult;

            if (dataReader is null)
                throw new Exception("context.LastQueryResult does not contain a IDataReader object");

            var tableDataResult = TabularData.FromSqlDataReader(dataReader);

            dataReader.Close();

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
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = _context.SqlTransaction;

            _context.CloseDataReaderIfOpen();

            returnValue = cmd.ExecuteScalar();
            
            return this;
        }

        /// <summary>
        /// A method which executes a Sql scalar query and evaluates an assertion using the supplied Func.
        /// </summary>
        /// <param name="cmdText">A string containing the Sql query</param>
        /// <param name="assertionUsingQueryResult">A Func which is passed the query result and should return a bool, false if the test should fail.</param>
        public Then TheScalarQueryIsExecuted(string cmdText, Func<object?, bool> assertionUsingQueryResult)
        {
            TheScalarQueryIsExecuted(cmdText, out var returnValue);

            assertionUsingQueryResult(returnValue).Should().BeTrue();

            return this;
        }

        /// <summary>
        /// A method which executes a Sql reader query and returns the resulting TabularData to be asserted against.
        /// </summary>
        /// <param name="cmdText">A string containing the Sql query</param>
        /// <param name="returnValue">A TabularData containing the results of the query to assert</param>
        public Then TheReaderQueryIsExecuted(string cmdText, out TabularData returnValue)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = _context.SqlTransaction;

            _context.CloseDataReaderIfOpen();

            returnValue = TabularData.FromSqlDataReader(cmd.ExecuteReader());
            
            return this;
        }

        /// <summary>
        /// A method which executes a Sql reader query and evaluates an assertion using the supplied Func.
        /// </summary>
        /// <param name="cmdText">A string containing the Sql query</param>
        /// <param name="assertionUsingQueryResult"></param>
        public Then TheReaderQueryIsExecuted(string cmdText, Func<TabularData, bool> assertionUsingQueryResult)
        {
            TheReaderQueryIsExecuted(cmdText, out var returnValue);
            
            assertionUsingQueryResult(returnValue).Should().BeTrue();

            return this;
        }
    }
}