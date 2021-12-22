using System.Data.Common;

namespace CSharpSqlTests.FrameworkTests
{
    public class TestColumn : DbColumn
    {
        public TestColumn(string name)
        {
            ColumnName = name;
        }
    }
}