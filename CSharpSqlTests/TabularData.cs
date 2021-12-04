using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace CSharpSqlTests
{
    public class TabularData
    {
        public List<string> Columns = new();
        public List<TabularDataRow> Rows = new();

        private static Func<string, object?> markdownStringValuesToSqlObjectValue = value =>
        {
            if (DateTime.TryParse(value, out var valueAsDate))
                return valueAsDate;

            if (int.TryParse(value, out var valueAsInt))
                return valueAsInt;

            if (decimal.TryParse(value, out var valueAsDecimal))
                return valueAsDecimal;

            if (float.TryParse(value, out var valueAsFloat))
                return valueAsFloat;

            if (double.TryParse(value, out var valueAsDouble))
                return valueAsDouble;

            return value switch
            {
                "emptyString" => string.Empty,
                "null" => null,
                "true" => true,
                "false" => false,
                null => DBNull.Value,
                _ => value.Trim()
            };
        };

        /// <summary>
        /// A method which creates a TabularData from a markdown table string.
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
        /// </summary>
        public static TabularData FromMarkdownTableString(string tableString)
        {
            var tableDefinition = new TabularData();

            var rawLines = tableString.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).ToList();

            // first, foreach row, remove any instances of 1 or more spaces before a pipe, this allows the tables to be indented inline with the code
            var trimmedLines = rawLines.Select(tableDataLine => Regex.Replace(tableDataLine, "[ ]{1,}\\|", "|")).ToList();

            foreach (var column in trimmedLines[0].Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList())
            {
                tableDefinition.Columns.Add(column.Trim());
            }
            
            foreach (var tableDataRow in trimmedLines.Skip(2))
            {
                var row = new TabularDataRow();
                foreach (var columnValue in tableDataRow.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).ToList())
                {                    
                    row.ColumnValues.Add(markdownStringValuesToSqlObjectValue(columnValue.Trim()));
                }
                tableDefinition.Rows.Add(row);
            }

            return tableDefinition;
        }

        /// <summary>
        /// A method which renders and returns the contents of a TabularData as a Sql INSERT string.
        /// </summary>
        /// <param name="tableName">A string specifying the name of the Table for the INSERT statement.</param>
        public string ToSqlString(string tableName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"INSERT INTO {tableName}");
            sb.Append("(");

            var firstInLoop = true;
            foreach (var column in Columns)
            {
                var comma = firstInLoop ? "" : ",";
                sb.Append($"{comma}{column}");

                if (firstInLoop)
                    firstInLoop = false;
            }
            sb.AppendLine(")");
            
            sb.AppendLine("VALUES");

            var firstRow = true;
            foreach (var row in Rows)
            {
                var leadingComma = firstRow ? "" : ",";

                sb.Append($"{leadingComma}(");

                if (firstRow)
                    firstRow = false;

                firstInLoop = true;
                foreach (var columnValue in row.ColumnValues)
                {
                    var comma = firstInLoop ? "" : ",";

                    if (columnValue is DateTime dateTime)
                    {
                        sb.Append($"{comma}'{dateTime:s}'");
                    }
                    else 
                    {
                        var isNumeric = columnValue?.IsNumeric() ?? false;
                                        
                        var columnValueString = columnValue is not null ? columnValue.ToString() : "null";
                    
                        var invertedComma = isNumeric || columnValueString == "null" ? "" : "'";

                        sb.Append($"{comma}{invertedComma}{columnValueString}{invertedComma}");
                    }

                    if (firstInLoop)
                        firstInLoop = false;
                }

                sb.AppendLine(")");
            }

            return sb.ToString();
        }

        /// <summary>
        /// A method which renders and returns the contents of the TabularData as a markdown table string. 
        /// </summary>
        public string ToMarkdownTableString()
        {
            var sbColumnNames = new StringBuilder("|");
            var sbColumnAlignment = new StringBuilder("|");
            var sbRows = new StringBuilder();
            
            foreach (var column in Columns)
            {
                sbColumnNames.Append($" {column} |");
                sbColumnAlignment.Append(" --- |");
            }

            foreach(var row in Rows)
            {
                sbRows.Append($"{Environment.NewLine}|");
                foreach (var columnValue in row.ColumnValues)
                {
                    sbRows.Append($" {columnValue} |");
                }
            }

            return $"{sbColumnNames}{Environment.NewLine}{sbColumnAlignment}{sbRows}";
        }

        /// <summary>
        /// A method which creates a TabularData from the columns and rows returned from an IDataReader, usually the result of a call to ExceuteReader() on an IDbConnection etc. 
        /// </summary>
        public static TabularData FromSqlDataReader(IDataReader dataReader)
        {
            var tableData = new TabularData();

            var columnSchemaGen = (IDbColumnSchemaGenerator)dataReader;
            var columns = columnSchemaGen.GetColumnSchema();
            
            foreach (var dbColumn in columns)
            {
                tableData.Columns.Add(dbColumn.ColumnName.Trim());
            }

            while (dataReader.Read())
            {
                var row = new TabularDataRow();
                foreach (var dbColumn in columns)
                {
                    if (dbColumn.DataTypeName == "money")
                        row.ColumnValues.Add($"{((decimal)dataReader[dbColumn.ColumnName]):C}");
                    else
                    {
                        var value = dataReader[dbColumn.ColumnName];
                        row.ColumnValues.Add(DBNull.Value == value ? null : value);
                    }
                }
                tableData.Rows.Add(row);
            }

            dataReader.Close();

            return tableData;
        }

        /// <summary>
        /// Returns true if this current TabularData exactly matches the supplied comparisonData. i.e. The list of columns matches and the collection of rows and the column values are matches.
        /// To assert on a subset of data use Contains() instead.
        /// </summary>
        public bool IsEqualTo(TabularData comparisonData, out List<string> differences)
        {
            differences = new List<string>();

            if (Columns.Count != comparisonData.Columns.Count) 
                differences.Add($"The number of columns is different: {Columns.Count} vs {comparisonData.Columns.Count}");

            for (int colIndex = 0; colIndex < Columns.Count; colIndex++)
            {
                var thisColumn = Columns[colIndex].Trim();
                var comparisonColumn = comparisonData.Columns[colIndex].Trim();

                if(thisColumn.Equals(comparisonColumn) == false)
                    differences.Add($"Column[{colIndex}] is different: <{thisColumn}> vs <{comparisonColumn}>");
            }

            if (Rows.Count != comparisonData.Rows.Count)
                differences.Add($"The number of rows is different: {Rows.Count} vs {comparisonData.Rows.Count}");

            for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
            {
                var thisRow = Rows[rowIndex];
                var comparisonDataRow = comparisonData.Rows[rowIndex];

                if (thisRow.ColumnValues.Count != comparisonDataRow.ColumnValues.Count)
                    differences.Add($"Row[{rowIndex}] has different count of RowValues: {thisRow.ColumnValues.Count} vs {comparisonDataRow.ColumnValues.Count}");
                
                for (var colValueIndex = 0; colValueIndex < thisRow.ColumnValues.Count; colValueIndex++)
                {
                    var thisColValue = thisRow.ColumnValues[colValueIndex];
                    var comparisonColValue = comparisonDataRow.ColumnValues[colValueIndex];

                    if (thisColValue is null && comparisonColValue is null)
                        continue;

                    if (thisColValue?.Equals(comparisonColValue) == false)
                        differences.Add($"Row[{rowIndex}].ColumnValues[{colValueIndex}] is different: <{thisColValue}> vs <{comparisonColValue}>");
                }
            }

            return differences.Count == 0;
        }

        /// <summary>
        /// Returns true if this current TabularData exactly matches the supplied comparisonData. i.e. The list of columns matches and the collection of rows and the column values are matches.
        /// To assert on a subset of data use Contains() instead.
        /// </summary>
        public bool IsEqualTo(string comparisonTabularDataString, out List<string> differences)
        {
            var isEqualTo = IsEqualTo(TabularData.FromMarkdownTableString(comparisonTabularDataString), out var diffs);
            differences = diffs;
            return isEqualTo;
        }


        /// <summary>
        /// Return true if this TabularData contains at least the columns and rows in the supplied comparisonData, also return a list of any missing data
        /// i.e. this TabularData can contain more columns and/or rows, but we are only checking against the columns and rows in comparisonData.
        /// If you want an exact match use Equals() instead.
        /// </summary>
        public bool Contains(TabularData comparisonData, out List<string> differences)
        {
            differences = new List<string>();
            
            foreach (var columnName in comparisonData.Columns)
            {
                if(!Columns.Contains(columnName))
                    differences.Add($"TabularData does not contain a column named {columnName}");
            }

            if (differences.Any())
                return false;

            // To succeed we need find a row which matches all supplied comparisonData column values
            // First take a comparisonData row...
            for (var comparisonRowIndex = 0; comparisonRowIndex < comparisonData.Rows.Count; comparisonRowIndex++)
            {
                var comparisonDataRow = comparisonData.Rows[comparisonRowIndex];
                var comparisonDataRowIsSatisfied = false;

                // Then loop through our Rows...
                var columnComparisonsSatisfied = 0;
                foreach (var tabularDataRow in Rows)
                {
                    // Now loop through the columns, checking if we have a matching comparisonData
                    for (var colIndex = 0; colIndex < comparisonData.Columns.Count; colIndex++)
                    {
                        var colName = comparisonData.Columns[colIndex];
                        var tabularDataColumnIndex = Columns.IndexOf(colName);
                        
                        // Compare this column comparisonData (select based on columnName) to the supplied comparisonData
                        if (tabularDataRow.ColumnValues[tabularDataColumnIndex]!.Equals(comparisonDataRow.ColumnValues[colIndex]))
                            columnComparisonsSatisfied += 1;
                        
                    }

                    // If we have incremented columnComparisonsSatisfied to match the count of the columns in comparisonData then we have satisfied the row
                    if (columnComparisonsSatisfied == comparisonData.Columns.Count)
                        comparisonDataRowIsSatisfied = true;
                    
                }

                if (!comparisonDataRowIsSatisfied)
                    differences.Add($"TabularData does not contain a row that contains the values {string.Join(",", comparisonDataRow.ColumnValues)}");
            }

            return differences.Count == 0;
        }

        /// <summary>
        /// Return true if this TabularData contains at least the columns and rows in the supplied comparisonData, also return a list of any missing data
        /// i.e. this TabularData can contain more columns and/or rows, but we are only checking against the columns and rows in comparisonData.
        /// If you want an exact match use Equals() instead.
        /// </summary>
        public bool Contains(string comparisonTabularDataString, out List<string> differences)
        {
            var contains = Contains(TabularData.FromMarkdownTableString(comparisonTabularDataString), out var diffs);
            differences = diffs;
            return contains;
        }

        /// <summary>
        /// Static builder method for creating a TabularData, specifying a param array of string column names.
        /// </summary>
        public static TabularData CreateWithColumns(params string[] columns)
        {
            var tableData = new TabularData();

            foreach (var column in columns)
            {
                tableData.Columns.Add(column);
            }

            return tableData;
        }

        /// <summary>
        /// Method for adding a row to a TabularData, specifying the row values as a param array of objects.
        /// The param array count must equal the number of columns added to the TabularData.
        /// </summary>
        public TabularData AddRowWithValues(params object[] columnValues)
        {
            if (columnValues.Length != Columns.Count)
                throw new Exception($"Incorrect number of column values supplied, should be {Columns.Count}");

            var newRow = new TabularDataRow();
            foreach (var columnValue in columnValues)
            {
                // todo handle nulls?

                newRow.ColumnValues.Add(columnValue);
            }
            Rows.Add(newRow);

            return this;
        }
    }
}