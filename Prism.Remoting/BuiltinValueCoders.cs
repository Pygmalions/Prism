using System.Buffers;
using System.IO.Pipelines;
using System.Reflection.Emit;
using System.Text;
using System.Reflection;

namespace Prism.Remoting;

public static class BuiltinValueCoders
{
    private static readonly MethodInfo PipelineWriterMethod =
        typeof(BuiltinValueCoders).GetMethod(nameof(WriteToPipeline))!;
    
    private static readonly MethodInfo PipelineReaderMethod = 
        typeof(BuiltinValueCoders).GetMethod(nameof(ReadFromPipeline))!;
    
    private static void WriteToPipeline(PipeWriter writer, byte[] data)
    {
        writer.Write(data);
    }
    
    private static byte[] ReadFromPipeline(PipeReader reader, int length)
    {
        var result = reader.ReadAtLeastAsync(length);
        while (!result.IsCompleted)
            Thread.Yield();
        var sequence = result.Result.Buffer.Slice(0, length);
        reader.AdvanceTo(sequence.Start, sequence.End);
        return sequence.ToArray();
    }

    private static DataEncoder CreateValueEncoder<TValue>()
    {
        return (code, writer) =>
        {
            var data = code.DeclareLocal(typeof(byte[]));
            code.Emit(OpCodes.Call, 
                typeof(BitConverter).GetMethod(nameof(BitConverter.GetBytes), new [] {typeof(TValue)})!);
            code.Emit(OpCodes.Stloc, data);
            
            code.Emit(OpCodes.Ldloc, writer);
            code.Emit(OpCodes.Ldloc, data);
            code.Emit(OpCodes.Call, PipelineWriterMethod);
        };
    }
    
    private static DataDecoder CreateValueDecoder(string method, int size)
    {
        return (code, reader) =>
        {
            code.Emit(OpCodes.Ldloc, reader);
            code.Emit(OpCodes.Ldc_I4, size);
            code.Emit(OpCodes.Call, PipelineReaderMethod);
            code.Emit(OpCodes.Ldc_I4_0);
            code.Emit(OpCodes.Call, 
                typeof(BitConverter).GetMethod(method, new [] {typeof(byte[]), typeof(int)})!);
        };
    }

    [DataCoder(typeof(short))]
    public static readonly DataEncoder ShortEncoder = CreateValueEncoder<short>();

