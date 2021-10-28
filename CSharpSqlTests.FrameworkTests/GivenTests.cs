using System.Data;
using System.Text;
using FluentAssertions;
using Moq;
using Xunit;

namespace CSharpSqlTests.FrameworkTests;

public class GivenTests
{

    [Fact]
    public void TheDacPacIsDeployed_calls_DeployDacpac_on_context()
    {
        var sb = new StringBuilder();
        var context = new Mock<ILocalDbTestContext>();

        var sut = new Given(context.Object, s => sb.AppendLine(s));

        sut.TheDacpacIsDeployed("dacpacName");

        context.Verify(x => x.DeployDacpac("dacpacName"), Times.Once);
    }

    [Fact]
    public void TheFollowingDataExistsInTheTable_calls_ExecuteNonQueryc_on_cmd_and_writes_log()
    {
        var sb = new StringBuilder();
        var mockContext = new Mock<ILocalDbTestContext>();
        var mockConnection = new Mock<IDbConnection>();
        var mockCommand = new Mock<IDbCommand>();

        mockConnection.Setup(x => x.CreateCommand()).Returns(mockCommand.Object);
        mockContext.Setup(x => x.SqlConnection).Returns(mockConnection.Object);

        var sut = new Given(mockContext.Object, s => sb.AppendLine(s));

        sut.TheFollowingDataExistsInTheTable("tableName", "markdownString ");

        mockCommand.Verify(x => x.ExecuteNonQuery(), Times.Once);
        sb.ToString().Should().StartWith("TheFollowingDataExistsInTheTable executed successfully");
    }
    
    [Fact]
    public void TheFollowingSqlStatementIsExecuted_calls_ExecuteNonQueryc_on_cmd()
    {
        var mockContext = new Mock<ILocalDbTestContext>();
        var mockConnection = new Mock<IDbConnection>();
        var mockCommand = new Mock<IDbCommand>();

        mockConnection.Setup(x => x.CreateCommand()).Returns(mockCommand.Object);
        mockContext.Setup(x => x.SqlConnection).Returns(mockConnection.Object);

        var sut = new Given(mockContext.Object);

        sut.TheFollowingSqlStatementIsExecuted("select * from blah");

        mockCommand.Verify(x => x.ExecuteNonQuery(), Times.Once);
    }

    [Fact]
    public void TheForeignKeyConstraintIsRemoved_calls_ExecuteNonQueryc_on_cmd()
    {
        var mockContext = new Mock<ILocalDbTestContext>();
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
    public void And_just_returns_the_given()
    {
        var sb = new StringBuilder();
        var context = new Mock<ILocalDbTestContext>();

        var sut = new Given(context.Object, s => sb.AppendLine(s));

        var result = sut.And();

        result.Should().Be(sut);
    }
}