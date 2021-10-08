using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace CSharpSqlTests
{
    public class TableDefinition
    {
        public List<string> Columns = new();
        public List<TableRowDefinition> Rows = new();

        public static TableDefinition FromMarkdownTableString(string tableString)
        {
            // example TableDefinition
            // | column1 | column2 |
            // | ------- | ------- |
            // | valueA  | valueB  |
            // | valueC  | valueD  |
            // | valueC  | [dbnull]| -> interpret as null
            // | valueC  |         | -> interpret as empty string

            var tableDefinition = new TableDefinition();

            var rawLines = tableString.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).ToList();

            // first, foreach row, remove any instances of 1 or more spaces before a pipe, this allows the tables to be indented inline with the code
            var trimmedLines = rawLines.Select(tableDataLine => Regex.Replace(tableDataLine, "[ ]{1,}\\|", "|")).ToList();

            foreach (var column in trimmedLines[0].Split("|", StringSplitOptions.RemoveEmptyEntries).ToList())
            {
                tableDefinition.Columns.Add(column.Trim());
            }
            
            foreach (var tableDataRow in trimmedLines.Skip(2))
            {
                var row = new TableRowDefinition();
                foreach (var columnValue in tableDataRow.Split("|", StringSplitOptions.RemoveEmptyEntries).ToList())
                {                    
                    row.ColumnValues.Add(columnValue.Trim() switch 
                    {
                        "emptyString" => string.Empty,
                        "null" => null,
                        _ => columnValue.Trim()
                    });
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
                    var isNumeric = decimal.TryParse(columnValue, out _);
                    var invertedComma = isNumeric ? "" : "'";

                    var comma = firstInLoop ? "" : ",";
                    var columnValueString = columnValue is not null ? columnValue.ToString() : "null";
                    sb.Append($"{comma}{invertedComma}{columnValueString}{invertedComma}");

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
                sbRows.Append("\n|");
                foreach (var columnValue in row.ColumnValues)
                {
                    sbRows.Append($" {columnValue} |");
                }
            }

            return $"{sbColumnNames}\n{sbColumnAlignment}{sbRows}";
        }

        public static TableDefinition FromSqlDataReader(SqlDataReader reader)
        {
            var tableData = new TableDefinition();

            var columns = reader.GetColumnSchema();

            foreach (var dbColumn in columns)
            {
                tableData.Columns.Add(dbColumn.ColumnName);
            }

            while (reader.Read())
            {
                var row = new TableRowDefinition();
                foreach (var dbColumn in columns)
                {
                    if (dbColumn.DataTypeName == "money")
                        row.ColumnValues.Add($" {((decimal)reader[dbColumn.ColumnName]):C}");
                    else
                    {
                        var value = reader[dbColumn.ColumnName];
                        var stringValue = value switch 
                        {
                            "" => "emptyString",
                            DBNull => "null",
                            _ => value.ToString()
                        };
                        row.ColumnValues.Add($" {stringValue}");
                    }
                }
                tableData.Rows.Add(row);
            }

            return tableData;
        }

        public bool IsEqualTo(string value)
        {
            Func<string, string> stripInsignificantStuff = value => value
                .Replace(" ", string.Empty)
                .Replace("\n", string.Empty)
                .Replace("\r", string.Empty)
                .Replace(Environment.NewLine, string.Empty)
                .Replace("-", string.Empty);

            var significantDataFromThis = stripInsignificantStuff(ToMarkdownTableString());
            var significantDataFromValue = stripInsignificantStuff(value);

            return significantDataFromThis.Equals(significantDataFromValue);
        }

        public static TableDefinition CreateWithColumns(params string[] columns)
        {
            var tableData = new TableDefinition();

            foreach (var column in columns)
            {
                tableData.Columns.Add(column);
            }

            return tableData;
        }

        public TableDefinition AddRowWithValues(params string[] columnValues)
        {
            if (columnValues.Length != Columns.Count)
                throw new Exception($"Incorrect number of column values supplied, should be {Columns.Count}");

            var newRow = new TableRowDefinition();
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