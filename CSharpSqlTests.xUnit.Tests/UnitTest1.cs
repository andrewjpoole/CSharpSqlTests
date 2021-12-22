using Moq;
using Xunit;

namespace CSharpSqlTests.xUnit.Tests
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
    }
}