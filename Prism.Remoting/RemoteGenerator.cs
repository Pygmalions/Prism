using System.Reflection;
using System.Reflection.Emit;

namespace Prism.Remoting;

public abstract class RemoteGenerator
{
    /// <summary>
    /// Registered data encoders.
    /// </summary>
    private readonly Dictionary<Type, DataCoder> _encoders = new();
    
    /// <summary>
    /// Registered data decoders.
    /// </summary>
    private readonly Dictionary<Type, DataCoder> _decoders = new();

    public void AddEncoder(Type dataType, DataCoder coder)
        => _encoders[dataType] = coder;
    
    public void AddDecoder(Type dataType, DataCoder coder)
        => _decoders[dataType] = coder;

    public void RemoveEncoder(Type dataType, DataCoder? target = null)
    {
        if (target != null && _encoders.TryGetValue(dataType, out var encoder) && encoder != target)
            return;
        _encoders.Remove(dataType);
    }
    
    public void RemoveDecoder(Type dataType, DataCoder? target = null)
    {
        if (target != null && _decoders.TryGetValue(dataType, out var decoder) && decoder != target)
            return;
        _decoders.Remove(dataType);
    }

    public void AddCoderProvider(Type provider)
    {
        foreach (var field in provider.GetFields())
        {
            if (!field.FieldType.IsAssignableTo(typeof(DataCoder)))
                continue;
            if (field.GetCustomAttribute<DataCoderAttribute>() is not { } attribute)
                continue;
            if (!field.IsStatic || field.GetValue(null) is not DataCoder coder)
                throw new InvalidOperationException(
                    "Only static data coder field is allowed to be auto discovered.");
            if (attribute.Category == DataCoderAttribute.CoderType.Encoder)
                AddEncoder(attribute.DataType, coder);
            else 
                AddDecoder(attribute.DataType, coder);
        }
    }

    public void RemoveCoderProvider(Type provider)
    {
        foreach (var field in provider.GetFields())
        {
            if (!field.FieldType.IsAssignableTo(typeof(DataCoder)))
                continue;
            if (field.GetCustomAttribute<DataCoderAttribute>() is not { } attribute)
                continue;
            if (!field.IsStatic || field.GetValue(null) is not DataCoder coder)
                throw new InvalidOperationException(
                    "Only static data coder field is allowed to be auto discovered.");
            if (attribute.Category == DataCoderAttribute.CoderType.Encoder)
                RemoveEncoder(attribute.DataType, coder);
            else 
                RemoveDecoder(attribute.DataType, coder);
        }
    }

    protected DataCoder RegisterCustomEncoder(Type dataType, CustomCoderAttribute attribute)
    {
        var method = dataType.GetMethod(attribute.EncoderName, BindingFlags.Public | BindingFlags.Static)
                     ?? throw new Exception(
                         "Can not find the public static encoder method" +
                         $" {attribute.EncoderName} in {dataType}.");
        var coder = DataCoderTool.CreateDecoderFromMethod(method);
        _encoders[dataType] = coder;
        return coder;
    }
    
    protected DataCoder RegisterCustomDecoder(Type dataType, CustomCoderAttribute attribute)
    {
        var method = dataType.GetMethod(attribute.DecoderName, BindingFlags.Public | BindingFlags.Static)
                     ?? throw new Exception(
                         "Can not find the public static decoder method" +
                         $" {attribute.DecoderName} in {dataType}.");
        var coder = DataCoderTool.CreateDecoderFromMethod(method);
        _decoders[dataType] = coder;
        return coder;
    }

    protected DataCoder RegisterArrayEncoder(Type dataType)
    {
        if (dataType.GetElementType() is not {} elementType)
            throw new InvalidOperationException($"The specified data type {dataType} is not an array type.");
        var coder = DataCoderTool.CreateArrayEncoder(elementType, GetEncoder(elementType));
        _encoders[dataType] = coder;
        return coder;
    }
    
    protected DataCoder RegisterArrayDecoder(Type dataType)
    {
        if (dataType.GetElementType() is not {} elementType)
            throw new InvalidOperationException($"The specified data type {dataType} is not an array type.");
        var coder = DataCoderTool.CreateArrayDecoder(elementType, GetDecoder(elementType));
        _decoders[dataType] = coder;
        return coder;
    }

    public DataCoder GetEncoder(Type dataType)
    {
        if (_encoders.TryGetValue(dataType, out var encoder))
            return encoder;
        if (dataType.GetCustomAttribute<CustomCoderAttribute>() is { } attribute)
            return RegisterCustomEncoder(dataType, attribute) ??
                throw new InvalidOperationException(
                    "Can not find a suitable public static encoder method" +
                    $" on {dataType} which is marked with a {nameof(CustomCoderAttribute)}.");
        if (dataType.IsArray)
            return RegisterArrayEncoder(dataType);
        return dataType.IsGenericType switch
        {
            true when dataType.GetGenericTypeDefinition() == typeof(Task<>) => 
                DataCoderTool.CreateTaskEncoder(dataType.GetGenericArguments()[0], 
                    GetEncoder(dataType.GetGenericArguments()[0])),
            true when dataType.GetGenericTypeDefinition() == typeof(ValueTask<>) =>
                DataCoderTool.CreateValueTaskEncoder(dataType.GetGenericArguments()[0], 
                    GetEncoder(dataType.GetGenericArguments()[0])),
            _ => throw new InvalidOperationException($"Missing data encoder for {dataType}.")
        };
    }

    public DataCoder GetDecoder(Type dataType)
    {
        if (_decoders.TryGetValue(dataType, out var decoder))
            return decoder;
        if (dataType.GetCustomAttribute<CustomCoderAttribute>() is { } attribute)
            return RegisterCustomDecoder(dataType, attribute) ??
                throw new InvalidOperationException(
                    "Can not find a suitable public static decoder method" +
                    $" on {dataType} which is marked with a {nameof(CustomCoderAttribute)}.");
        if (dataType.IsArray)
            return RegisterArrayDecoder(dataType);
        return dataType.IsGenericType switch
        {
            true when dataType.GetGenericTypeDefinition() == typeof(Task<>) => 
                DataCoderTool.CreateTaskDecoder(dataType.GetGenericArguments()[0], 
                    GetDecoder(dataType.GetGenericArguments()[0])),
            true when dataType.GetGenericTypeDefinition() == typeof(ValueTask<>) =>
                DataCoderTool.CreateValueTaskDecoder(dataType.GetGenericArguments()[0], 
                    GetDecoder(dataType.GetGenericArguments()[0])),
            _ => throw new InvalidOperationException($"Missing data decoder for {dataType}.")
        };
    }

    protected void ApplyEncoder(Type dataType, ILGenerator code, LocalBuilder stream)
    {
        GetEncoder(dataType)(code, stream);
    }

    protected void ApplyDecoder(Type dataType, ILGenerator code, LocalBuilder stream)
    {
        GetDecoder(dataType)(code, stream);
    }
    
    protected RemoteGenerator()
    {
        // Enable built-in value coders by default.
        AddCoderProvider(typeof(BuiltinValueCoders));
    }
}