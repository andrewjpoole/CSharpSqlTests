using System;
using System.Data;
using FluentAssertions;
using Moq;
using Xunit;

namespace CSharpSqlTests.FrameworkTests
{
    public class GivenTests
    {

        [Fact]
        public void TheDacPacIsDeployed_calls_DeployDacpac_on_context()
        {
            var context = new Mock<IDbTestContext>();

            var sut = new Given(context.Object);

            sut.TheDacpacIsDeployed("dacpacName");

            context.Verify(x => x.DeployDacpac("dacpacName", 4), Times.Once);
        }

        [Fact]
        public void TheFollowingDataExistsInTheTable_calls_ExecuteNonQueryc_on_cmd_and_writes_log()
        {
            var mockContext = new Mock<IDbTestContext>();
            var mockConnection = new Mock<IDbConnection>();
            var mockCommand = new Mock<IDbCommand>();

            mockConnection.Setup(x => x.CreateCommand()).Returns(mockCommand.Object);
            mockContext.Setup(x => x.SqlConnection).Returns(mockConnection.Object);

            var sut = new Given(mockContext.Object);

            sut.TheFollowingDataExistsInTheTable("tableName", "markdownString ");

            mockCommand.Verify(x => x.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void TheFollowingDataExistsInTheTable_logs_exceptions_thrown_during_sql_execute()
        {
            var mockContext = new Mock<IDbTestContext>();
            var mockConnection = new Mock<IDbConnection>();
            var mockCommand = new Mock<IDbCommand>();

            mockConnection.Setup(x => x.CreateCommand()).Returns(mockCommand.Object);
            mockContext.Setup(x => x.SqlConnection).Returns(mockConnection.Object);
            mockCommand.Setup(x => x.ExecuteNonQuery()).Throws<Exception>();

            var sut = new Given(mockContext.Object);

            Assert.Throws<Exception>(() => sut.TheFollowingDataExistsInTheTable("tableName", "markdownString "));
        }

        [Fact]
        public void TheFollowingSqlStatementIsExecuted_calls_ExecuteNonQueryc_on_cmd()
        {
            var mockContext = new Mock<IDbTestContext>();
            var mockConnection = new Mock<IDbConnection>();
            var mockCommand = new Mock<IDbCommand>();

            mockConnection.Setup(x => x.CreateCommand()).Returns(mockCommand.Object);
            mockContext.Setup(x => x.SqlConnection).Returns(mockConnection.Object);

            var sut = new Given(mockContext.Object);

            sut.TheFollowingSqlStatementIsExecuted("select * from blah");

            mockCommand.Verify(x => x.ExecuteNonQuery(), Times.Once);
        }
    
        [Fact]
        public void TheFollowingSqlStatementIsExecuted_logs_exceptions_thrown_during_sql_execute()
        {
            var mockContext = new Mock<IDbTestContext>();
            var mockConnection = new Mock<IDbConnection>();
            var mockCommand = new Mock<IDbCommand>();

            mockConnection.Setup(x => x.CreateCommand()).Returns(mockCommand.Object);
            mockContext.Setup(x => x.SqlConnection).Returns(mockConnection.Object);
            mockCommand.Setup(x => x.ExecuteNonQuery()).Throws<Exception>();

            var sut = new Given(mockContext.Object);

            Assert.Throws<Exception>(() => sut.TheFollowingSqlStatementIsExecuted("select * from blah"));
        }
    

        [Fact]
        public void TheForeignKeyConstraintIsRemoved_calls_ExecuteNonQueryc_on_cmd()
        {
            var mockContext = new Mock<IDbTestContext>();
            var mockConnection = new Mock<IDbConnection>();
            var mockCommand = new Mock<IDbCommand>();

            mockConnection.Setup(x => x.CreateCommand()).Returns(mockCommand.Object);
            mockContext.Setup(x => x.SqlConnection).Returns(mockConnection.Object);

            var sut = new Given(mockContext.Object);

            sut.TheForeignKeyConstraintIsRemoved("tableBlah", "fkBlah");

            mockCommand.VerifySet(x => x.CommandText = "ALTER TABLE tableBlah DROP CONSTRAINT fkBlah;", Times.Once);
            mockCommand.Verify(x => x.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void AnyDataIsTheTableIsRemoved_calls_ExecuteNonQueryc_on_cmd()
        {
            var mockContext = new Mock<IDbTestContext>();
            var mockConnection = new Mock<IDbConnection>();
            var mockCommand = new Mock<IDbCommand>();

            mockConnection.Setup(x => x.CreateCommand()).Returns(mockCommand.Object);
            mockContext.Setup(x => x.SqlConnection).Returns(mockConnection.Object);

            var sut = new Given(mockContext.Object);

            sut.AnyDataInTheTableIsRemoved("tableBlah");

            mockCommand.VerifySet(x => x.CommandText = "DELETE FROM tableBlah;", Times.Once);
            mockCommand.Verify(x => x.ExecuteNonQuery(), Times.Once);
        }

        [Fact]
        public void And_just_returns_the_given()
        {
            var context = new Mock<IDbTestContext>();

            var sut = new Given(context.Object);

            var result = sut.And;

            result.Should().Be(sut);
        }
    }
}