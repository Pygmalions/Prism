using System.Reflection.Emit;
using System.Text;

namespace Prism.Remoting;

/// <summary>
/// This static class provides coder implementations of basic value types.
/// This coder set will automatically enabled in remote proxy generators by default,
/// including client generator and server generator.
/// </summary>
public static class BuiltinValueCoders
{
    private static DataCoder CreateValueEncoder<TValue>()
    {
        return (code, stream) =>
        {
            code.Emit(OpCodes.Call, 
                typeof(BitConverter).GetMethod(nameof(BitConverter.GetBytes), new [] {typeof(TValue)})!);
            code.Emit(OpCodes.Ldloc, stream);
            code.Emit(OpCodes.Call, DataCoderTool.StreamWritingHelper);
        };
    }
    
    private static DataCoder CreateValueDecoder(string method, int size)
    {
        return (code, stream) =>
        {
            code.Emit(OpCodes.Ldc_I4, size);
            code.Emit(OpCodes.Ldloc, stream);
            code.Emit(OpCodes.Call, DataCoderTool.StreamReadingHelper);
            code.Emit(OpCodes.Ldc_I4_0); // Start from index 0.
            code.Emit(OpCodes.Call, 
                typeof(BitConverter).GetMethod(method, new [] {typeof(byte[]), typeof(int)})!);
        };
    }

    [DataCoder(typeof(short), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder ShortEncoder = CreateValueEncoder<short>();

    [DataCoder(typeof(short), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder ShortDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToInt16), sizeof(short));

    [DataCoder(typeof(int), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder IntegerEncoder = CreateValueEncoder<int>();
    
    [DataCoder(typeof(int), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder IntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToInt32), sizeof(int));
    
    [DataCoder(typeof(long), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder LongIntegerEncoder = CreateValueEncoder<long>();
    
    [DataCoder(typeof(long), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder LongIntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToInt64), sizeof(long));
    
    [DataCoder(typeof(ushort), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder UnsignedShortEncoder = CreateValueEncoder<ushort>();

    [DataCoder(typeof(ushort), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder UnsignedShortDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToUInt16), sizeof(ushort));

    [DataCoder(typeof(uint), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder UnsignedIntegerEncoder = CreateValueEncoder<uint>();
    
    [DataCoder(typeof(uint), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder UnsignedIntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToUInt32), sizeof(uint));
    
    [DataCoder(typeof(ulong), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder UnsignedLongIntegerEncoder = CreateValueEncoder<ulong>();
    
    [DataCoder(typeof(ulong), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder UnsignedLongIntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToUInt64), sizeof(ulong));

    [DataCoder(typeof(float), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder FloatEncoder = CreateValueEncoder<float>();
    
    [DataCoder(typeof(float), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder FloatDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToSingle), sizeof(float));
    
    [DataCoder(typeof(double), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder DoubleEncoder = CreateValueEncoder<double>();
    
    [DataCoder(typeof(double), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder DoubleDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToDouble), sizeof(double));
    
    [DataCoder(typeof(char), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder CharEncoder = CreateValueEncoder<char>();
    
    [DataCoder(typeof(char), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder CharDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToChar), sizeof(char));

    [DataCoder(typeof(byte), DataCoderAttribute.CoderType.Encoder)] 
    public static readonly DataCoder ByteEncoder = (code, stream) =>
    {
        var data = code.DeclareLocal(typeof(byte));
        code.Emit(OpCodes.Stloc, data);
        
        code.Emit(OpCodes.Ldc_I4_1);
        code.Emit(OpCodes.Newarr, typeof(byte));
        code.Emit(OpCodes.Dup);
        code.Emit(OpCodes.Ldc_I4_0);
        code.Emit(OpCodes.Ldloc, data);
        code.Emit(OpCodes.Stelem);
        
        code.Emit(OpCodes.Ldloc, stream);
        code.Emit(OpCodes.Call, DataCoderTool.StreamWritingHelper);
    };
    
    [DataCoder(typeof(byte), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder ByteDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToChar), sizeof(char));

    
    [DataCoder(typeof(string), DataCoderAttribute.CoderType.Encoder)]
    public static readonly DataCoder StringEncoder = (code, stream) =>
    {
        var text = code.DeclareLocal(typeof(string));
        code.Emit(OpCodes.Stloc, text);
        
        // Write the string length into the data stream.
        code.Emit(OpCodes.Ldloc, text);
        code.Emit(OpCodes.Call, typeof(string).GetProperty(nameof(string.Length))!.GetMethod!);
        code.Emit(OpCodes.Call, 
            typeof(BitConverter).GetMethod(nameof(BitConverter.GetBytes), new [] {typeof(int)})!);
        code.Emit(OpCodes.Ldloc, stream);
        code.Emit(OpCodes.Call, DataCoderTool.StreamWritingHelper);
        
        // Write the string bytes into the data stream.
        code.Emit(OpCodes.Call, typeof(Encoding).GetProperty(nameof(Encoding.UTF8))!.GetMethod!);
        code.Emit(OpCodes.Ldloc, text);
        code.Emit(OpCodes.Call, 
            typeof(Encoding).GetMethod(nameof(Encoding.GetBytes), new [] {typeof(string)})!);
        code.Emit(OpCodes.Ldloc, stream);
        code.Emit(OpCodes.Call, DataCoderTool.StreamWritingHelper);
    };
    
    [DataCoder(typeof(string), DataCoderAttribute.CoderType.Decoder)]
    public static readonly DataCoder StringDecoder = (code, stream) =>
    {
        // Get the length of the text.
        var length = code.DeclareLocal(typeof(int));
        code.Emit(OpCodes.Ldc_I4, sizeof(int));
        code.Emit(OpCodes.Ldloc, stream);
        code.Emit(OpCodes.Call, DataCoderTool.StreamReadingHelper);
        code.Emit(OpCodes.Ldc_I4_0);
        code.Emit(OpCodes.Call, 
            typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt32), new [] {typeof(byte[]), typeof(int)})!);
        code.Emit(OpCodes.Stloc, length);
        
        code.Emit(OpCodes.Call, typeof(Encoding).GetProperty(nameof(Encoding.UTF8))!.GetMethod!);
        code.Emit(OpCodes.Ldloc,length);
        code.Emit(OpCodes.Ldloc, stream);
        code.Emit(OpCodes.Call, DataCoderTool.StreamReadingHelper);
        code.Emit(OpCodes.Call, 
            typeof(Encoding).GetMethod(nameof(Encoding.GetString), new [] {typeof(byte[])})!);
    };
}