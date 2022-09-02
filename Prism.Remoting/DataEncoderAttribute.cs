namespace Prism.Remoting;

[AttributeUsage(AttributeTargets.Field)]
public class DataEncoderAttribute : Attribute
{
    public readonly Type DataType;

    public DataEncoderAttribute(Type dataType)
    {
        DataType = dataType;
    }
}