    [DataCoder(typeof(short))]
    public static readonly DataDecoder ShortDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToInt16), sizeof(short));

    [DataCoder(typeof(int))]
    public static readonly DataEncoder IntegerEncoder = CreateValueEncoder<int>();
    
    [DataCoder(typeof(int))]
    public static readonly DataDecoder IntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToInt32), sizeof(int));
    
    [DataCoder(typeof(long))]
    public static readonly DataEncoder LongIntegerEncoder = CreateValueEncoder<long>();
    
    [DataCoder(typeof(long))]
    public static readonly DataDecoder LongIntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToInt64), sizeof(long));
    
    [DataCoder(typeof(ushort))]
    public static readonly DataEncoder UnsignedShortEncoder = CreateValueEncoder<ushort>();

    [DataCoder(typeof(ushort))]
    public static readonly DataDecoder UnsignedShortDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToUInt16), sizeof(ushort));

    [DataCoder(typeof(uint))]
    public static readonly DataEncoder UnsignedIntegerEncoder = CreateValueEncoder<uint>();
    
    [DataCoder(typeof(uint))]
    public static readonly DataDecoder UnsignedIntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToUInt32), sizeof(uint));
    
    [DataCoder(typeof(ulong))]
    public static readonly DataEncoder UnsignedLongIntegerEncoder = CreateValueEncoder<ulong>();
    
    [DataCoder(typeof(ulong))]
    public static readonly DataDecoder UnsignedLongIntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToUInt64), sizeof(ulong));

    [DataCoder(typeof(float))]
    public static readonly DataEncoder FloatEncoder = CreateValueEncoder<float>();
    
    [DataCoder(typeof(float))]
    public static readonly DataDecoder FloatDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToSingle), sizeof(float));
    
    [DataCoder(typeof(double))]
    public static readonly DataEncoder DoubleEncoder = CreateValueEncoder<double>();
    
    [DataCoder(typeof(double))]
    public static readonly DataDecoder DoubleDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToDouble), sizeof(double));
    
    [DataCoder(typeof(char))]
    public static readonly DataEncoder CharEncoder = CreateValueEncoder<char>();
    
    [DataCoder(typeof(char))]
    public static readonly DataDecoder CharDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToChar), sizeof(char));

    [DataCoder(typeof(byte))] 
    public static readonly DataEncoder ByteEncoder = (code, writer) =>
    {
        var data = code.DeclareLocal(typeof(byte));
        code.Emit(OpCodes.Stloc, data);
        
        code.Emit(OpCodes.Ldc_I4_1);
        code.Emit(OpCodes.Newarr, typeof(byte));
        code.Emit(OpCodes.Dup);
        code.Emit(OpCodes.Ldc_I4_0);
        code.Emit(OpCodes.Ldloc, data);
        code.Emit(OpCodes.Stelem);
        
        code.Emit(OpCodes.Ldloc, writer);
        code.Emit(OpCodes.Call, PipelineWriterMethod);
    };
    
    [DataCoder(typeof(byte))]
    public static readonly DataDecoder ByteDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToChar), sizeof(char));

    
    [DataCoder(typeof(string))]
    public static readonly DataEncoder StringEncoder = (code, writer) =>
    {
        var text = code.DeclareLocal(typeof(string));
        code.Emit(OpCodes.Stloc, text);
        
        // Write the string length into the data stream.
        code.Emit(OpCodes.Ldloc, writer);
        code.Emit(OpCodes.Ldloc, text);
        code.Emit(OpCodes.Call, typeof(string).GetProperty(nameof(string.Length))!.GetMethod!);
        code.Emit(OpCodes.Call, 
            typeof(BitConverter).GetMethod(nameof(BitConverter.GetBytes), new [] {typeof(int)})!);
        code.Emit(OpCodes.Call, PipelineWriterMethod);
        
        // Write the string bytes into the data stream.
        code.Emit(OpCodes.Ldloc, writer);
        code.Emit(OpCodes.Call, typeof(Encoding).GetProperty(nameof(Encoding.UTF8))!.GetMethod!);
        code.Emit(OpCodes.Ldloc, text);
        code.Emit(OpCodes.Call, 
            typeof(Encoding).GetMethod(nameof(Encoding.GetBytes), new [] {typeof(string)})!);
        code.Emit(OpCodes.Call, PipelineWriterMethod);
    };
    
    [DataCoder(typeof(string))]
    public static readonly DataDecoder StringDecoder = (code, reader) =>
    {
        // Get the length of the text.
        var length = code.DeclareLocal(typeof(int));
        code.Emit(OpCodes.Ldloc, reader);
        code.Emit(OpCodes.Ldc_I4, sizeof(int));
        code.Emit(OpCodes.Call, PipelineReaderMethod);
        code.Emit(OpCodes.Ldc_I4_0);
        code.Emit(OpCodes.Call, 
            typeof(BitConverter).GetMethod(nameof(BitConverter.ToInt32), new [] {typeof(byte[]), typeof(int)})!);
        code.Emit(OpCodes.Stloc, length);
        
        code.Emit(OpCodes.Call, typeof(Encoding).GetProperty(nameof(Encoding.UTF8))!.GetMethod!);
        code.Emit(OpCodes.Ldloc, reader);
        code.Emit(OpCodes.Ldloc,length);
        code.Emit(OpCodes.Call, PipelineReaderMethod);
        code.Emit(OpCodes.Call, 
            typeof(Encoding).GetMethod(nameof(Encoding.GetString), new [] {typeof(byte[])})!);
    };
}