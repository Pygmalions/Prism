using System.Collections.Concurrent;
using System.ComponentModel;
using System.Reflection;
using System.Reflection.Emit;
using Prism.Framework.Builders;

namespace Prism.Framework;

public class Generator
{
    /// <summary>
    /// Cached generated proxy classes.
    /// </summary>
    private readonly ConcurrentDictionary<Type, Type> _proxies = new();

    /// <summary>
    /// Dynamic module where all proxy classes exist.
    /// </summary>
    private readonly ModuleBuilder _module;

    public Generator(string? assemblyName = null, string? moduleName = null)
    {
        assemblyName ??= Assembly.GetCallingAssembly().GetName().Name + ".Prism";
        moduleName ??= "Proxies";
        _module = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(assemblyName), AssemblyBuilderAccess.Run)
            .DefineDynamicModule(moduleName);
    }

    /// <summary>
    /// Get the proxy class of the specified class.
    /// </summary>
    /// <param name="proxiedClass">Class type to generate proxy for.</param>
    /// <returns>Proxy class.</returns>
    /// <exception cref="InvalidOperationException">
    /// Throw if the base type is a value type, an interface, or an abstract class.
    /// </exception>
    public Type GetProxy(Type proxiedClass)
    {
        if (proxiedClass.IsValueType || proxiedClass.IsAbstract || proxiedClass.IsInterface)
            throw new InvalidOperationException(
                "Can not generate a proxy class for value types, interfaces, or abstract classes.");
        return _proxies.GetOrAdd(proxiedClass, GenerateProxy);
    }

    /// <summary>
    /// Registered proxy plugins.
    /// </summary>
    private readonly Dictionary<Type, HashSet<IProxyPlugin>> _pluginsByAttribute = new();

    /// <summary>
    /// Registered proxy plugins.
    /// </summary>
    private readonly Dictionary<Type, HashSet<IProxyPlugin>> _pluginsByType = new();

    /// <summary>
    /// Register a proxy plugin.
    /// </summary>
    /// <param name="plugin">Proxy plugin instance.</param>
    /// <exception cref="InvalidEnumArgumentException">
    /// Throw if any PluginTriggerBy mode is invalid.
    /// </exception>
    public void RegisterPlugin(IProxyPlugin plugin)
    {
        void AddToGroup(IDictionary<Type, HashSet<IProxyPlugin>> category, Type type, IProxyPlugin instance)
        {
            if (!category.TryGetValue(type, out var group))
            {
                group = new HashSet<IProxyPlugin>();
                category[type] = group;
            }

            group.Add(instance);
        }

        var added = false;
        foreach (var attribute in plugin.GetType().GetCustomAttributes<TriggerByAttribute>())
        {
            added = true;
            switch (attribute.Mode)
            {
                case TriggerMode.ByAttribute:
                    if (!attribute.Trigger.IsSubclassOf(typeof(Attribute)))
                        throw new InvalidOperationException(
                            $"Trigger type {attribute.Trigger} in attribute mode is not an attribute class.");
                    AddToGroup(_pluginsByAttribute, attribute.Trigger, plugin);
                    break;
                case TriggerMode.ByHierarchy:
                    AddToGroup(_pluginsByType, attribute.Trigger, plugin);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }

        if (!added)
            throw new InvalidOperationException(
                $"A valid plugin must have {nameof(TriggerByAttribute)} marked on it.");
    }

    /// <summary>
    /// Register a proxy plugin.
    /// </summary>
    /// <param name="plugin">Proxy plugin instance.</param>
    /// <exception cref="InvalidEnumArgumentException">
    /// Throw if any PluginTriggerBy mode is invalid.
    /// </exception>
    public void UnregisterPlugin(IProxyPlugin plugin)
    {
        void RemoveFromGroup(Dictionary<Type, HashSet<IProxyPlugin>> category, Type type, IProxyPlugin instance)
        {
            if (!category.TryGetValue(type, out var group))
                return;
            group.Remove(instance);
            if (group.Count == 0)
                category.Remove(type);
        }
        
        foreach (var attribute in plugin.GetType().GetCustomAttributes<TriggerByAttribute>())
        {
            switch (attribute.Mode)
            {
                case TriggerMode.ByAttribute:
                    RemoveFromGroup(_pluginsByAttribute, attribute.Trigger, plugin);
                    break;
                case TriggerMode.ByHierarchy:
                    RemoveFromGroup(_pluginsByType, attribute.Trigger, plugin);
                    break;
                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }
    
    private static readonly CustomAttributeBuilder GeneratedAttributeBuilder = new(
        typeof(PrismGenerationAttribute).GetConstructor(Type.EmptyTypes)!, Array.Empty<object>());
    
    /// <summary>
    /// Generate a proxy for the specific base class.
    /// </summary>
    /// <param name="baseClass">Class to generate proxy for.</param>
    /// <returns>Proxy class.</returns>
    /// <exception cref="NotImplementedException"></exception>
    private Type GenerateProxy(Type baseClass)
    {
        // Generate proxy builder context.
        var context = PrepareContext(baseClass);

        var plugins = new HashSet<IProxyPlugin>();
        
        // Summarize plugins by trigger attributes.
        foreach (var trigger in context.TriggerMembers.Keys)
        {
            if (_pluginsByAttribute.TryGetValue(trigger, out var plugin))
                plugins.UnionWith(plugin);
        }
        
        // Summarize plugins by base classes.
        foreach (var (type, plugin) in _pluginsByType)
        {
            if (baseClass.IsAssignableTo(type))
                plugins.UnionWith(plugin);
        }

        // Apply plugins.
        foreach (var plugin in plugins)
        {
            plugin.Modify(context);
        }
        
        // Complete initializer method.
        context.Initializer.GetILGenerator().Emit(OpCodes.Ret);
        
        // Generate proxy methods.
        foreach (var (_, methodContext) in context.MethodContexts)
            GenerateProxyMethod(context, methodContext);
        
        /* Generate constructors.
         * Note:
         * The compiler will automatically generate a default constructor for non-user-constructor class,
         * thus the initializer will always be invoked.
         */
        foreach (var constructor in baseClass.GetConstructors())
            GenerateProxyConstructor(context, constructor);
        
        return context.Builder.CreateType() ?? 
               throw new Exception($"Failed to generate proxy class for {baseClass}.");
    }

    /// <summary>
    /// In this stage, the generator will scan the base class and collect plugins triggered by specific members.
    /// </summary>
    /// <param name="baseClass">Base class to generate proxy for.</param>
    /// <returns>Class context.</returns>
    private ClassContext PrepareContext(Type baseClass)
    {
        var context = new ClassContext(_module, baseClass);
        
        var methodProxyId = 0;

        void CheckAndGenerateMethodContext(MethodInfo? method)
        {
            if (method == null)
                return;
            if (!method.IsVirtual || method.IsAbstract || method.IsFinal)
                return;
            context.MethodContexts[method] = new MethodContext(method, methodProxyId++);
        }
        
        // Scan members.
        foreach (var member in baseClass.GetMembers(BindingFlags.Instance | 
                                                    BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (member.GetCustomAttribute<TriggerAttribute>() == null)
                continue;
            foreach (var attribute in member.GetCustomAttributes<TriggerAttribute>())
                context.RegisterTriggerMember(attribute, member);
            switch (member)
            {
                case MethodInfo method:
                    CheckAndGenerateMethodContext(method);
                    break;
                case PropertyInfo property:
                    CheckAndGenerateMethodContext(property.GetMethod);
                    CheckAndGenerateMethodContext(property.SetMethod);
                    break;
            }
        }

        return context;
    }
    
    private static void GenerateProxyConstructor(ClassContext context, ConstructorInfo baseConstructor)
    {
        var parameters = baseConstructor.GetParameters();
        var constructorFlags = MethodAttributes.HideBySig |
                               MethodAttributes.SpecialName | MethodAttributes.RTSpecialName;
        if (baseConstructor.IsPublic)
            constructorFlags |= MethodAttributes.Public;
        else constructorFlags |= MethodAttributes.Private;

        var code = context.Builder.DefineConstructor(
                constructorFlags, CallingConventions.Standard,
                parameters.Select(info => info.ParameterType).ToArray())
            .GetILGenerator();
        for (var argumentIndex = 0; argumentIndex <= parameters.Length; ++argumentIndex)
            // Load constructor arguments to the stack.
            code.Emit(OpCodes.Ldarg, argumentIndex);
        // Call base constructor.
        code.Emit(OpCodes.Call, baseConstructor);

        // Invoke the initializer method.
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Call, context.Initializer);

        code.Emit(OpCodes.Ret);
    }
    
    private static void GenerateProxyMethod(ClassContext classContext, MethodContext methodBuilder)
    {
        var methodParameters = methodBuilder.ProxiedMethod.GetParameters();

        var proxyMethod = classContext.Builder.DefineMethod(
            $"_Prism_{methodBuilder.Id}_{methodBuilder.ProxiedMethod.Name}",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual,
            CallingConventions.Standard, methodBuilder.ProxiedMethod.ReturnType,
            methodParameters.Select(parameter => parameter.ParameterType).ToArray()
        );
        classContext.Builder.DefineMethodOverride(
            proxyMethod, methodBuilder.ProxiedMethod);

        // Initialize code context
        var code = proxyMethod.GetILGenerator();
        var context = new CodeContext(code, methodBuilder);

        // Initialize skipping flag variable.
        code.Emit(OpCodes.Ldc_I4_0);
        code.Emit(OpCodes.Stloc, context.Skipped);
        
        // Apply pre-invoking operations.
        foreach (var operation in methodBuilder.OperationsBeforeInvoking)
            operation(ref context);

        // Declare the skipping jump target.
        var labelInvoked = code.DefineLabel();
        
        // Skip the invoking stage if the skipping variable is true.
        code.Emit(OpCodes.Ldloc, context.Skipped);
        code.Emit(OpCodes.Brtrue, labelInvoked);
        
        // Load all arguments into the stack.
        for (var parameterIndex = 0; parameterIndex <= methodParameters.Length; ++parameterIndex)
        {
            code.Emit(OpCodes.Ldarg, parameterIndex);
        }
        // Invoke the base method without looking up in the virtual function table.
        code.Emit(OpCodes.Call, methodBuilder.ProxiedMethod);
        // Store the method returning value into a local variable.
        // The result variable will be overwritten here.
        if (context.Result != null)
            code.Emit(OpCodes.Stloc, context.Result);
        // Update skipping flag.
        code.Emit(OpCodes.Ldc_I4_0);
        code.Emit(OpCodes.Stloc, context.Skipped);
        
        code.MarkLabel(labelInvoked);
        
        // Apply post-invoking operations.
        foreach (var operation in methodBuilder.OperationsAfterInvoking)
            operation(ref context);
        
        // Load the returning value if the result variable is not null.
        if (context.Result != null)
            code.Emit(OpCodes.Ldloc, context.Result);
        
        code.Emit(OpCodes.Ret);
    }
}