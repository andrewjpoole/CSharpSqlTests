using System;
using CSharpSqlTests;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SampleDatabaseTests
{
    public class LocalDbContextFixture : IDisposable
    {
        public LocalDbTestContext2 Context;

        public LocalDbContextFixture(IMessageSink sink)
        {            
            Context = new LocalDbTestContext2("DatabaseToTest", log => sink.OnMessage(new DiagnosticMessage(log)));
            Context.Start();
            Context.DeployDacpac();
        }       

        public void Dispose()
        {
            Context.TearDown();
        }
    }
}