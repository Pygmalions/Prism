using System.Buffers;
using System.Reflection;
using System.Reflection.Emit;
using Prism.Framework;
using Prism.Framework.Builders;

namespace Prism.Decorating;

[TriggerBy(typeof(DecorateAttribute))]
public class DecorationPlugin : IProxyPlugin
{
    public void Modify(ClassContext context)
    {
        var builder = context.Builder;
        builder.AddInterfaceImplementation(typeof(IDecorated));
        
        var proxyManager = context.Builder.DefineField("_Prism_MethodProxies",
            typeof(Dictionary<MethodInfo, MethodDecorator>), FieldAttributes.Private);

        // Implement decorated object interface.
        ImplementInterface(context, proxyManager);
        
        var proxies = new List<(FieldBuilder proxy, MethodInfo method)>();
        var proxyId = 0;

        void ProcessMethod(MethodInfo method)
        {
            var methodContext = context.GetMethodContext(method);
            if (methodContext == null)
                return;
            var proxy = context.Builder.DefineField(
                $"_Prism_Decorator_{proxyId++}_{method.Name}", typeof(MethodDecorator),
                FieldAttributes.Private);
            proxies!.Add((proxy, method));
            GenerateMethod(methodContext, proxy);
        }
        
        foreach (var member in context.GetTriggerMember(typeof(DecorateAttribute)))
        {
            switch (member)
            {
                case MethodInfo method:
                    ProcessMethod(method);
                    break;
                case PropertyInfo property:
                    if (property.GetMethod != null)
                        ProcessMethod(property.GetMethod);
                    if (property.SetMethod != null)
                        ProcessMethod(property.SetMethod);
                    break;
            }
        }
        GenerateInitializer(context, proxyManager, proxies);
    }

