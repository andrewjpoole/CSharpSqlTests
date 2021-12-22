using NUnit.Framework;

namespace CSharpSqlTests.NUnit
{
    public static class TabularDataExtensions
    {
        /// <summary>
        /// A method which asserts that this TabularData contains a supplied TabularData.
        /// </summary>
        /// <param name="comparisonData">The TabularData that should be contained within this TabularData for the assertion to succeed.</param>
        public static void AssertContains(this TabularData tabularData, TabularData comparisonData)
        {
            Assert.True(tabularData.Contains(comparisonData, out var diffs), $"{diffs}");
        }

        /// <summary>
        /// A method which asserts that this TabularData contains a supplied TabularData.
        /// </summary>
        /// <param name="comparisonData">A string representing a TabularData that should be contained within this TabularData for the assertion to succeed.
        /// <example>
        /// <code>
        ///  | Id | Type | Make | Model   |
        ///  | -- | ---- | ---- | ------- |
        ///  | 1  | Car  | Fiat | 500     |
        ///  | 2  | Van  | Ford | Transit |
        /// this string represents a table with 4 columns and 2 rows.
        /// 
        /// A string value in a column of: 
        /// 2021-11-03  -> will be interpreted as a DateTime, use any parsable date and time string
        /// 234         -> will be interpreted as an int
        /// null        -> will be interpreted as null
        /// emptyString -> will be interpreted as an empty string
        /// true        -> will be interpreted as boolean true
        /// false       -> will be interpreted as boolean false
        /// </code>
        /// </example>
        /// </param>
        public static void AssertContains(this TabularData tabularData, string comparisonData)
        {
            Assert.True(tabularData.Contains(comparisonData, out var diffs), $"{diffs}");
        }

        /// <summary>
        /// A method which asserts that this TabularData is equal to a supplied TabularData.
        /// </summary>
        /// <param name="comparisonData">The TabularData that should be equal to this TabularData for the assertion to succeed.</param>
        public static void AssertEquals(this TabularData tabularData, TabularData comparisonData)
        {
            Assert.True(tabularData.IsEqualTo(comparisonData, out var diffs), $"{diffs}");
        }

        /// <summary>
        /// A method which asserts that this TabularData is equal to a supplied TabularData.
        /// </summary>
        /// <param name="comparisonData">The TabularData that should be equal to this TabularData for the assertion to succeed.
        /// <example>
        /// <code>
        ///  | Id | Type | Make | Model   |
        ///  | -- | ---- | ---- | ------- |
        ///  | 1  | Car  | Fiat | 500     |
        ///  | 2  | Van  | Ford | Transit |
        /// this string represents a table with 4 columns and 2 rows.
        /// 
        /// A string value in a column of: 
        /// 2021-11-03  -> will be interpreted as a DateTime, use any parsable date and time string
        /// 234         -> will be interpreted as an int
        /// null        -> will be interpreted as null
        /// emptyString -> will be interpreted as an empty string
        /// true        -> will be interpreted as boolean true
        /// false       -> will be interpreted as boolean false
        /// </code>
        /// </example>
        /// </param>
        public static void AssertEquals(this TabularData tabularData, string comparisonData)
        {
            Assert.True(tabularData.IsEqualTo(comparisonData, out var diffs), $"{diffs}");
        }
    }
}