using System.Reflection;
using System.Reflection.Emit;

namespace Prism.Remoting;

public delegate void DataCoder(ILGenerator code, LocalBuilder stream);

public static class DataCoderHelper
{
    /// <summary>
    /// Create an encoder delegate with the given encoder.
    /// This method is to help verifying the behavior of custom coders.
    /// </summary>
    /// <param name="coder">Encoder to use.</param>
    /// <typeparam name="TType">Type that this encoder can encode.</typeparam>
    /// <returns>Delegate created from the specified encoder.</returns>
    public static Action<MemoryStream, TType> CreateEncoderDelegate<TType>(DataCoder coder)
    {
        var method = new DynamicMethod($"{typeof(TType)}Encoder", typeof(void), 
            new[] { typeof(MemoryStream), typeof(TType) });
        var code = method.GetILGenerator();
        
        var stream = code.DeclareLocal(typeof(MemoryStream));
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Stloc, stream);
        
        code.Emit(OpCodes.Ldarg_1);
        coder(code, stream);
        
        code.Emit(OpCodes.Ret);

        return method.CreateDelegate<Action<MemoryStream, TType>>();
    }
    
    /// <summary>
    /// Create an decoder delegate with the given decoder.
    /// This method is to help verifying the behavior of custom coders.
    /// </summary>
    /// <param name="coder">Decoder to use.</param>
    /// <typeparam name="TType">Type that this decoder can decode.</typeparam>
    /// <returns>Delegate created from the specified decoder.</returns>
    public static Func<MemoryStream, TType> CreateDecoderDelegate<TType>(DataCoder coder)
    {
        var method = new DynamicMethod($"{typeof(TType)}Decoder", typeof(TType), 
            new[] { typeof(MemoryStream) });
        var code = method.GetILGenerator();
        
        var stream = code.DeclareLocal(typeof(MemoryStream));
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Stloc, stream);
        
        coder(code, stream);
        
        code.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<MemoryStream, TType>>();
    }
    
    /// <summary>
    /// Lazy reflection information binding for stream writing helper.
    /// </summary>
    private static readonly Lazy<MethodInfo> StreamWritingMethod = new(
        () => typeof(DataCoderHelper).GetMethod(nameof(WriteToStream),
            BindingFlags.Static | BindingFlags.Public)!);
    /// <summary>
    /// Lazy reflection information binding for stream reading helper.
    /// </summary>
    private static readonly Lazy<MethodInfo> StreamReadingMethod = new(
        () => typeof(DataCoderHelper).GetMethod(nameof(ReadFromStream), 
            BindingFlags.Static| BindingFlags.Public)!);

    /// <summary>
    /// The reflection information of the stream writing helper.
    /// </summary>
    public static MethodInfo StreamWritingHelper => StreamWritingMethod.Value;

    /// <summary>
    /// The reflection information of the stream reading helper.
    /// </summary>
    public static MethodInfo StreamReadingHelper => StreamReadingMethod.Value;
    
    /// <summary>
    /// This method helps generated code to write bytes into a stream.
    /// </summary>
    /// <param name="data">Bytes to write.</param>
    /// <param name="stream">Stream to write to.</param>
    public static void WriteToStream(byte[] data, Stream stream)
    {
        stream.Write(data);
    }

    /// <summary>
    /// This method helps generated code to read a certain amount of bytes from a stream.
    /// </summary>
    /// <param name="length">Length of bytes to read.</param>
    /// <param name="stream">Stream to read from.</param>
    /// <returns>Bytes with the specified length.</returns>
    /// <exception cref="Exception">
    /// Throw when the stream has not enough reading bytes to read.
    /// This may happens when the position is not correctly set after stream writing.
    /// </exception>
    public static byte[] ReadFromStream(int length, Stream stream)
    {
        var buffer = new byte[length];
        while (length > 0)
        {
            var count = stream.Read(buffer, buffer.Length - length, length);
            if (count == 0)
                throw new Exception(
                    $"The stream to read has not enough remaining bytes; another {length} bytes is needed.");
            length -= count;
        }
        return buffer;
    }
}