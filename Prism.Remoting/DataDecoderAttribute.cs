namespace Prism.Remoting;

[AttributeUsage(AttributeTargets.Field)]
public class DataDecoderAttribute : Attribute
{
    public readonly Type DataType;

    public DataDecoderAttribute(Type dataType)
    {
        DataType = dataType;
    }
}