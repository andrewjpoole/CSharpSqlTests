using System.IO;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace CSharpSqlTests.FrameworkTests;

public class DacPacInfoTests
{
    [Fact]
    public void Ctor_finds_existing_dacpac_by_name()
    {
        var sut = new DacPacInfo("SampleDb", 6);

        sut.DacPacFound.Should().BeTrue();
        sut.DacPacPath.Should().EndWith("SampleDb.dacpac");
    }

    [Fact]
    public void Ctor_doesnt_find_non_existant_dacpac_by_name()
    {
        var sut = new DacPacInfo("j34hg54kjg5", 4);

        sut.DacPacFound.Should().BeFalse();
        sut.DacPacPath.Should().BeEmpty();
    }

    [Fact]
    public void Ctor_finds_existing_dacpac_by_absolute_path()
    {
        var currentDirectory = new DirectoryInfo(Assembly.GetExecutingAssembly().Location);
        var solutionDir = currentDirectory.Parent?.Parent?.Parent?.Parent?.Parent;
        var dacPacs = solutionDir!.EnumerateFiles("..\\examples\\SampleDb.dacpac", SearchOption.AllDirectories).ToList();

        var sut = new DacPacInfo(dacPacs.First().FullName, 4);

        sut.DacPacFound.Should().BeTrue();
        sut.DacPacPath.Should().EndWith("SampleDb.dacpac");
    }

    [Fact]
    public void Ctor_doesnt_find_non_existant_dacpac_by_absolute_path()
    {
        Assert.Throws<FileNotFoundException>(() => { new DacPacInfo("c:\\temp\\temp2\\temp3\\blah.dacpac", 4); });
    }
}