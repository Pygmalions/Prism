namespace Prism.Remoting;

[AttributeUsage(AttributeTargets.Field)]
internal class DataCoderAttribute : Attribute
{
    public readonly Type DataType;

    public DataCoderAttribute(Type dataType)
    {
        DataType = dataType;
    }
}