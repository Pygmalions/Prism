using System.Reflection;
using System.Reflection.Emit;
using Prism.Framework;
using Prism.Framework.Builders;

namespace Prism.Remoting;

public class RemotingPlugin : RemoteGenerator, IProxyPlugin
{
    public void Modify(ClassContext context)
    {
        context.Builder.AddInterfaceImplementation(typeof(IRemoteServer));
        
        var handlerMethod = context.Builder.DefineMethod("_Prism_HandleInvocation",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual,
            CallingConventions.Standard,
            typeof(Memory<byte>), new[] { typeof(Memory<byte>) });
        context.Builder.DefineMethodOverride(handlerMethod,
            typeof(IRemoteServer).GetMethod(nameof(IRemoteServer.HandleInvocation))!);
        var code = handlerMethod.GetILGenerator();

        // Collect all remote callable methods, organize them with their meta data token.
        var methods = new List<(MethodInfo Method, Label Target)>();

        foreach (var member in context.GetTriggerMember(typeof(RemoteAttribute)))
        {
            if (member is not MethodInfo method)
                continue;
            methods.Add((method, code.DefineLabel()));
        }

        // Construct a stream from the invocation data.
        var variableStream = code.DeclareLocal(typeof(MemoryStream));
        code.Emit(OpCodes.Ldloc_1);
        code.Emit(OpCodes.Newobj, typeof(MemoryStream).GetConstructor(new []{typeof(byte[])})!);
        code.Emit(OpCodes.Stloc, variableStream);

        // Decode integer method token.
        var variableToken = code.DeclareLocal(typeof(int));
        ApplyDecoder(typeof(int), code, variableStream);
        code.Emit(OpCodes.Stloc, variableToken);

        // Generate code of method selecting branches.
        foreach (var (method, label) in methods)
        {
            code.Emit(OpCodes.Ldloc, variableToken);
            code.Emit(OpCodes.Ldc_I4, method.MetadataToken);
            code.Emit(OpCodes.Beq, label);
        }

        var labelReturning = code.DefineLabel();
        
        // Generate code of each method branch.
        foreach (var (method, label) in methods)
        {
            code.MarkLabel(label);

            // Load 'this' onto the stack.
            code.Emit(OpCodes.Ldloc_0);
            
            // Decode parameters from data package.
            foreach (var parameter in method.GetParameters())
                ApplyDecoder(parameter.ParameterType, code, variableStream);
            
            // Invoke method.
            code.Emit(OpCodes.Callvirt, method);
            
            // Dispose the stream.
            code.Emit(OpCodes.Ldloc, variableStream);
            code.Emit(OpCodes.Call, typeof(MemoryStream).GetMethod(nameof(MemoryStream.Dispose))!);
            // Create an empty stream to store returning value.
            code.Emit(OpCodes.Newobj, typeof(MemoryStream).GetConstructor(Type.EmptyTypes)!);
            code.Emit(OpCodes.Stloc, variableStream);
            
            // Encode returning value.
            if (method.ReturnType != typeof(void))
                ApplyEncoder(method.ReturnType, code, variableStream);
            
            // Jump to returning process.
            code.Emit(OpCodes.Br, labelReturning);
        }

        code.MarkLabel(labelReturning);
        
        // Generate a byte array form the stream.
        code.Emit(OpCodes.Ldloc, variableStream);
        code.Emit(OpCodes.Call, typeof(MemoryStream).GetMethod(nameof(MemoryStream.ToArray))!);
        // Dispose the stream.
        code.Emit(OpCodes.Ldloc, variableStream);
        code.Emit(OpCodes.Call, typeof(MemoryStream).GetMethod(nameof(MemoryStream.Dispose))!);
        code.Emit(OpCodes.Ret);
    }
}