using System.Reflection;
using System.Reflection.Emit;
using Prism.Framework;
using Prism.Framework.Builders;

namespace Prism.Injecting;

[TriggerBy(typeof(InjectAttribute))]
public class InjectionPlugin : IProxyPlugin
{
    public void Modify(ClassContext context)
    {
        var baseClass = context.Builder.BaseType;
        if (baseClass == null)
            throw new Exception($"Failed to access the base class information of {context.Builder}.");

        var injectionFields = CollectInjectionFields(baseClass);
        var injectionProperties = CollectInjectionProperties(baseClass);
        
        // Generate code.
        context.Builder.AddInterfaceImplementation(typeof(IInjectable));
        
        var injectorMethod = context.Builder.DefineMethod("_Nebula_Inject",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual,
            CallingConventions.Standard,
            null, new[] { typeof(IContainer) });
        context.Builder.DefineMethodOverride(injectorMethod,
            typeof(IInjectable).GetMethod(nameof(IInjectable.Inject))!);
        var code = injectorMethod.GetILGenerator();

        var variableInjection = code.DeclareLocal(typeof(object));
        
        var exceptionConstructor = typeof(Exception).GetConstructor(new[] { typeof(string) })!;
        
        foreach (var (targetField, targetAttribute) in injectionFields)
        {
            InsertQueryingCode(code, targetAttribute.Category ?? targetField.FieldType, targetAttribute.Id,
                variableInjection);

            // Check injection content, and skip the injection procedure if it is null.
            var labelInjectionEnd = code.DefineLabel();
            var labelInjectionFailed = code.DefineLabel();
            
            // Check whether the injection is null or not, if it is null, then jump to the error handling.
            code.Emit(OpCodes.Ldloc, variableInjection);
            code.Emit(OpCodes.Brfalse, labelInjectionFailed);
            
            // Injection is not null, perform the injection.
            InsertFieldInjectingCode(code, targetField, variableInjection);
            code.Emit(OpCodes.Br, labelInjectionEnd);
            
            code.MarkLabel(labelInjectionFailed);
            if (targetAttribute.Necessary)
            {
                code.Emit(OpCodes.Ldstr, 
                    $"Missing the injection for {targetField.Name} of {baseClass.Name} from the container.");
                code.Emit(OpCodes.Newobj, exceptionConstructor);
                code.Emit(OpCodes.Throw);
            }
            
            code.MarkLabel(labelInjectionEnd);
        }
        
        foreach (var (targetProperty, targetAttribute) in injectionProperties)
        {
            InsertQueryingCode(code, targetAttribute.Category ?? targetProperty.PropertyType, targetAttribute.Id,
                variableInjection);

            var labelInjectionEnd = code.DefineLabel();
            var labelInjectionFailed = code.DefineLabel();
            
            // Check whether the injection is null or not, if it is null, then jump to the error handling.
            code.Emit(OpCodes.Ldloc, variableInjection);
            code.Emit(OpCodes.Brfalse, labelInjectionFailed);
            
            InsertPropertyInjectingCode(code, targetProperty, variableInjection);
            code.Emit(OpCodes.Br, labelInjectionEnd);
            
            code.MarkLabel(labelInjectionFailed);
            if (targetAttribute.Necessary)
            {
                code.Emit(OpCodes.Ldstr, 
                    $"Missing the injection for {targetProperty.Name} of {baseClass.Name} from the container.");
                code.Emit(OpCodes.Newobj, exceptionConstructor);
                code.Emit(OpCodes.Throw);
            }
        
            code.MarkLabel(labelInjectionEnd);
        }
        
        code.Emit(OpCodes.Ret);
        
        // Generate injection constructor if it has a default constructor.
        var defaultConstructor = baseClass.GetConstructor(Type.EmptyTypes);
        if (defaultConstructor == null)
            return;
        var constructorCode = context.Builder.DefineConstructor(
            defaultConstructor.Attributes, defaultConstructor.CallingConvention,
            new[] { typeof(IContainer) }).GetILGenerator();
        // Call the proxied constructor.
        constructorCode.Emit(OpCodes.Ldarg_0);
        constructorCode.Emit(OpCodes.Call, defaultConstructor);
        // Invoke the injecting method.
        constructorCode.Emit(OpCodes.Ldarg_0);
        constructorCode.Emit(OpCodes.Ldarg_1);
        constructorCode.Emit(OpCodes.Call, injectorMethod);
        constructorCode.Emit(OpCodes.Ret);
    }

