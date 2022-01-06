﻿using System;
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
            Context.DeployDacpac(maxSearchDepth:6);
        }       

        public void Dispose()
        {
            Context.TearDown();
        }
    }
}