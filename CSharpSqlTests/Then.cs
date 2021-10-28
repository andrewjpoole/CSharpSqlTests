using System;
using Microsoft.Data.SqlClient;
using Xunit;
// ReSharper disable UnusedMember.Local

namespace CSharpSqlTests
{
    public class Then
    {
        private readonly ILocalDbTestContext _context;
        private readonly Action<string>? _logAction;

        public Then(ILocalDbTestContext context, Action<string>? logAction = null)
        {
            _context = context;
            _logAction = logAction;
        }

        public static Then UsingThe(ILocalDbTestContext context, Action<string>? logAction = null) => new Then(context, logAction);

        public Then And() => this;

        private void LogMessage(string message)
        {
            _logAction?.Invoke(message);
        }

        public Then TheNonReaderQueryResultShouldBe(object expected)
        {
            Assert.True(_context.LastQueryResult?.Equals(expected));
            return this;
        }

        public Then TheReaderQueryResultsShouldBe(string expectedMarkDownTableString)
        {
            var expectedTableData = TabularData.FromMarkdownTableString(expectedMarkDownTableString);

            TheReaderQueryResultsShouldBe(expectedTableData);

            return this;
        }

        public Then TheReaderQueryResultsShouldBe(TabularData expectedData)
        {
            if (_context.LastQueryResult is null)
                throw new Exception("context.LastQueryResult is null");

            var reader = (SqlDataReader)_context.LastQueryResult;

            if (reader is null)
                throw new Exception("context.LastQueryResult does not contain a SqlDataReader object");

            var tableDataResult = TabularData.FromSqlDataReader(reader);

            reader.Close();

            var areEqual = tableDataResult.IsEqualTo(expectedData, out var differences);

            if (areEqual) return this;

            var message = $"Differences:\n{string.Join(Environment.NewLine, differences)}";
            throw new Exception(message);
        }
    }
}