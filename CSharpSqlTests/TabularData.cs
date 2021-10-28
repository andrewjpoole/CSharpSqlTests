using System;
using System.Collections.Generic;
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

        //private static Func<object, string> sqlObjectValuesToMarkdownStringValue = sqlValue =>
        //{
        //    if (DBNull.Value == sqlValue)
        //        return "null";

        //    return sqlValue switch
        //    {
        //        "" => "emptyString",
        //        null => "null",
        //        _ => sqlValue.ToString().Trim()
        //    };
        //};

        public static TabularData FromMarkdownTableString(string tableString)
        {
            // example TabularData
            // | column1 | column2 |
            // | ------- | ------- |
            // | valueA  | valueB  |
            // | valueC  | valueD  |
            // | valueC  | null    | -> interpret as null
            // | valueC  | emptyString | -> interpret as empty string

            var tableDefinition = new TabularData();

            var rawLines = tableString.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();

            // first, foreach row, remove any instances of 1 or more spaces before a pipe, this allows the tables to be indented inline with the code
            var trimmedLines = rawLines.Select(tableDataLine => Regex.Replace(tableDataLine, "[ ]{1,}\\|", "|")).ToList();

            foreach (var column in trimmedLines[0].Split("|", StringSplitOptions.RemoveEmptyEntries).ToList())
            {
                tableDefinition.Columns.Add(column.Trim());
            }
            
            foreach (var tableDataRow in trimmedLines.Skip(2))
            {
                var row = new TabularDataRow();
                foreach (var columnValue in tableDataRow.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList())
                {                    
                    row.ColumnValues.Add(markdownStringValuesToSqlObjectValue(columnValue.Trim()));
                }
                tableDefinition.Rows.Add(row);
            }

            return tableDefinition;
        }

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

        public static TabularData FromSqlDataReader(SqlDataReader reader)
        {
            var tableData = new TabularData();

            var columns = reader.GetColumnSchema();

            foreach (var dbColumn in columns)
            {
                tableData.Columns.Add(dbColumn.ColumnName.Trim());
            }

            while (reader.Read())
            {
                var row = new TabularDataRow();
                foreach (var dbColumn in columns)
                {
                    if (dbColumn.DataTypeName == "money")
                        row.ColumnValues.Add($"{((decimal)reader[dbColumn.ColumnName]):C}");
                    else
                    {
                        var value = reader[dbColumn.ColumnName];
                        row.ColumnValues.Add(DBNull.Value == value ? null : value);
                    }
                }
                tableData.Rows.Add(row);
            }

            return tableData;
        }

        public bool IsEqualTo(string value)
        {
            string StripInsignificantStuff(string thingToStrip) =>
                thingToStrip.Replace(" ", string.Empty)
                    .Replace("\n", string.Empty)
                    .Replace("\r", string.Empty)
                    .Replace(Environment.NewLine, string.Empty)
                    .Replace("-", string.Empty);

            var significantDataFromThis = StripInsignificantStuff(ToMarkdownTableString());
            var significantDataFromValue = StripInsignificantStuff(value);

            return significantDataFromThis.Equals(significantDataFromValue);
        }

        public bool IsEqualTo(TabularData value, out List<string> differences)
        {
            differences = new List<string>();

            if (Columns.Count != value.Columns.Count) 
                differences.Add($"The number of columns is different: {Columns.Count} vs {value.Columns.Count}");

            for (int colIndex = 0; colIndex < Columns.Count; colIndex++)
            {
                var thisColumn = Columns[colIndex].Trim();
                var valueColumn = value.Columns[colIndex].Trim();

                if(thisColumn.Equals(valueColumn) == false)
                    differences.Add($"Column[{colIndex}] is different: <{thisColumn}> vs <{valueColumn}>");
            }

            if (Rows.Count != value.Rows.Count)
                differences.Add($"The number of rows is different: {Rows.Count} vs {value.Rows.Count}");

            for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
            {
                var thisRow = Rows[rowIndex];
                var valueRow = value.Rows[rowIndex];

                if (thisRow.ColumnValues.Count != valueRow.ColumnValues.Count)
                    differences.Add($"Row[{rowIndex}] has different count of RowValues: {thisRow.ColumnValues.Count} vs {valueRow.ColumnValues.Count}");
                
                for (var colValueIndex = 0; colValueIndex < thisRow.ColumnValues.Count; colValueIndex++)
                {
                    var thisColValue = thisRow.ColumnValues[colValueIndex];
                    var valueColValue = valueRow.ColumnValues[colValueIndex];

                    if (thisColValue is null && valueColValue is null)
                        continue;

                    if (thisColValue?.Equals(valueColValue) == false)
                        differences.Add($"Row[{rowIndex}].ColumnValues[{colValueIndex}] is different: <{thisColValue}> vs <{valueColValue}>");
                }
            }

            return differences.Count == 0;
        }

        public static TabularData CreateWithColumns(params string[] columns)
        {
            var tableData = new TabularData();

            foreach (var column in columns)
            {
                tableData.Columns.Add(column);
            }

            return tableData;
        }

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

    public static class ObjectExtensions 
    {
        public static bool IsNumeric(this object value)
        {
            return value is sbyte
                    || value is byte
                    || value is short
                    || value is ushort
                    || value is int
                    || value is uint
                    || value is long
                    || value is ulong
                    || value is float
                    || value is double
                    || value is decimal;
        }
    }
}