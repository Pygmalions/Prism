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

        // Check whether this class has an custom implementation of IInjectable.
        if (baseClass.IsAssignableTo(typeof(IInjectable)))
            return;

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

        foreach (var (member, attribute) in context.GetTriggerMember(typeof(InjectAttribute)))
        {
            if (attribute is not InjectAttribute injectAttribute)
                continue;

            FieldInfo? possibleField = null;
            PropertyInfo? possibleProperty = null;
            switch (member)
            {
                case FieldInfo field:
                    possibleField = field;
                    break;
                case PropertyInfo property:
                    possibleProperty = property;
                    break;
                default:
                    throw new Exception($"Member {member.Name}({member.GetType()}) " +
                                        $"of {baseClass} is not a valid injection target.");
            }

            var injectionCategory = (injectAttribute.Category ??
                                     possibleField?.FieldType ?? possibleProperty?.PropertyType)!;
            InsertQueryingCode(code, injectionCategory, injectAttribute.Id, variableInjection);

            // Check injection content, and skip the injection procedure if it is null.
            var labelInjectionEnd = code.DefineLabel();
            var labelInjectionFailed = code.DefineLabel();
            
            // Check whether the injection is null or not, if it is null, then jump to the error handling.
            code.Emit(OpCodes.Ldloc, variableInjection);
            code.Emit(OpCodes.Brfalse, labelInjectionFailed);
            
            // Injection is not null, perform the injection.
            if (possibleField != null)
                InsertFieldInjectingCode(code, possibleField, variableInjection);
            else if (possibleProperty != null)
                InsertPropertyInjectingCode(code, possibleProperty, variableInjection);
            code.Emit(OpCodes.Br, labelInjectionEnd);
            
            // Insert code to throw an exception if the necessary injection failed.
            code.MarkLabel(labelInjectionFailed);
            if (injectAttribute.Necessary)
            {
                code.Emit(OpCodes.Ldstr, 
                    $"Missing the injection for {member.Name} of {baseClass.Name} from the container.");
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

    /// <summary>
    /// Load injection content and store it into the field.
    /// </summary>
    /// <param name="code">IL stream.</param>
    /// <param name="field">Field to inject.</param>
    /// <param name="variableInjection">Injection content.</param>
    private static void InsertFieldInjectingCode(ILGenerator code, FieldInfo field, 
        LocalBuilder variableInjection)
    {
        code.Emit(OpCodes.Ldarg_0);
        code.Emit(OpCodes.Ldloc, variableInjection);
        if (field.FieldType.IsValueType)
            code.Emit(OpCodes.Unbox_Any, field.FieldType);
        code.Emit(OpCodes.Stfld, field);
    }
    
    /// <summary>
    /// Load injection content and store it into the property.
    /// </summary>
    /// <param name="code">IL stream.</param>
    /// <param name="property">Property to inject.</param>
    /// <param name="variableInjection">Injection content.</param>
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
    
    /// <summary>
    /// Generate query code which will get the injection content in the container.
    /// </summary>
    /// <param name="code">IL stream.</param>
    /// <param name="category">Category type of the injection content.</param>
    /// <param name="id">Optional searching ID.</param>
    /// <param name="variableInjection">Local variable to store injection content.</param>
    private static void InsertQueryingCode(ILGenerator code, Type category, string? id, 
        LocalBuilder variableInjection)
    {
        // Load container.
        code.Emit(OpCodes.Ldarg_1);

        // Load type.
        code.Emit(OpCodes.Ldtoken, category);
        code.Emit(OpCodes.Call, typeof(Type).GetMethod(nameof(Type.GetTypeFromHandle))!);

        // Load optional name or null.
        if (id != null)
            code.Emit(OpCodes.Ldstr, id);
        else code.Emit(OpCodes.Ldnull);

        // Get the injection from the container.
        code.Emit(OpCodes.Callvirt, 
            typeof(IContainer).GetMethod(nameof(IContainer.Get))!);
        
        code.Emit(OpCodes.Stloc, variableInjection);
    }
}