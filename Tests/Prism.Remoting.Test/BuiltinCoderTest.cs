using System;
using System.IO;
using System.Reflection.Emit;
using NUnit.Framework;

namespace Prism.Remoting.Test;

public class BuiltinCoderTest
{
    public static Func<TType, byte[]> CreateEncoderDelegate<TType>(DataCoder coder)
    {
        var method = new DynamicMethod($"{typeof(TType)}Encoder", typeof(byte[]), new[] { typeof(TType) });
        var code = method.GetILGenerator();
        
        var stream = code.DeclareLocal(typeof(MemoryStream));
        code.Emit(OpCodes.Newobj, typeof(MemoryStream).GetConstructor(Type.EmptyTypes)!);
        code.Emit(OpCodes.Stloc, stream);
        
        code.Emit(OpCodes.Ldarg_0);

        coder(code, stream);
        
        code.Emit(OpCodes.Ldloc, stream);
        code.Emit(OpCodes.Call, typeof(MemoryStream).GetMethod(nameof(MemoryStream.ToArray))!);
        code.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<TType, byte[]>>();
    }
    
    public static Func<byte[], TType> CreateDecoderDelegate<TType>(DataCoder coder)
    {
        var method = new DynamicMethod($"{typeof(TType)}Decoder", typeof(TType), new[] { typeof(byte[]) });
        var code = method.GetILGenerator();
        
        var stream = code.DeclareLocal(typeof(MemoryStream));
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Newobj, typeof(MemoryStream).GetConstructor(new []{typeof(byte[])})!);
        code.Emit(OpCodes.Stloc, stream);

        coder(code, stream);

        code.Emit(OpCodes.Ret);

        return method.CreateDelegate<Func<byte[], TType>>();
    }

    [Test]
    public void IntegerEncoderTest()
    {
        var encoder = CreateEncoderDelegate<int>(BuiltinValueCoders.IntegerEncoder);
        var result = encoder(1234);
        Assert.NotNull(result);
        Assert.AreEqual(1234, BitConverter.ToInt32(result, 0));
    }
    
    [Test]
    public void IntegerDecoderTest()
    {
        var decoder = CreateDecoderDelegate<int>(BuiltinValueCoders.IntegerDecoder);
        var result = decoder(BitConverter.GetBytes(1234));
        Assert.AreEqual(1234, result);
    }

    [Test]
    public void FloatEncoderTest()
    {
        var encoder = CreateEncoderDelegate<float>(BuiltinValueCoders.FloatEncoder);
        const float value = 0.1234f;
        var result = encoder(value);
        Assert.NotNull(result);
        Assert.AreEqual(value, BitConverter.ToSingle(result, 0));
    }
    
    [Test]
    public void FloatDecoderTest()
    {
        var decoder = CreateDecoderDelegate<float>(BuiltinValueCoders.FloatDecoder);
        const float value = 0.1234f;
        var result = decoder(BitConverter.GetBytes(value));
        Assert.AreEqual(value, result);
    }

    [Test]
    public void StringCoderTest()
    {
        const string text = "Hello world";
        
        var encoder = CreateEncoderDelegate<string>(BuiltinValueCoders.StringEncoder);
        var encodingResult = encoder(text);
        Assert.NotNull(encodingResult);

        var decoder = CreateDecoderDelegate<string>(BuiltinValueCoders.StringDecoder);
        var decodingResult = decoder(encodingResult);
        Assert.AreEqual(text, decodingResult);
    }
}