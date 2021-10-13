using System;
using System.Linq;
using System.Text;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CSharpSqlTests
{
    public class Then
    {
        private LocalDbTestContext _context;
        private readonly Action<string> _logAction;

        public Then(LocalDbTestContext context, Action<string> logAction = null)
        {
            _context = context;
            _logAction = logAction;
        }

        public static Then UsingThe(LocalDbTestContext context, Action<string> logAction = null) => new Then(context, logAction);

        public Then And() => this;

        private void LogMessage(string message)
        {
            if (_logAction is not null)
                _logAction(message);
        }

        public Then TheLastQueryResultShouldBe(object expected) // todo rename this, it shouldn't be used for datareader
        {
            Assert.True(_context.LastQueryResult.Equals(expected));
            return this;
        }

        public Then TheReaderQueryResultsShouldBe(string expectedMarkDownTableString)
        {
            var expectedTableData = TableDefinition.FromMarkdownTableString(expectedMarkDownTableString);

            TheReaderQueryResultsShouldBe(expectedTableData);

            return this;
        }

        public Then TheReaderQueryResultsShouldBe(TableDefinition expectedData)
        {
            var reader = (SqlDataReader)_context.LastQueryResult;

            var tableDataResult = TableDefinition.FromSqlDataReader(reader);

            reader.Close();

            var areEqual = tableDataResult.IsEqualTo(expectedData, out var differences);

            if (!areEqual)
            {
                var message = $"Differences:\n{string.Join(Environment.NewLine, differences)}";
                throw new Exception(message);
            }

            return this;
        }
    }
}