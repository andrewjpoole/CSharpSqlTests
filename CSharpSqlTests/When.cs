using System;
using System.Data;
// ReSharper disable UnusedMember.Local

namespace CSharpSqlTests
{
    public class When
    {
        private readonly ILocalDbTestContext _context;
        private readonly Action<string>? _logAction;

        public When(ILocalDbTestContext context, Action<string>? logAction = null)
        {
            _context = context;
            _logAction = logAction;
        }

        public static When UsingThe(ILocalDbTestContext context, Action<string>? logAction = null) => new When(context, logAction);

        public When And() => this;

        private void LogMessage(string message)
        {
            _logAction?.Invoke(message);
        }

        public When TheStoredProcedureIsExecutedWithReturnParameter(string storedProcedureName, out object? returnValue, params (string Name, object Value)[] parameters)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = storedProcedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Transaction = _context.SqlTransaction;

            foreach (var (name, value) in parameters)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = name;
                param.Value = value;
                cmd.Parameters.Add(param);
            }

            var returnParameter = cmd.CreateParameter();
            returnParameter.ParameterName = "@ReturnVal";
            returnParameter.Direction = ParameterDirection.ReturnValue;
            cmd.Parameters.Add(returnParameter);

            cmd.ExecuteNonQuery();

            _context.LastQueryResult = returnParameter.Value;
            returnValue = _context.LastQueryResult;

            return this;
        }

        public When TheStoredProcedureIsExecutedWithReader(string storedProcedureName, params (string Name, object Value)[] parameters)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = storedProcedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Transaction = _context.SqlTransaction;

            foreach (var (name, value) in parameters)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = name;
                param.Value = value;
                cmd.Parameters.Add(param);
            }
            
            _context.LastQueryResult = cmd.ExecuteReader();

            return this;
        }

        public When TheScalarQueryIsExecuted(string cmdText, out object? returnValue)
        {
            TheScalarQueryIsExecuted(cmdText);
            returnValue = _context.LastQueryResult;

            return this;
        }

        public When TheScalarQueryIsExecuted(string cmdText)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = _context.SqlTransaction;

            _context.LastQueryResult = cmd.ExecuteScalar();

            return this;
        }

        public When TheReaderQueryIsExecuted(string cmdText, out object? returnValue)
        {
            TheReaderQueryIsExecuted(cmdText);
            returnValue = _context.LastQueryResult;

            return this;
        }

        public When TheReaderQueryIsExecuted(string cmdText)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = _context.SqlTransaction;

            _context.LastQueryResult = cmd.ExecuteReader();

            return this;
        }
    }
}