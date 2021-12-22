using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using FluentAssertions;
using Moq;
using Xunit;

namespace CSharpSqlTests.FrameworkTests
{
    public class ThenTests
    {
        [Fact]
        public void TheReaderQueryResultsShouldBe_compares_LastQueryResult_as_reader_with_parameter()
        {
            var mockContext = new Mock<ILocalDbTestContext>();

            var expected = TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears");

            mockContext.Setup(x => x.LastQueryResult).Returns(SetupDataReader(TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears")).Object);

            var sut = new Then(mockContext.Object);

            sut.TheReaderQueryResultsShouldBe(expected);
        }

        [Fact]
        public void TheReaderQueryResultsShouldBe_compares_LastQueryResult_as_reader_with_parameter_and_fails_if_different()
        {
            var mockContext = new Mock<ILocalDbTestContext>();

            var expected = TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears");
            
            mockContext.Setup(x => x.LastQueryResult).Returns(SetupDataReader(TabularData.CreateWithColumns("A", "B").AddRowWithValues("fred", "george")).Object);

            var sut = new Then(mockContext.Object);

            Assert.Throws<Exception>(() => sut.TheReaderQueryResultsShouldBe(expected));
        }

        [Fact]
        public void TheReaderQueryResultsShouldBe_throws_if_datareader_is_null()
        {
            var mockContext = new Mock<ILocalDbTestContext>();
        
            mockContext.Setup(x => x.LastQueryResult).Returns(null);

            var sut = new Then(mockContext.Object);

            Assert.Throws<Exception>(() => sut.TheReaderQueryResultsShouldBe(new TabularData()));
        }

        [Fact]
        public void TheReaderQueryResultsShouldBe_passed_a_markdown_string_compares_LastQueryResult_as_reader_with_string()
        {
            var mockContext = new Mock<ILocalDbTestContext>();

            var expected = TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears").ToMarkdownTableString();

            mockContext.Setup(x => x.LastQueryResult).Returns(SetupDataReader(TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears")).Object);

            var sut = new Then(mockContext.Object);

            sut.TheReaderQueryResultsShouldBe(expected);
        }

        [Fact]
        public void TheReaderQueryResultsShouldContains_compares_LastQueryResult_as_reader_with_parameter()
        {
            var mockContext = new Mock<ILocalDbTestContext>();

            var expected = TabularData.CreateWithColumns("B").AddRowWithValues("pears");

            mockContext.Setup(x => x.LastQueryResult).Returns(SetupDataReader(TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears")).Object);

            var sut = new Then(mockContext.Object);

            sut.TheReaderQueryResultsShouldContain(expected);
        }

        [Fact]
        public void TheReaderQueryResultsShouldContains_compares_LastQueryResult_as_reader_with_string()
        {
            var mockContext = new Mock<ILocalDbTestContext>();

            var expected = TabularData.CreateWithColumns("B").AddRowWithValues("pears");
            
            mockContext.Setup(x => x.LastQueryResult).Returns(SetupDataReader(TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears")).Object);

            var sut = new Then(mockContext.Object);

            sut.TheReaderQueryResultsShouldContain(expected.ToMarkdownTableString());
        }

        [Fact]
        public void TheReaderQueryIsExecutedAndIsEqualTo_asserts_that_the_query_is_equal_to_the_supplied_TabularData_string()
        {
            var (mockContext, mockCmd) = GetMockedContext();

            var expected = TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears");

            mockCmd.Setup(x => x.ExecuteReader()).Returns(SetupDataReader(expected).Object);

            var sut = new Then(mockContext.Object);

            sut.TheReaderQueryIsExecutedAndIsEqualTo("", expected.ToMarkdownTableString());

            mockCmd.Verify(x => x.ExecuteReader(), Times.Once);
        }

        [Fact]
        public void TheReaderQueryIsExecutedAndContains_asserts_that_the_query_is_equal_to_the_supplied_TabularData_string()
        {
            var (mockContext, mockCmd) = GetMockedContext();
            
            mockCmd.Setup(x => x.ExecuteReader()).Returns(SetupDataReader(TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears")).Object);

            var sut = new Then(mockContext.Object);

            var expected = TabularData.CreateWithColumns("B").AddRowWithValues("pears").ToMarkdownTableString();

            sut.TheReaderQueryIsExecutedAndContains("", expected);

            mockCmd.Verify(x => x.ExecuteReader(), Times.Once);

        }

        private (Mock<ILocalDbTestContext> MockContext, Mock<IDbCommand> MockCommand) GetMockedContext()
        {
            var mockContext = new Mock<ILocalDbTestContext>();
            var mockParam = new Mock<IDbDataParameter>();
            var mockCmd = new Mock<IDbCommand>();
            var mockParamCollection = new Mock<IDataParameterCollection>();
            var mockDbConnection = new Mock<IDbConnection>();

            mockCmd.Setup(x => x.CreateParameter()).Returns(mockParam.Object);
            mockCmd.Setup(x => x.Parameters).Returns(mockParamCollection.Object);
            mockDbConnection.Setup(x => x.CreateCommand()).Returns(mockCmd.Object);
            mockContext.Setup(x => x.SqlConnection).Returns(mockDbConnection.Object);

            return (mockContext, mockCmd);
        }

        private Mock<IDataReader> SetupDataReader(TabularData dataToReturn)
        {
            var mockedReader = new Mock<IDataReader>();
            var mockedReaderColumnSchemaGen = mockedReader.As<IDbColumnSchemaGenerator>();

            var columns = dataToReturn.Columns.Select(columnName => new TestColumn(columnName)).ToList<DbColumn>();

            mockedReaderColumnSchemaGen.Setup(x => x.GetColumnSchema()).Returns(
                new ReadOnlyCollection<DbColumn>(columns));

            var readToggle = true;
            mockedReader.Setup(x => x.Read())
                .Returns(() => readToggle)
                .Callback(() => readToggle = false);
            
            foreach (var tabularDataRow in dataToReturn.Rows)
            {
                foreach (var columnName in dataToReturn.Columns)
                {
                    mockedReader.Setup(x => x[columnName]).Returns(tabularDataRow.ColumnValues[columnName]);
                }
            }
            
            return mockedReader;
        }

        [Fact]
        public void And_just_returns_the_then()
        {
            var context = new Mock<ILocalDbTestContext>();

            var sut = new Then(context.Object);

            var result = sut.And;

            result.Should().Be(sut);
        }

        [Fact]
        public void UsingThe_assigns_the_context_and_returns_the_then()
        {
            var context = new Mock<ILocalDbTestContext>();

            var sut = Then.UsingThe(context.Object);

            sut.Should().NotBeNull();
        }
    }
}