    private static void ImplementInterface(ClassContext context, FieldInfo proxyManager)
    {
        if (proxyManager == null) throw new ArgumentNullException(nameof(proxyManager));
        context.Builder.AddInterfaceImplementation(typeof(IDecorated));
        
        var proxyGetterMethod = context.Builder.DefineMethod("_Prism_GetMethodProxy",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual,
            CallingConventions.Standard,
            typeof(MethodDecorator), new[] { typeof(MethodInfo) });
        context.Builder.DefineMethodOverride(proxyGetterMethod,
            typeof(IDecorated).GetMethod(nameof(IDecorated.GetMethodDecorator))!);
        var code = proxyGetterMethod.GetILGenerator();
        
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Ldfld, proxyManager);
        code.Emit(OpCodes.Ldarg_1);
        code.Emit(OpCodes.Call,
            typeof(IDecorated.PrismHelper).GetMethod(nameof(IDecorated.PrismHelper.GetMethodDecorator))!);
        code.Emit(OpCodes.Ret);
    }

    private static void GenerateInitializer(ClassContext context, FieldInfo proxyManager,
        List<(FieldBuilder proxy, MethodInfo method)> proxies)
    {
        var code = context.Initializer.GetILGenerator();
        
        // Initialize proxy manager.
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Newobj, typeof(Dictionary<MethodInfo, MethodDecorator>).GetConstructor(Type.EmptyTypes)!);
        code.Emit(OpCodes.Stfld, proxyManager);

        var variableMethodHandle = code.DeclareLocal(typeof(MethodBase));
        var variableProxy = code.DeclareLocal(typeof(MethodDecorator));
        var variableHolderType = code.DeclareLocal(typeof(Type));

        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Call,
            typeof(object).GetMethod(nameof(GetType))!);
        code.Emit(OpCodes.Stloc, variableHolderType);

        foreach (var (proxy, method) in proxies)
        {
            // Get method reflection handle.
            code.Emit(OpCodes.Ldtoken, method);
            code.Emit(OpCodes.Call, 
                typeof(MethodBase).GetMethod(nameof(MethodBase.GetMethodFromHandle), 
                    new [] {typeof(RuntimeMethodHandle)})!);
            code.Emit(OpCodes.Stloc, variableMethodHandle);
        
            // Construct a method proxy.
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldloc, variableMethodHandle);
            code.Emit(OpCodes.Newobj,
                typeof(MethodDecorator).GetConstructor(new []{typeof(object), typeof(MethodInfo)})!);
            code.Emit(OpCodes.Stloc, variableProxy);
        
            // Initialize the method proxy field.
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldloc, variableProxy);
            code.Emit(OpCodes.Stfld, proxy);
        
            // Add proxy to the proxy manager.
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, proxyManager);
            code.Emit(OpCodes.Ldloc, variableMethodHandle);
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldfld, proxy);
            code.Emit(OpCodes.Call, 
                typeof(IDecorated.PrismHelper).GetMethod(nameof(IDecorated.PrismHelper.SetMethodDecorator))!);
        }
    }

    private static void GenerateMethod(MethodContext builder, FieldInfo proxy)
    {
        LocalBuilder? variableInvocation = null;
        LocalBuilder variableArguments = null!;
        builder.InsertAfterInvoking((ref CodeContext context) => 
            GenerateCodeBeforeInvoking(ref context, out variableInvocation, proxy, out variableArguments));
        builder.InsertAfterInvoking((ref CodeContext context) => 
            GenerateCodeAfterInvoking(ref context, variableInvocation, proxy, variableArguments));
    }

    private static void GenerateCodeBeforeInvoking(
        ref CodeContext context, out LocalBuilder? variableInvocation, FieldInfo proxy, out LocalBuilder variableArguments)
    {
        var code = context.Code;
        variableInvocation = code.DeclareLocal(typeof(Invocation));

        variableArguments = code.DeclareLocal(typeof(object?[]));

        // Construct arguments array.
        var methodParameters = context.Method.GetParameters();
        code.Emit(OpCodes.Call,
            typeof(ArrayPool<object?>).
                GetProperty(nameof(ArrayPool<object?>.Shared))!.GetMethod!);
        code.Emit(OpCodes.Ldc_I4, methodParameters.Length);
        code.Emit(OpCodes.Callvirt, typeof(ArrayPool<object?>)
            .GetMethod(nameof(ArrayPool<object?>.Rent))!);
        
        for (var parameterIndex = 0; parameterIndex < methodParameters.Length; ++parameterIndex)
        {
            code.Emit(OpCodes.Dup);
            code.Emit(OpCodes.Ldc_I4, parameterIndex);
            // Exclude the parameter 0, which is the pointer "this".
            code.Emit(OpCodes.Ldarg, parameterIndex + 1);
            if (methodParameters[parameterIndex].ParameterType.IsValueType)
                code.Emit(OpCodes.Box, methodParameters[parameterIndex].ParameterType);
            code.Emit(OpCodes.Stelem_Ref);
        }
        code.Emit(OpCodes.Stloc, variableArguments);

        // Construct invocation context.
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Ldfld, proxy);
        code.Emit(OpCodes.Ldloc, variableArguments);
        code.Emit(OpCodes.Call, typeof(IDecorated.PrismHelper)
            .GetMethod(nameof(IDecorated.PrismHelper.CreateInvocationContext))!);
        code.Emit(OpCodes.Stloc, variableInvocation);

        // Trigger invoking event.
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Ldfld, proxy);
        code.Emit(OpCodes.Ldloca, variableInvocation);
        code.Emit(OpCodes.Call, typeof(IDecorated.PrismHelper)
            .GetMethod(nameof(IDecorated.PrismHelper.TriggerMethodDecoratorInvokingEvent))!);

        // Update skipping flag.
        code.Emit(OpCodes.Ldloca, variableInvocation);
        code.Emit(OpCodes.Call, 
            typeof(Invocation).GetProperty(nameof(Invocation.Skipped))!.GetMethod!);
        code.Emit(OpCodes.Stloc, context.Skipped);

        // Copy result form the invocation context into the local variable.
        if (context.Result != null)
        {
            var labelAfterUploadResult = code.DefineLabel();
        
            var variableContextResult = code.DeclareLocal(typeof(object));
            
            // Load returning value from the invocation context.
            code.Emit(OpCodes.Ldloca, variableInvocation);
            code.Emit(OpCodes.Call, 
                typeof(Invocation).GetProperty(nameof(Invocation.Result))!.GetMethod!);
            code.Emit(OpCodes.Stloc, variableContextResult);
            
            // Check context result.
            code.Emit(OpCodes.Ldloc, variableContextResult);
            code.Emit(OpCodes.Brfalse, labelAfterUploadResult);
            
            // Overwrite the result variable with the context result.
            code.Emit(OpCodes.Ldloc, variableContextResult);
            if (context.Method.ReturnType.IsValueType)
                code.Emit(OpCodes.Unbox_Any, context.Method.ReturnType);
            code.Emit(OpCodes.Stloc, context.Result);
            
            code.MarkLabel(labelAfterUploadResult);
        }
        
        // Check stop flag.
        code.Emit(OpCodes.Ldloca, variableInvocation);
        code.Emit(OpCodes.Call, 
            typeof(Invocation).GetProperty(nameof(Invocation.Stopped))!.GetMethod!);
        var labelAfterStop = code.DefineLabel();
        code.Emit(OpCodes.Brfalse, labelAfterStop);
        if (context.Result != null)
            code.Emit(OpCodes.Ldloc, context.Result);
        // Return the rented arguments array if the invocation is early stopped.
        InsertReturningArgumentArray(code, variableArguments);
        code.Emit(OpCodes.Ret);
        code.MarkLabel(labelAfterStop);
    }

    private static void InsertReturningArgumentArray(ILGenerator code, LocalBuilder variableArguments)
    {
        code.Emit(OpCodes.Call,
            typeof(ArrayPool<object?>).
                GetProperty(nameof(ArrayPool<object?>.Shared))!.GetMethod!);
        code.Emit(OpCodes.Ldloc, variableArguments);
        code.Emit(OpCodes.Ldc_I4_1);
        code.Emit(OpCodes.Callvirt, typeof(ArrayPool<object?>)
            .GetMethod(nameof(ArrayPool<object?>.Return))!);
    }
    
    private static void GenerateCodeAfterInvoking(
        ref CodeContext context, LocalBuilder? variableInvocation, FieldInfo proxy, LocalBuilder variableArguments)
    {
        if (variableInvocation == null)
            throw new Exception("Object proxy plugin pre-invoking code action has not been invoked.");

        var code = context.Code;
        // Copy the result from the local variable to the invocation context.
        if (context.Result != null)
        {
            code.Emit(OpCodes.Ldloca, variableInvocation);
            code.Emit(OpCodes.Ldloc, context.Result);
            if (context.Method.ReturnType.IsValueType)
                code.Emit(OpCodes.Box, context.Method.ReturnType);
            code.Emit(OpCodes.Call, 
                typeof(Invocation).GetProperty(nameof(Invocation.Result))!.SetMethod!);
        }
        
        // Trigger invoked event.
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Ldfld, proxy);
        code.Emit(OpCodes.Ldloca, variableInvocation);
        code.Emit(OpCodes.Call, 
            typeof(IDecorated.PrismHelper).GetMethod(nameof(IDecorated.PrismHelper.TriggerMethodDecoratorInvokedEvent))!);
        
        if (context.Result == null) return;
        
        // Copy the result from the invocation context to the local variable.
        var labelAfterDownloadResult = code.DefineLabel();

        var variableContextResult = code.DeclareLocal(typeof(object));
            
        // Load returning value from the invocation context.
        code.Emit(OpCodes.Ldloca, variableInvocation);
        code.Emit(OpCodes.Call, 
            typeof(Invocation).GetProperty(nameof(Invocation.Result))!.GetMethod!);
        code.Emit(OpCodes.Stloc, variableContextResult);
            
        // Check context result, and skip the downloading procedure if it is null.
        code.Emit(OpCodes.Ldloc, variableContextResult);
        code.Emit(OpCodes.Brfalse, labelAfterDownloadResult);
            
        // Overwrite the result variable with the context result.
        code.Emit(OpCodes.Ldloc, variableContextResult);
        if (context.Method.ReturnType.IsValueType)
            code.Emit(OpCodes.Unbox_Any, context.Method.ReturnType);
        code.Emit(OpCodes.Stloc, context.Result);
        
        code.MarkLabel(labelAfterDownloadResult);
        
        // Return the rented argument array.
        InsertReturningArgumentArray(code, variableArguments);
    }
}