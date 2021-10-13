using System;
using System.Data;

namespace CSharpSqlTests
{
    public class When
    {
        private LocalDbTestContext _context;
        private readonly Action<string> _logAction;

        public When(LocalDbTestContext context, Action<string> logAction = null)
        {
            _context = context;
            _logAction = logAction;
        }

        public static When UsingThe(LocalDbTestContext context, Action<string> logAction = null) => new When(context, logAction);

        public When And() => this;

        private void LogMessage(string message)
        {
            if (_logAction is not null)
                _logAction(message);
        }

        public When TheStoredProcedureIsExecutedWithReturnParameter(string storedProcedureName, out object returnValue, params (string Name, object Value)[] parameters)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = storedProcedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Transaction = _context.SqlTransaction;

            foreach (var (name, value) in parameters)
            {
                cmd.Parameters.AddWithValue(name, value);
            }
            
            var returnParameter = cmd.Parameters.Add("@ReturnVal", SqlDbType.Int);
            returnParameter.Direction = ParameterDirection.ReturnValue;

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
                cmd.Parameters.AddWithValue(name, value);
            }
            
            _context.LastQueryResult = cmd.ExecuteReader();

            return this;
        }

        public When TheScalarQueryIsExecuted(string cmdText, out object returnValue)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = _context.SqlTransaction;

            _context.LastQueryResult = cmd.ExecuteScalar();
            returnValue = _context.LastQueryResult;

            return this;
        }

        public When TheReaderQueryIsExecuted(string cmdText, out object returnValue)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = _context.SqlTransaction;

            _context.LastQueryResult = cmd.ExecuteReader();
            returnValue = _context.LastQueryResult;

            return this;
        }
    }
}