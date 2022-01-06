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

            var sut = new DacPacInfo("SampleDb", 6);

            sut.DacPacFound.Should().Be(true);
            sut.DacPacProjectName.Should().Be("SampleDb");
            sut.DacPacPath.EndsWith("SampleDb\\bin\\Debug\\SampleDb.dacpac");
        }
    }
}