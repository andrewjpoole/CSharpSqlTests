namespace CSharpSqlTests;

public static class ObjectExtensions 
{
    public static bool IsNumeric(this object value)
    {
        return value is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal;
    }
}