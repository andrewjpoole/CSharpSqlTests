using System.Text;
using FluentAssertions;
using Xunit;

namespace CSharpSqlTests.FrameworkTests
{
    public class LocalDbContextTests
    {
        [Fact]
        public void Ctor_initialises_new_localdb_instance()
        {
            var sb = new StringBuilder();

            var sut = new DacPacInfo("DatabaseToTest");

            sut.DacPacFound.Should().Be(true);
            sut.DacPacProjectName.Should().Be("DatabaseToTest");
            sut.DacPacPath.EndsWith("DatabaseToTest\\bin\\Debug\\DatabaseToTest.dacpac");
        }
    }
}