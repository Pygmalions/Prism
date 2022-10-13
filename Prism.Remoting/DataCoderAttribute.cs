namespace Prism.Remoting;

[AttributeUsage(AttributeTargets.Field)]
internal class DataCoderAttribute : Attribute
{
    public enum CoderType
    {
        Encoder,
        Decoder
    }

    public readonly CoderType Category;
    
    public readonly Type DataType;

    public DataCoderAttribute(Type dataType, CoderType category)
    {
        DataType = dataType;
        Category = category;
    }
}