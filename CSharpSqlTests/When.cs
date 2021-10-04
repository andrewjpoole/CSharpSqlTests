using System.Data;

namespace CSharpSqlTests
{
    public class When
    {
        private LocalDbTestContext _context;

        public When(LocalDbTestContext context) => _context = context;

        public static When UsingThe(LocalDbTestContext context) => new When(context);

        public When And() => this;

        public When TheStoredProcedureIsExecuted(string storedProcedureName, out object returnValue, params (string Name, object Value)[] parameters)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = storedProcedureName;
            cmd.CommandType = CommandType.StoredProcedure;

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
        
        public When TheScalarQueryIsExecuted(string cmdText, out object returnValue)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;

            _context.LastQueryResult = cmd.ExecuteScalar();
            returnValue = _context.LastQueryResult;

            return this;
        }

        public When TheQueryIsExecuted(string cmdText, out object returnValue)
        {
            var cmd = _context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;

            _context.LastQueryResult = cmd.ExecuteReader();
            returnValue = _context.LastQueryResult;

            return this;
        }
    }
}