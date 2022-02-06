using System;
using System.Data;
using System.IO;
using MartinCostello.SqlLocalDb;
#pragma warning disable CS1591

namespace CSharpSqlTests;

public class TemporaryLocalDbContext : ISqlDatabaseContext
{
    private readonly Action<string>? _logTiming;
    private readonly ISqlLocalDbInstanceManager _manager;
    private readonly string _instanceName;
    // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
    private readonly TemporarySqlLocalDbInstance _temporaryInstance;

    private string InstancePath =>
        $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\{_instanceName}";

    public string ConnectionString => _manager.GetInstanceInfo().GetConnectionString();

    public TemporaryLocalDbContext(Action<string>? logTiming = null)
    {
        _logTiming = logTiming;
        var localDbApi = new SqlLocalDbApi();

        _temporaryInstance = localDbApi.CreateTemporaryInstance();
        _manager = _temporaryInstance.Manage();

        _logTiming?.Invoke($"Temporary localDb instance created named {_temporaryInstance.Name}");

        _instanceName = _temporaryInstance.Name;
            
    }

    public IDbConnection GetNewSqlConnection()
    {
        return _manager.CreateConnection();
    }

    public void TearDown()
    {
        _manager.Stop();

        _logTiming?.Invoke("Instance stopped");

        var tempInstanceDir = new DirectoryInfo(InstancePath);

        tempInstanceDir.Delete(true);

        _logTiming?.Invoke("Instance directory deleted");
    }

    public void CreateNewDatabase(string databaseName)
    {
        using var connection = GetNewSqlConnection();
        connection.Open();
        var createDbCmd = connection.CreateCommand();
        createDbCmd.CommandText = @$"CREATE DATABASE [{databaseName}] ON  PRIMARY 
    ( NAME = N'{databaseName}_Data', FILENAME = N'{InstancePath}\{databaseName}.mdf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )
     LOG ON 
    ( NAME = N'{databaseName}_Log', FILENAME = N'{InstancePath}\{databaseName}.ldf' , SIZE = 10MB , MAXSIZE = 50MB, FILEGROWTH = 5MB )";
        createDbCmd.CommandType = CommandType.Text;
        createDbCmd.ExecuteNonQuery();
        connection.Close();
    }
}