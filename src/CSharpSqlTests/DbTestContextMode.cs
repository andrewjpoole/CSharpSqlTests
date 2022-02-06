#pragma warning disable CS1591
namespace CSharpSqlTests;

public enum DbTestContextMode
{
    ExistingDatabaseViaConnectionString,
    TemporaryLocalDbInstance,
    ExistingLocalDbInstanceViaInstanceName
}