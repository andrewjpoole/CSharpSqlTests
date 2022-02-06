using System;
using System.Data;
using MartinCostello.SqlLocalDb;
#pragma warning disable CS1591

namespace CSharpSqlTests;

public class ExistingLocalDbViaInstanceNameContext : ISqlDatabaseContext
{
    private readonly Action<string>? _logTiming;
    private readonly bool _stopNormalInstanceAfterwards;
    private readonly ISqlLocalDbInstanceManager _manager;
    private readonly string _instanceName;

    private string InstancePath =>
        $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\Microsoft\\Microsoft SQL Server Local DB\\Instances\\{_instanceName}";

    public string ConnectionString => _manager.GetInstanceInfo().GetConnectionString();

    public ExistingLocalDbViaInstanceNameContext(string localDbInstanceName, Action<string>? logTiming = null)
    {
        _logTiming = logTiming;
        var localDbApi = new SqlLocalDbApi();

        var normalInstance = localDbApi.GetOrCreateInstance(localDbInstanceName);
        _manager = normalInstance.Manage();

        if (!normalInstance.IsRunning)
        {
            _logTiming?.Invoke($"Starting localDb instance {_instanceName}");
            _stopNormalInstanceAfterwards = true;
            _manager.Start();
        }

        _instanceName = normalInstance.Name;
    }

    public IDbConnection GetNewSqlConnection()
    {
        return _manager.CreateConnection();
    }
        
    public void TearDown()
    {
        if (_stopNormalInstanceAfterwards)
        {
            _logTiming?.Invoke("Stopping localDb instance (because it wasn't running beforehand)");
            _manager.Stop();
        }
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