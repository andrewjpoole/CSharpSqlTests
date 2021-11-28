﻿using System;
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

        public Then And() => this;
        
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

        public Then TheReaderQueryResultsShouldContain(string expectedMarkDownTableString)
        {
            var expectedTableData = TabularData.FromMarkdownTableString(expectedMarkDownTableString);

            TheReaderQueryResultsShouldContain(expectedTableData);

            return this;
        }

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
    }
}