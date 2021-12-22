using System.Data;

namespace CSharpSqlTests
{
    public static class DatabaseCommandExtensions
    {
        public static IDbDataParameter AddParameterWithValue(this IDbCommand cmd, string name, object value)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Value = value;
            cmd.Parameters.Add(param);

            return param;
        }

        public static IDbDataParameter AddReturnParameter(this IDbCommand cmd, string name)
        {
            var param = cmd.CreateParameter();
            param.ParameterName = name;
            param.Direction = ParameterDirection.ReturnValue;
            cmd.Parameters.Add(param);

            return param;
        }
    }
}