    private static void InsertFieldInjectingCode(ILGenerator code, FieldInfo field, 
        LocalBuilder variableInjection)
    {
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Ldloc, variableInjection);
        if (field.FieldType.IsValueType)
            code.Emit(OpCodes.Unbox_Any, field.FieldType);
        code.Emit(OpCodes.Stfld, field);
    }
    
    private static void InsertPropertyInjectingCode(ILGenerator code, PropertyInfo property, 
        LocalBuilder variableInjection)
    {
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Ldloc, variableInjection);
        if (property.PropertyType.IsValueType)
            code.Emit(OpCodes.Unbox_Any, property.PropertyType);
        code.Emit(property.SetMethod!.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, 
            property.SetMethod!);
    }
    
    private static void InsertQueryingCode(ILGenerator code, Type category, string? name, 
        LocalBuilder variableInjection)
    {
        // Load container.
        code.Emit(OpCodes.Ldarg_1);

        // Load type.
        code.Emit(OpCodes.Ldtoken, category);
        code.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

        // Load optional name or null.
        if (name != null)
            code.Emit(OpCodes.Ldstr, name);
        else code.Emit(OpCodes.Ldnull);

        // Get the injection from the container.
        code.Emit(OpCodes.Callvirt, 
            typeof(IContainer).GetMethod(nameof(IContainer.Get))!);
        
        code.Emit(OpCodes.Stloc, variableInjection);
    }
    
    /// <summary>
    /// Collect all available fields which are marked with the injection attribute.
    /// </summary>
    /// <param name="baseClass">Reflection information to search in.</param>
    /// <returns>List of available fields to inject and their corresponding injection attributes.</returns>
    /// <exception cref="Exception">Throw if any init-only field is marked to inject.</exception>
    private static List<(FieldInfo, InjectAttribute)> CollectInjectionFields(Type baseClass)
    {
        var injections = new List<(FieldInfo, InjectAttribute)>();
        foreach (var field in baseClass.GetFields(BindingFlags.Instance | 
                                                  BindingFlags.NonPublic | BindingFlags.Public ))
        {
            var attribute = field.GetCustomAttribute<InjectAttribute>();
            if (attribute == null) continue;
            if (field.IsInitOnly)
                throw new Exception(
                    $"Init-only field {field.Name} of {baseClass.Name} " +
                    "can not be marked to be injected.");
            injections.Add((field, attribute));
        }

        return injections;
    }
    
    /// <summary>
    /// Collect all available properties which are marked with the injection attribute.
    /// </summary>
    /// <param name="baseClass">Reflection information to search in.</param>
    /// <returns>List of available properties to inject and their corresponding injection attributes.</returns>
    /// <exception cref="Exception">Throw if any read-only property is marked to inject.</exception>
    private static List<(PropertyInfo, InjectAttribute)> CollectInjectionProperties(Type baseClass)
    {
        var injections = new List<(PropertyInfo, InjectAttribute)>();
        foreach (var property in baseClass.GetProperties(BindingFlags.Instance | 
                                                         BindingFlags.NonPublic | BindingFlags.Public ))
        {
            var attribute = property.GetCustomAttribute<InjectAttribute>();
            if (attribute == null) continue;
            if (!property.CanWrite) 
                throw new Exception($"Read-only property {property.Name} of {baseClass.Name} " +
                                    "can not be marked to be injected.");;
            injections.Add((property, attribute));
        }

        return injections;
    }
}