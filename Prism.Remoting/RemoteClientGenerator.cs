using System.Reflection;
using System.Reflection.Emit;
using Prism.Remoting.Builtin;

namespace Prism.Remoting;

/// <summary>
/// A remote proxy client should not been effected by other proxy plugins,
/// for its responsibility is only to pack the invocation data and unpack the returning value.
/// This static class is basically a function-restricted version of Prism proxy generator,
/// it will only override remote methods with data operating and transporting.
/// <br/><br/>
/// Currently, async methods are not supported.
/// </summary>
public class RemoteClientGenerator
{
    /// <summary>
    /// Coder provider for this generator to use.
    /// </summary>
    public ICoderProvider CoderProvider { get; set; } = CoderManager.Global;
    
    private void ApplyEncoder(Type dataType, ILGenerator code, LocalBuilder stream)
    {
        CoderProvider.GetEncoder(dataType)(code, stream);
    }

    private void ApplyDecoder(Type dataType, ILGenerator code, LocalBuilder stream)
    {
        CoderProvider.GetDecoder(dataType)(code, stream);
    }
    
    /// <summary>
    /// Module of emitted remote clients.
    /// </summary>
    private readonly ModuleBuilder _module;

    public RemoteClientGenerator(string? assemblyName = null, string? moduleName = null)
    {
        assemblyName ??= Assembly.GetCallingAssembly().GetName().Name + ".Prism";
        moduleName ??= "RemoteClients";
        _module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run)
            .DefineDynamicModule(moduleName);
    }
    
    /// <summary>
    /// Cache of created proxies.
    /// </summary>
    private readonly Dictionary<Type, Type> _proxies = new();

    private static FieldBuilder ImplementInterface(TypeBuilder builder)
    {
        builder.AddInterfaceImplementation(typeof(IRemoteClient));
        var transporterField = 
            builder.DefineField("_Prism_Remote_Transporter", typeof(ITransporter), FieldAttributes.Family);
        
        var transporterGetterMethod = builder.DefineMethod("_Prism_Remote_GetTransporter",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual,
            CallingConventions.Standard,
            typeof(ITransporter), Type.EmptyTypes);
        builder.DefineMethodOverride(transporterGetterMethod,
            typeof(IRemoteClient).GetProperty(nameof(IRemoteClient.Transporter))!.GetMethod!);
        var getterCode = transporterGetterMethod.GetILGenerator();
        
        getterCode.Emit(OpCodes.Ldarg_0);
        getterCode.Emit(OpCodes.Ldfld, transporterField);
        getterCode.Emit(OpCodes.Ret);
        
        var transporterSetterMethod = builder.DefineMethod("_Prism_Remote_SetTransporter",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual,
            CallingConventions.Standard,
            typeof(void), new []{typeof(ITransporter)});
        builder.DefineMethodOverride(transporterSetterMethod,
            typeof(IRemoteClient).GetProperty(nameof(IRemoteClient.Transporter))!.SetMethod!);
        var setterCode = transporterSetterMethod.GetILGenerator();
        
        setterCode.Emit(OpCodes.Ldarg_0);
        setterCode.Emit(OpCodes.Ldarg_1);
        setterCode.Emit(OpCodes.Stfld, transporterField);
        setterCode.Emit(OpCodes.Ret);
        
        return transporterField;
    }
    
    private static void ImplementConstructor(TypeBuilder builder, FieldInfo? transporter)
    {
        var code = builder.DefineConstructor(
                MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | 
                MethodAttributes.Public, CallingConventions.Standard, 
                new []{typeof(ITransporter)}
                )
            .GetILGenerator();

        if (!builder.BaseType!.IsInterface)
        {
            var baseConstructor = builder.BaseType!.GetConstructor(new[] { typeof(ITransporter) });
            if (baseConstructor == null || baseConstructor.IsPrivate)
                baseConstructor = builder.BaseType!.GetConstructor(Type.EmptyTypes);
        
            if (baseConstructor == null || baseConstructor.IsPrivate)
                throw new InvalidOperationException(
                    $"Can not generate remote client for {builder.BaseType}, " +
                    $"for it has no accessible constructor with a {nameof(ITransporter)} parameter, nor parameterless one.");
        
            // Load 'this' to the stack.
            code.Emit(OpCodes.Ldarg_0);
            if (baseConstructor.GetParameters().Length > 0)
                code.Emit(OpCodes.Ldarg_1);
            // Call base constructor.
            code.Emit(OpCodes.Call, baseConstructor);
        }
        
        if (transporter != null)
        {
            // Bind transporter.
            code.Emit(OpCodes.Ldarg_0);
            code.Emit(OpCodes.Ldarg_1);
            code.Emit(OpCodes.Stfld, transporter);
        }

        code.Emit(OpCodes.Ret);
    }

    private void GenerateRemoteMethod(TypeBuilder builder, MethodInfo baseMethod)
    {
        var parameters = baseMethod.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        var proxyMethod = builder.DefineMethod(
            $"_Prism_Remote_{baseMethod.Name}",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual,
            CallingConventions.Standard, baseMethod.ReturnType, parameters);
        builder.DefineMethodOverride(
            proxyMethod, baseMethod);

        var code = proxyMethod.GetILGenerator();

        // Prepare memory stream.
        var variableStream = code.DeclareLocal(typeof(MemoryStream));
        code.Emit(OpCodes.Newobj, typeof(MemoryStream).GetConstructor(Type.EmptyTypes)!);
        code.Emit(OpCodes.Stloc, variableStream);

        // Push the meta token of the method into the stream.
        code.Emit(OpCodes.Ldc_I4, baseMethod.MetadataToken);
        ApplyEncoder(typeof(int), code, variableStream);
        
        // Encode parameters into invocation data package.
        for (var parameterIndex = 0; parameterIndex < parameters.Length; ++parameterIndex)
        {
            // Skip parameter 'this'.
            code.Emit(OpCodes.Ldarg, parameterIndex + 1);
            ApplyEncoder(parameters[parameterIndex], code, variableStream);
        }

        // Get transporter.
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Callvirt, 
            typeof(IRemoteClient).GetProperty(nameof(IRemoteClient.Transporter))!.GetMethod!);

        // Get invocation data.
        code.Emit(OpCodes.Ldloc, variableStream);
        code.Emit(OpCodes.Call, typeof(MemoryStream).GetMethod(nameof(MemoryStream.ToArray))!);
        
        // Transport invocation data and get return value data.
        code.Emit(OpCodes.Callvirt, typeof(ITransporter).GetMethod(nameof(ITransporter.Transport))!);

        // Re-initialize the stream.
        code.Emit(OpCodes.Newobj, typeof(MemoryStream).GetConstructor(new [] {typeof(byte[])})!);
        code.Emit(OpCodes.Stloc, variableStream);
        
        // Decode return value.
        if (baseMethod.ReturnType != typeof(void))
            ApplyDecoder(baseMethod.ReturnType, code, variableStream);
        
        code.Emit(OpCodes.Ret);
    }
    
    private Type GenerateRemoteClient(Type baseClass)
    {
        if (baseClass.GetCustomAttribute<CustomRemoteAttribute>() is {} attribute)
            return attribute.RemoteClient ?? baseClass;
        
        var builder = _module.DefineType(baseClass.Name + "RemoteClient",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
            TypeAttributes.BeforeFieldInit);
        builder.SetParent(baseClass);

        FieldBuilder? transporter = null;

        if (!baseClass.IsAssignableTo(typeof(ITransporter)))
            transporter = ImplementInterface(builder);
        
        ImplementConstructor(builder, transporter);

        var enableMethodCheck = !baseClass.IsInterface && baseClass.GetCustomAttribute<RemoteAttribute>() == null;
        
        foreach (var method in baseClass.GetMethods())
        {
            if (enableMethodCheck && method.GetCustomAttribute<RemoteAttribute>() == null)
                continue;
            if (!method.IsAbstract && !method.IsVirtual)
                throw new Exception("Remote methods must be virtual or abstract.");
            GenerateRemoteMethod(builder, method);
        }

        return builder.CreateType() ?? 
               throw new Exception($"Failed to generate remote client for {baseClass}.");
    }

    public Type GetClient(Type proxiedClass)
    {
        if (!_proxies.TryGetValue(proxiedClass, out var proxyClass))
        {
            proxyClass = GenerateRemoteClient(proxiedClass);
            _proxies[proxiedClass] = proxyClass;
        }

        return proxyClass;
    }
    
    public object New(Type proxiedClass, ITransporter transporter)
    {
        return Activator.CreateInstance(GetClient(proxiedClass), transporter) ?? 
               throw new Exception($"Failed to instantiate remote proxy for {proxiedClass}.");;
    }
}