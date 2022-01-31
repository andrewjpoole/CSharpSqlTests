using CSharpSqlTests.NUnit;
using Moq;
using NUnit.Framework;

namespace CSharpSqlTests.NUit.Tests
{
    public class ThenTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var mockContext = new Mock<IDbTestContext>();

            mockContext.Setup(x => x.LastNonReaderQueryResult).Returns(23);

            var sut = new Then(mockContext.Object);

            sut.TheNonReaderQueryResultShouldBe(23);
        }
    }
}