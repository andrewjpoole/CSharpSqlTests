using System.Data;
using FluentAssertions;
using Moq;
using Xunit;

namespace CSharpSqlTests.FrameworkTests;

public class WhenTests
{
    private readonly Mock<IDbTestContext> _mockContext;
    private readonly Mock<IDbDataParameter> _mockParam;
    private readonly Mock<IDbCommand> _mockCmd;
    private readonly Mock<IDataParameterCollection> _mockParamCollection;
    private readonly Mock<IDbConnection> _mockDbConnection;
    private When _sut;


    public WhenTests()
    {
        _mockContext = new Mock<IDbTestContext>();
        _mockParam = new Mock<IDbDataParameter>();
        _mockCmd = new Mock<IDbCommand>();
        _mockParamCollection = new Mock<IDataParameterCollection>();
        _mockDbConnection = new Mock<IDbConnection>();

        _mockCmd.Setup(x => x.CreateParameter()).Returns(_mockParam.Object);
        _mockCmd.Setup(x => x.Parameters).Returns(_mockParamCollection.Object);
        _mockDbConnection.Setup(x => x.CreateCommand()).Returns(_mockCmd.Object);
        _mockContext.Setup(x => x.SqlConnection).Returns(_mockDbConnection.Object);

        _sut = new When(_mockContext.Object);
    }

    [Fact]
    public void TheStoredProcedureIsExecutedWithReturnParameter_executes_a_stored_proc()
    {
        _mockParam.Setup(x => x.Value).Returns(13); // simulate the return param value

        _sut.TheStoredProcedureIsExecutedWithReturnParameter("spTest", ("Input", 12));

        _mockDbConnection.Verify(x => x.CreateCommand(), Times.Once);
        _mockCmd.Verify(x => x.ExecuteNonQuery(), Times.Once);
        _mockContext.VerifySet(x => x.LastNonReaderQueryResult = 13, Times.Once());
    }

    [Fact]
    public void TheStoredProcedureIsExecutedWithReturnParameter_executes_a_stored_proc_and_returns_the_result_via_out_arg()
    {
        _mockParam.Setup(x => x.Value).Returns(13);
        _sut.TheStoredProcedureIsExecutedWithReturnParameter("spTest", out var result,("Input", 12));
        
        result.Should().Be(13);
    }
    
    [Fact]
    public void TheStoredProcedureIsExecutedWithReader_executes_a_stored_proc_using_reader_and_assigns_result_to_LastQueryResult()
    {
        _sut.TheStoredProcedureIsExecutedWithReader("spTest", ("Input", 12));

        _mockDbConnection.Verify(x => x.CreateCommand(), Times.Once);
        _mockCmd.Verify(x => x.ExecuteReader(), Times.Once);
        _mockContext.VerifySet(x => x.CurrentDataReader = It.IsAny<IDataReader>(), Times.Once());
    }
    
    [Fact]
    public void TheScalarQueryIsExecuted_executes_a_scalerquery_on_the_context()
    {
        _sut.TheScalarQueryIsExecuted("SELECT * FROM Test");

        _mockCmd.Verify(x => x.ExecuteScalar(), Times.Once);
        _mockContext.VerifySet(x => x.LastNonReaderQueryResult = It.IsAny<object>(), Times.Once());
    }

    [Fact]
    public void TheScalarQueryIsExecuted_executes_a_scalerquery_on_the_context_and_returns_the_result_via_out_arg()
    {
        _mockCmd.Setup(x => x.ExecuteScalar()).Returns(13);
        _sut.TheScalarQueryIsExecuted("SELECT * FROM Test", out var result);

        _mockCmd.Verify(x => x.ExecuteScalar(), Times.Once);

        result.Should().Be(13);
    }
    
    [Fact]
    public void TheReaderQueryIsExecuted_executes_a_readerquery_on_the_context()
    {
        _sut.TheReaderQueryIsExecuted("SELECT * FROM Test");

        _mockCmd.Verify(x => x.ExecuteReader(), Times.Once);
        _mockContext.VerifySet(x => x.CurrentDataReader = It.IsAny<IDataReader>(), Times.Once());
    }

    [Fact]
    public void TheReaderQueryIsExecuted_executes_a_readerquery_on_the_context_and_returns_the_result_via_out_arg()
    {
        var mockReader = new Mock<IDataReader>();
        _mockCmd.Setup(x => x.ExecuteReader()).Returns(mockReader.Object);
        _sut.TheReaderQueryIsExecuted("SELECT * FROM Test", out var readerResult);

        _mockCmd.Verify(x => x.ExecuteReader(), Times.Once);

        readerResult.Should().Be(mockReader.Object);
    }

    [Fact]
    public void TheStoredProcedureIsExecuted_executes_a_nonquery_stored_procedure()
    {
        var mockReader = new Mock<IDataReader>();
        _mockCmd.Setup(x => x.ExecuteNonQuery()).Returns(2);
        _sut.TheStoredProcedureIsExecuted("spTest", out var affectedRows, ("Input", 12));

        _mockDbConnection.Verify(x => x.CreateCommand(), Times.Once);
        _mockCmd.Verify(x => x.ExecuteNonQuery(), Times.Once);
        affectedRows.Should().Be(2);
    }

    [Fact]
    public void And_just_returns_the_when()
    {
        var context = new Mock<IDbTestContext>();

        var sut = new When(context.Object);

        var result = sut.And;

        result.Should().Be(sut);
    }

    [Fact]
    public void UsingThe_assigns_the_context_and_returns_the_when()
    {
        var context = new Mock<IDbTestContext>();

        var sut = When.UsingThe(context.Object);

        sut.Should().NotBeNull();
    }
}