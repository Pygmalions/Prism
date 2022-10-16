using System.Reflection;
using System.Reflection.Emit;

namespace Prism.Remoting;

public delegate void DataCoder(ILGenerator code, LocalBuilder stream);

public static class DataCoderTool
{
    /// <summary>
    /// Create a data encoder from a public static method,
    /// which has parameters of the object to encode and the memory stream to use.
    /// <br/><br/>
    /// The method should has a signature like:
    /// <b>void Method(TypeOfTheObject, MemoryStream)</b>
    /// </summary>
    /// <param name="method">Public static method to create encoder from.</param>
    /// <returns>Data encoder created from this method.</returns>
    /// <exception cref="InvalidOperationException">
    /// Throw if the signature of the specified method does not satisfy the requirements.
    /// </exception>
    public static DataCoder CreateEncoderFromMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 2 || parameters[1].ParameterType != typeof(MemoryStream) || !method.IsStatic
            || method.ReturnType != typeof(void))
            throw new InvalidOperationException(
                $"Method {method.Name} of {method.DeclaringType} is not satisfied to become a encoder.");;
        return (code, stream) =>
        {
            code.Emit(OpCodes.Ldloc, stream);
            code.Emit(OpCodes.Call, method);
        };
    }

    /// <summary>
    /// Create a data decoder from a public static method,
    /// which has a parameter of memory stream and returns the decoded object.
    /// <br/><br/>
    /// The method should has a signature like:
    /// <b>TypeOfTheObject Method(MemoryStream)</b>
    /// </summary>
    /// <param name="method">Public static method to create decoder from.</param>
    /// <returns>Data decoder created from this method.</returns>
    /// <exception cref="InvalidOperationException">
    /// Throw if the signature of the specified method does not satisfy the requirements.
    /// </exception>
    public static DataCoder CreateDecoderFromMethod(MethodInfo method)
    {
        var parameters = method.GetParameters();
        if (parameters.Length != 1 || parameters[0].ParameterType != typeof(MemoryStream) ||
            method.ReturnType == typeof(void) || !method.IsStatic)
            throw new InvalidOperationException(
                $"Method {method.Name} of {method.DeclaringType} is not satisfied to become a decoder.");
        return (code, stream) =>
        {
            code.Emit(OpCodes.Ldloc, stream);
            code.Emit(OpCodes.Call, method);
        };
    }

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

    public static DataCoder CreateArrayEncoder(Type elementType, DataCoder valueEncoder)
    {
        return (code, stream) =>
        {
            var variableArray = code.DeclareLocal(elementType.MakeArrayType());
            code.Emit(OpCodes.Stloc, variableArray);
            
            // Store the length of the array.
            var variableLength = code.DeclareLocal(typeof(int));
            code.Emit(OpCodes.Ldloc, variableArray);
            code.Emit(OpCodes.Call, elementType.MakeArrayType().GetProperty("Length")!.GetMethod!);
            code.Emit(OpCodes.Stloc, variableLength);
            
            // Write the length of the array into the stream.
            code.Emit(OpCodes.Ldloc, variableLength);
            BuiltinValueCoders.IntegerEncoder(code, stream);
            
            // Initialize the index to 0.
            var variableIndex = code.DeclareLocal(typeof(int));
            code.Emit(OpCodes.Ldc_I4_0);
            code.Emit(OpCodes.Stloc, variableIndex);

            var labelBegin = code.DefineLabel();
            var labelFinish = code.DefineLabel();
            
            code.MarkLabel(labelBegin);
            
            // Jump to the finish label if index == length.
            code.Emit(OpCodes.Ldloc, variableIndex);
            code.Emit(OpCodes.Ldloc, variableLength);
            code.Emit(OpCodes.Beq, labelFinish);
            
            // Get the element.
            code.Emit(OpCodes.Ldloc, variableArray);
            code.Emit(OpCodes.Ldloc, variableIndex);
            code.Emit(OpCodes.Ldelem);

            // Encode a value into the stream.
            valueEncoder(code, stream);
            
            // index += 1;
            code.Emit(OpCodes.Ldloc, variableIndex);
            code.Emit(OpCodes.Ldc_I4_1);
            code.Emit(OpCodes.Add);
            code.Emit(OpCodes.Stloc, variableIndex);
            
            // Jump to the begin.
            code.Emit(OpCodes.Br, labelBegin);
            
            code.MarkLabel(labelFinish);
        };
    }

    public static DataCoder CreateArrayDecoder(Type elementType, DataCoder valueDecoder)
    {
        return (code, stream) =>
        {
            // Store the length of the array.
            var variableLength = code.DeclareLocal(typeof(int));
            BuiltinValueCoders.IntegerDecoder(code, stream);
            code.Emit(OpCodes.Stloc, variableLength);
            
            // Create the array.
            var variableArray = code.DeclareLocal(elementType.MakeArrayType());
            code.Emit(OpCodes.Ldloc, variableLength);
            code.Emit(OpCodes.Newarr, elementType);
            code.Emit(OpCodes.Stloc, variableArray);

            // Initialize the index to 0.
            var variableIndex = code.DeclareLocal(typeof(int));
            code.Emit(OpCodes.Ldc_I4_0);
            code.Emit(OpCodes.Stloc, variableIndex);

            var labelBegin = code.DefineLabel();
            var labelFinish = code.DefineLabel();
            
            code.MarkLabel(labelBegin);
            
            // Jump to the finish label if index == length.
            code.Emit(OpCodes.Ldloc, variableIndex);
            code.Emit(OpCodes.Ldloc, variableLength);
            code.Emit(OpCodes.Beq, labelFinish);
            
            // Decode and store the element.
            code.Emit(OpCodes.Ldloc, variableArray);
            code.Emit(OpCodes.Ldloc, variableIndex);
            valueDecoder(code, stream);
            code.Emit(OpCodes.Stelem);

            // index += 1;
            code.Emit(OpCodes.Ldloc, variableIndex);
            code.Emit(OpCodes.Ldc_I4_1);
            code.Emit(OpCodes.Add);
            code.Emit(OpCodes.Stloc, variableIndex);
            
            // Jump to the begin.
            code.Emit(OpCodes.Br, labelBegin);
            
            code.MarkLabel(labelFinish);
        };
    }
    
    /// <summary>
    /// Lazy reflection information binding for stream writing helper.
    /// </summary>
    private static readonly Lazy<MethodInfo> StreamWritingMethod = new(
        () => typeof(DataCoderTool).GetMethod(nameof(WriteToStream),
            BindingFlags.Static | BindingFlags.Public)!);
    /// <summary>
    /// Lazy reflection information binding for stream reading helper.
    /// </summary>
    private static readonly Lazy<MethodInfo> StreamReadingMethod = new(
        () => typeof(DataCoderTool).GetMethod(nameof(ReadFromStream), 
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