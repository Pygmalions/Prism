using System.Buffers;
using System.IO.Pipelines;
using System.Reflection.Emit;
using System.Text;
using System.IO.Pipes;
using Prism.Framework;

namespace Prism.Remoting;

public static class ValueTranslator
{
    private static DataEncoder CreateValueEncoder<TValue>()
    {
        return (code, input, output) =>
        {
            code.Emit(OpCodes.Call, 
                typeof(BitConverter).GetMethod(nameof(BitConverter.GetBytes), new [] {typeof(TValue)})!);
            code.Emit(OpCodes.Newobj, 
                typeof(ReadOnlySpan<byte>).GetConstructor(new [] {typeof(byte[])})!);
            code.Emit(OpCodes.Call, typeof(PipeWriter).GetMethod(nameof(PipeWriter.)));   
            code.Emit(OpCodes.Stloc, output);
            var pipe = new Pipe();
            var writer = pipe.Writer;
            writer.Write(new ReadOnlySpan<byte>(BitConverter.GetBytes(3)));
        };
    }
    
    private static DataDecoder CreateValueDecoder(string method)
    {
        return (code, input, output) =>
        {
            code.Emit(OpCodes.Ldloc, input);
            code.Emit(OpCodes.Call, 
                typeof(BitConverter).GetMethod(method, new [] {typeof(ReadOnlySpan<byte>)})!);
            code.Emit(OpCodes.Stloc, output);
        };
    }

    [Translator(typeof(short), typeof(PipeWriter))]
    public static readonly Translator ShortEncoder = CreateValueEncoder<short>();

    [Translator(typeof(PipeReader), typeof(short))]
    public static readonly Translator ShortDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToInt16));

    [Translator(typeof(int), typeof(PipeWriter))]
    public static readonly Translator IntegerEncoder = CreateValueEncoder<int>();
    
    [Translator(typeof(byte[]), typeof(int))]
    public static readonly Translator IntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToInt32));
    
    [Translator(typeof(long), typeof(PipeWriter))]
    public static readonly Translator LongIntegerEncoder = CreateValueEncoder<long>();
    
    [Translator(typeof(byte[]), typeof(long))]
    public static readonly Translator LongIntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToInt64));
    
    [Translator(typeof(ushort), typeof(PipeWriter))]
    public static readonly Translator UnsignedShortEncoder = CreateValueEncoder<ushort>();

    [Translator(typeof(byte[]), typeof(ushort))]
    public static readonly Translator UnsignedShortDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToUInt16));

    [Translator(typeof(uint), typeof(PipeWriter))]
    public static readonly Translator UnsignedIntegerEncoder = CreateValueEncoder<uint>();
    
    [Translator(typeof(byte[]), typeof(uint))]
    public static readonly Translator UnsignedIntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToUInt32));
    
    [Translator(typeof(ulong), typeof(PipeWriter))]
    public static readonly Translator UnsignedLongIntegerEncoder = CreateValueEncoder<ulong>();
    
    [Translator(typeof(byte[]), typeof(ulong))]
    public static readonly Translator UnsignedLongIntegerDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToUInt64));
    
    [Translator(typeof(Half), typeof(PipeWriter))]
    public static readonly Translator HalfFloatEncoder = CreateValueEncoder<Half>();
    
    [Translator(typeof(byte[]), typeof(Half))]
    public static readonly Translator HalfFloatDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToHalf));
    
    [Translator(typeof(float), typeof(PipeWriter))]
    public static readonly Translator FloatEncoder = CreateValueEncoder<float>();
    
    [Translator(typeof(byte[]), typeof(float))]
    public static readonly Translator FloatDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToSingle));
    
    [Translator(typeof(double), typeof(PipeWriter))]
    public static readonly Translator DoubleEncoder = CreateValueEncoder<double>();
    
    [Translator(typeof(byte[]), typeof(double))]
    public static readonly Translator DoubleDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToDouble));
    
    [Translator(typeof(char), typeof(PipeWriter))]
    public static readonly Translator CharEncoder = CreateValueEncoder<char>();
    
    [Translator(typeof(byte[]), typeof(char))]
    public static readonly Translator CharDecoder = 
        CreateValueDecoder(nameof(BitConverter.ToChar));

    [Translator(typeof(string), typeof(PipeWriter))]
    public static readonly Translator StringEncoder = (code, input, output) =>
    {
        code.Emit(OpCodes.Call, typeof(Encoding).GetProperty(nameof(Encoding.UTF8))!.GetMethod!);
        code.Emit(OpCodes.Ldloc, input);
        code.Emit(OpCodes.Call, 
            typeof(Encoding).GetMethod(nameof(Encoding.GetBytes), new [] {typeof(string)})!);
        code.Emit(OpCodes.Stloc, output);
    };
    
    [Translator(typeof(byte[]), typeof(string))]
    public static readonly Translator StringDecoder = (code, input, output) =>
    {
        code.Emit(OpCodes.Call, typeof(Encoding).GetProperty(nameof(Encoding.UTF8))!.GetMethod!);
        code.Emit(OpCodes.Ldloc, input);
        code.Emit(OpCodes.Call, 
            typeof(Encoding).GetMethod(nameof(Encoding.GetString), new [] {typeof(ReadOnlySpan<byte>)})!);
        code.Emit(OpCodes.Stloc, output);
    };
}