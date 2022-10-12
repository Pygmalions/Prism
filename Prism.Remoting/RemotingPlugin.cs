using System.Reflection;
using Prism.Framework;
using Prism.Framework.Builders;

namespace Prism.Remoting;

public abstract class RemotingPlugin<TCoder> : IProxyPlugin
{
    public abstract void Modify(ClassContext context);

    protected RemotingPlugin()
    {
        // Enable built-in value coders by default.
        AddCoderProvider(typeof(BuiltinValueCoders));
    }

    protected readonly Dictionary<Type, TCoder> Coders = new();

    public void AddCoder(Type dataType, TCoder coder)
        => Coders[dataType] = coder;

    public void RemoveCoder(Type dataType)
        => Coders.Remove(dataType);

    public void AddCoderProvider(Type provider)
    {
        foreach (var field in provider.GetFields())
        {
            if (!field.FieldType.IsAssignableTo(typeof(TCoder)))
                continue;
            if (field.GetCustomAttribute<DataCoderAttribute>() is not { } attribute)
                continue;
            if (!field.IsStatic || field.GetValue(null) is not TCoder translator)
                throw new InvalidOperationException(
                    "Only static DataDecoder field is allowed to be auto discovered.");
            AddCoder(attribute.DataType, translator);
        }
    }

    public void RemoveCoderProvider(Type provider)
    {
        foreach (var field in provider.GetFields())
        {
            if (!field.FieldType.IsAssignableTo(typeof(TCoder)))
                continue;
            if (field.GetCustomAttribute<DataCoderAttribute>() is not { } attribute)
                continue;
            if (!field.IsStatic || field.GetValue(null) is not TCoder coder)
                throw new InvalidOperationException(
                    "Only static DataDecoder field is allowed to be auto discovered.");
            if (Coders.TryGetValue(attribute.DataType, out var foundCoder) && coder.Equals(foundCoder))
                Coders.Remove(attribute.DataType);
        }
    }
}