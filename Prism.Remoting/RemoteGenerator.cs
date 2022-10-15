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

    protected void ApplyEncoder(Type data, ILGenerator code, LocalBuilder stream)
    {
        if (!_encoders.TryGetValue(data, out var encoder))
            throw new InvalidOperationException($"Missing data encoder for {data}.");    
        encoder(code, stream);
    }

    protected void ApplyDecoder(Type data, ILGenerator code, LocalBuilder stream)
    {
        if (!_decoders.TryGetValue(data, out var decoder))
            throw new InvalidOperationException($"Missing data decoder for {data}.");    
        decoder(code, stream);
    }
    
    protected RemoteGenerator()
    {
        // Enable built-in value coders by default.
        AddCoderProvider(typeof(BuiltinValueCoders));
    }
}