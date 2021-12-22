using System;
using System.Data;
// ReSharper disable UnusedMember.Local

namespace CSharpSqlTests
{
    public class When
    {
        public readonly ILocalDbTestContext Context;

        public When(ILocalDbTestContext context)
        {
            Context = context;
        }
        
        public static When UsingThe(ILocalDbTestContext context) => new When(context);

        public When And => this;
        
        /// <summary>
        /// A method which executes a stored procedure and returns the result as an object.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure to execute</param>
        /// <param name="returnValue">The return value as an object</param>
        /// <param name="parameters">A param array where each param has a string name and an object value.</param>
        public When TheStoredProcedureIsExecutedWithReturnParameter(string storedProcedureName, out object? returnValue, params (string Name, object Value)[] parameters)
        {
            TheStoredProcedureIsExecutedWithReturnParameter(storedProcedureName, parameters);
            returnValue = Context.LastQueryResult;

            return this;
        }

        /// <summary>
        /// A method which executes a stored procedure and stores the result on the context's LastQueryResult property.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">A param array where each param has a string name and an object value.</param>
        public When TheStoredProcedureIsExecutedWithReturnParameter(string storedProcedureName, params (string Name, object Value)[] parameters)
        {
            var cmd = Context.SqlConnection.CreateCommand();
            cmd.CommandText = storedProcedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Transaction = Context.SqlTransaction;

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

            Context.LastQueryResult = returnParameter.Value;

            return this;
        }

        /// <summary>
        /// A method which executes a stored procedure that doesn't return anything i.e. an insert.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure to execute</param>
        /// <param name="affectedRows">An int containing the number of rows affected by the stored procedure.</param>
        /// <param name="parameters">A param array where each param has a string name and an object value.</param>
        public When TheStoredProcedureIsExecuted(string storedProcedureName, out int affectedRows, params (string Name, object Value)[] parameters)
        {
            var cmd = Context.SqlConnection.CreateCommand();
            cmd.CommandText = storedProcedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Transaction = Context.SqlTransaction;

            foreach (var (name, value) in parameters)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = name;
                param.Value = value;
                cmd.Parameters.Add(param);
            }
            
            affectedRows = cmd.ExecuteNonQuery();

            return this;
        }

        /// <summary>
        /// A method which executes a stored procedure and returns the resulting DataReader on the context's LastQueryResult property.
        /// </summary>
        /// <param name="storedProcedureName">The name of the stored procedure to execute</param>
        /// <param name="parameters">A param array where each param has a string name and an object value.</param>
        public When TheStoredProcedureIsExecutedWithReader(string storedProcedureName, params (string Name, object Value)[] parameters)
        {
            var cmd = Context.SqlConnection.CreateCommand();
            cmd.CommandText = storedProcedureName;
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Transaction = Context.SqlTransaction;

            foreach (var (name, value) in parameters)
            {
                var param = cmd.CreateParameter();
                param.ParameterName = name;
                param.Value = value;
                cmd.Parameters.Add(param);
            }
            
            Context.LastQueryResult = cmd.ExecuteReader();
            
            return this;
        }

        /// <summary>
        /// A method which executes a Sql scalar query and stores the result on the context's LastQueryResult property as an object.
        /// </summary>
        /// <param name="cmdText">The Sql query to execute</param>
        /// <param name="returnValue">The return value as an object</param>
        public When TheScalarQueryIsExecuted(string cmdText, out object? returnValue)
        {
            TheScalarQueryIsExecuted(cmdText);
            returnValue = Context.LastQueryResult;

            return this;
        }

        /// <summary>
        /// A method which executes a Sql scalar query and stores the result on the context's LastQueryResult property as an object.
        /// </summary>
        /// <param name="cmdText">The Sql query to execute</param>
        public When TheScalarQueryIsExecuted(string cmdText)
        {
            var cmd = Context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = Context.SqlTransaction;

            Context.LastQueryResult = cmd.ExecuteScalar();

            return this;
        }

        /// <summary>
        /// A method which executes a Sql reader query and stores the result on the context's LastQueryResult property as an IDataReader.
        /// </summary>
        /// <param name="cmdText">The Sql query to execute</param>
        /// <param name="returnValue">The return value as an IDataReader</param>
        public When TheReaderQueryIsExecuted(string cmdText, out IDataReader? returnValue)
        {
            TheReaderQueryIsExecuted(cmdText);
            returnValue = Context.LastQueryResult! as IDataReader;

            return this;
        }

        /// <summary>
        /// A method which executes a Sql reader query and stores the result on the context's LastQueryResult property as an IDataReader.
        /// </summary>
        /// <param name="cmdText">The Sql query to execute</param>
        public When TheReaderQueryIsExecuted(string cmdText)
        {
            var cmd = Context.SqlConnection.CreateCommand();
            cmd.CommandText = cmdText;
            cmd.CommandType = CommandType.Text;
            cmd.Transaction = Context.SqlTransaction;
            
            Context.LastQueryResult = cmd.ExecuteReader();

            return this;
        }
    }
}