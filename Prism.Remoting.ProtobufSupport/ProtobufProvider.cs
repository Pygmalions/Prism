using System.Reflection;
using System.Reflection.Emit;

namespace Prism.Remoting.ProtobufSupport;

public class ProtobufProvider : ICoderProvider
{
    public static void Encode<TData>(TData data, MemoryStream stream)
    {
        ProtoBuf.Serializer.Serialize(stream, data);
    }

    public static TData Decode<TData>(MemoryStream stream)
        => ProtoBuf.Serializer.Deserialize<TData>(stream);
    
    public DataCoder GetEncoder(Type dataType)
    {
        return (code, stream) =>
        {
            code.Emit(OpCodes.Ldloc, stream);
            code.Emit(OpCodes.Call, 
                typeof(ProtobufProvider).
                    GetMethod(nameof(Encode), 
                        BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(dataType));
        };
    }

    public DataCoder GetDecoder(Type dataType)
    {
        return (code, stream) =>
        {
            code.Emit(OpCodes.Ldloc, stream);
            code.Emit(OpCodes.Call, 
                typeof(ProtobufProvider).
                    GetMethod(nameof(Decode), 
                        BindingFlags.Public | BindingFlags.Static)!
                    .MakeGenericMethod(dataType));
        };
    }
}