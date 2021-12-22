using System;
using CSharpSqlTests;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace SampleDatabaseTestsXunit
{
    public class LocalDbContextFixture : IDisposable
    {
        public LocalDbTestContext Context;

        public LocalDbContextFixture(IMessageSink sink)
        {
            Context = new LocalDbTestContext("SampleDb", log => sink.OnMessage(new DiagnosticMessage(log)));
            Context.DeployDacpac();
        }       

        public void Dispose()
        {
            Context.TearDown();
        }
    }
}