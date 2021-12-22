using System;
using NUnit.Framework;

namespace CSharpSqlTests.NUnit
{
    public static class ThenExtensions
    {
        /// <summary>
        /// A method which asserts that the context's LastQueryResult property is equal to a supplied object.
        /// </summary>
        /// <param name="expected">An object containing the value to assert</param>
        public static Then TheNonReaderQueryResultShouldBe(this Then then, object expected)
        {
            Assert.True(then.Context.LastQueryResult?.Equals(expected));
            return then;
        }

        /// <summary>
        /// A method which executes a Sql scalar query and evaluates an assertion using the supplied Func.
        /// </summary>
        /// <param name="cmdText">A string containing the Sql query</param>
        /// <param name="assertionUsingQueryResult">A Func which is passed the query result and should return a bool, false if the test should fail.</param>
        public static Then TheScalarQueryIsExecuted(this Then then, string cmdText, Func<object?, bool> assertionUsingQueryResult)
        {
            then.TheScalarQueryIsExecuted(cmdText, out var returnValue);

            Assert.True(assertionUsingQueryResult(returnValue), "Func assertionUsingQueryResult did not evaluate to true");

            return then;
        }

        /// <summary>
        /// A method which executes a Sql reader query and evaluates an assertion using the supplied Func.
        /// </summary>
        /// <param name="cmdText">A string containing the Sql query</param>
        /// <param name="assertionUsingQueryResult"></param>
        public static Then TheReaderQueryIsExecuted(this Then then, string cmdText, Func<TabularData, bool> assertionUsingQueryResult)
        {
            then.TheReaderQueryIsExecuted(cmdText, out var returnValue);

            Assert.True(assertionUsingQueryResult(returnValue), "Func assertionUsingQueryResult did not evaluate to true");

            return then;
        }
    }
}