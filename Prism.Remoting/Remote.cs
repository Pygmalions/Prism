namespace Prism.Remoting;

public static class Remote
{
    private static void PrepareRemoteProxyConstructor()
    {
        // if (context.Builder.BaseType!.GetConstructors().Select(constructor => constructor.GetParameters()).Any(parameters => parameters.Length == 1 &&
        //         parameters[0].ParameterType == typeof(ITransporter)))
        //     return;
        //
        // var code = context.Builder.DefineConstructor(
        //         MethodAttributes.HideBySig |
        //         MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Public, 
        //         CallingConventions.Standard,
        //         new [] {typeof(ITransporter)})
        //     .GetILGenerator();
        //
        // // Invoke the initializer method.
        // code.Emit(OpCodes.Ldarg_0);
        // code.Emit(OpCodes.Call, context.Initializer);
        //
        // // Load constructor arguments to the stack.
        // code.Emit(OpCodes.Ldarg_0);
        // code.Emit(OpCodes.Ldarg_1);
        // // Call base constructor.
        // code.Emit(OpCodes.Call, 
        //     typeof(IRemoteCallable).GetMethod(nameof(IRemoteCallable.Connect))!);
        // code.Emit(OpCodes.Ret);
    }
    
    public static Type GenerateProxy(Type proxiedClass)
    {
        throw new NotImplementedException();
    }
    
    
    public static TClass New<TClass>(ITransporter transporter) where TClass : class
        => Activator.CreateInstance(typeof(TClass), transporter) as TClass ??
           throw new InvalidOperationException(
               $"Class {typeof(TClass)} has no constructor with only a transporter parameter.");
}