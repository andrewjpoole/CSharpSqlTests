using System;
using System.Text;
using Microsoft.Data.SqlClient;
using Xunit;

namespace CSharpSqlTests
{
    public class Then
    {
        private LocalDbTestContext2 _context;

        public Then(LocalDbTestContext2 context) => _context = context;

        public static Then UsingThe(LocalDbTestContext2 context) => new Then(context);

        public Then And() => this;

        public Then TheLastQueryResultShouldBe(object expected)
        {
            Assert.True(_context.LastQueryResult.Equals(expected));
            return this;
        }

        public Then TheQueryResultsShouldBe(string expectedMarkDownTableString)
        {
            var reader = (SqlDataReader) _context.LastQueryResult;

            var tableDataResult = TableDefinition.FromSqlDataReader(reader);

            reader.Close();

            var areEqual = tableDataResult.IsEqualTo(expectedMarkDownTableString);

            if (!areEqual)
            {
                var sb = new StringBuilder();
                sb.AppendLine("Expected:");
                sb.AppendLine(expectedMarkDownTableString);
                sb.AppendLine("Actual:");
                sb.AppendLine(tableDataResult.ToMarkdownTableString());
                throw new Exception(sb.ToString());
            }

            return this;
        }
    }
}