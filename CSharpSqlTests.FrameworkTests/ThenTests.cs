using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Text;
using FluentAssertions;
using Moq;
using Xunit;

namespace CSharpSqlTests.FrameworkTests
{
    public class ThenTests
    {
        [Fact]
        public void TheNonReaderQueryResultShouldBe_compares_LastQueryResult_with_parameter()
        {
            var mockContext = new Mock<ILocalDbTestContext>();
        
            mockContext.Setup(x => x.LastQueryResult).Returns(23);

            var sut = new Then(mockContext.Object);

            sut.TheNonReaderQueryResultShouldBe(23);
        }

        [Fact]
        public void TheReaderQueryResultsShouldBe_compares_LastQueryResult_as_reader_with_parameter()
        {
            var mockContext = new Mock<ILocalDbTestContext>();

            var expected = TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears");

            var mockedReader = new Mock<IDataReader>();
            var mockedReaderColumnSchemaGen = mockedReader.As<IDbColumnSchemaGenerator>();
            mockedReaderColumnSchemaGen.Setup(x => x.GetColumnSchema()).Returns(
                new ReadOnlyCollection<DbColumn>(
                    new List<DbColumn>
                    {
                        new TestColumn("A"),
                        new TestColumn("B")
                    }));

            var readToggle = true;
            mockedReader.Setup(x => x.Read())
                .Returns(() => readToggle)
                .Callback(() => readToggle = false);

            mockedReader.Setup(x => x["A"]).Returns("apples");
            mockedReader.Setup(x => x["B"]).Returns("pears");

            mockContext.Setup(x => x.LastQueryResult).Returns(mockedReader.Object);

            var sut = new Then(mockContext.Object);

            sut.TheReaderQueryResultsShouldBe(expected);
        }

        [Fact]
        public void TheReaderQueryResultsShouldBe_compares_LastQueryResult_as_reader_with_parameter_and_fails_if_different()
        {
            var mockContext = new Mock<ILocalDbTestContext>();

            var expected = TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pears");

            var mockedReader = new Mock<IDataReader>();
            var mockedReaderColumnSchemaGen = mockedReader.As<IDbColumnSchemaGenerator>();
            mockedReaderColumnSchemaGen.Setup(x => x.GetColumnSchema()).Returns(
                new ReadOnlyCollection<DbColumn>(
                    new List<DbColumn>
                    {
                        new TestColumn("A"),
                        new TestColumn("B")
                    }));

            var readToggle = true;
            mockedReader.Setup(x => x.Read())
                .Returns(() => readToggle)
                .Callback(() => readToggle = false);

            mockedReader.Setup(x => x["A"]).Returns("fred");
            mockedReader.Setup(x => x["B"]).Returns("george");

            mockContext.Setup(x => x.LastQueryResult).Returns(mockedReader.Object);

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

            var mockedReader = new Mock<IDataReader>();
            var mockedReaderColumnSchemaGen = mockedReader.As<IDbColumnSchemaGenerator>();
            mockedReaderColumnSchemaGen.Setup(x => x.GetColumnSchema()).Returns(
                new ReadOnlyCollection<DbColumn>(
                    new List<DbColumn>
                    {
                        new TestColumn("A"),
                        new TestColumn("B")
                    }));

            var readToggle = true;
            mockedReader.Setup(x => x.Read())
                .Returns(() => readToggle)
                .Callback(() => readToggle = false);

            mockedReader.Setup(x => x["A"]).Returns("apples");
            mockedReader.Setup(x => x["B"]).Returns("pears");

            mockContext.Setup(x => x.LastQueryResult).Returns(mockedReader.Object);

            var sut = new Then(mockContext.Object);

            sut.TheReaderQueryResultsShouldBe(expected);
        }

        [Fact]
        public void TheReaderQueryResultsShouldContains_compares_LastQueryResult_as_reader_with_parameter()
        {
            var mockContext = new Mock<ILocalDbTestContext>();

            var expected = TabularData.CreateWithColumns("B").AddRowWithValues("pears");

            var mockedReader = new Mock<IDataReader>();
            var mockedReaderColumnSchemaGen = mockedReader.As<IDbColumnSchemaGenerator>();
            mockedReaderColumnSchemaGen.Setup(x => x.GetColumnSchema()).Returns(
                new ReadOnlyCollection<DbColumn>(
                    new List<DbColumn>
                    {
                        new TestColumn("A"),
                        new TestColumn("B")
                    }));

            var readToggle = true;
            mockedReader.Setup(x => x.Read())
                .Returns(() => readToggle)
                .Callback(() => readToggle = false);

            mockedReader.Setup(x => x["A"]).Returns("apples");
            mockedReader.Setup(x => x["B"]).Returns("pears");

            mockContext.Setup(x => x.LastQueryResult).Returns(mockedReader.Object);

            var sut = new Then(mockContext.Object);

            sut.TheReaderQueryResultsShouldContain(expected);
        }

        [Fact]
        public void TheReaderQueryResultsShouldContains_compares_LastQueryResult_as_reader_with_string()
        {
            var mockContext = new Mock<ILocalDbTestContext>();

            var expected = TabularData.CreateWithColumns("B").AddRowWithValues("pears");

            var mockedReader = new Mock<IDataReader>();
            var mockedReaderColumnSchemaGen = mockedReader.As<IDbColumnSchemaGenerator>();
            mockedReaderColumnSchemaGen.Setup(x => x.GetColumnSchema()).Returns(
                new ReadOnlyCollection<DbColumn>(
                    new List<DbColumn>
                    {
                        new TestColumn("A"),
                        new TestColumn("B")
                    }));

            var readToggle = true;
            mockedReader.Setup(x => x.Read())
                .Returns(() => readToggle)
                .Callback(() => readToggle = false);

            mockedReader.Setup(x => x["A"]).Returns("apples");
            mockedReader.Setup(x => x["B"]).Returns("pears");

            mockContext.Setup(x => x.LastQueryResult).Returns(mockedReader.Object);

            var sut = new Then(mockContext.Object);

            sut.TheReaderQueryResultsShouldContain(expected.ToMarkdownTableString());
        }

        [Fact]
        public void And_just_returns_the_then()
        {
            var context = new Mock<ILocalDbTestContext>();

            var sut = new Then(context.Object);

            var result = sut.And();

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