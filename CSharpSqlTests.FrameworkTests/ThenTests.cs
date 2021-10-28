using System.Data;
using System.Text;
using FluentAssertions;
using Moq;
using Xunit;

namespace CSharpSqlTests.FrameworkTests;

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
        //var mockContext = new Mock<ILocalDbTestContext>();

        //var mockedReader = new Mock<SqlDataReader>();

        //var expected = TabularData.CreateWithColumns("A", "B").AddRowWithValues("apples", "pairs");
        //mockContext.Setup(x => x.LastQueryResult).Returns(23);

        //var sut = new Then(mockContext.Object);

        //sut.TheReaderQueryResultsShouldBe(expected);
    }

    [Fact]
    public void And_just_returns_the_given()
    {
        var sb = new StringBuilder();
        var context = new Mock<ILocalDbTestContext>();

        var sut = new Then(context.Object);

        var result = sut.And();

        result.Should().Be(sut);
    }
}