using System.Collections.Generic;

namespace CSharpSqlTests
{
    public class TabularDataRow
    {
        public TabularData Parent { get; }
        
        public Dictionary<string, object?> ColumnValues = new();

        public TabularDataRow(TabularData parent)
        {
            Parent = parent;
        }

        public object? this[string columnName]
        {
            get => ColumnValues[columnName];
            set => ColumnValues[columnName] = value;
        }
    }
}