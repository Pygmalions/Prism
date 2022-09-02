using System.Reflection;
using System.Reflection.Emit;
using Prism.Framework;
using Prism.Framework.Builders;

namespace Prism.Remoting;

[TriggerBy(typeof(TriggerAttribute))]
public class RemotingServerPlugin : IProxyPlugin
{
    private readonly Dictionary<(Type, Type), Translator> _translators = new();

    /// <summary>
    /// Add a translator to the specified entry.
    /// </summary>
    /// <param name="from">Input type.</param>
    /// <param name="to">Output type.</param>
    /// <param name="translator">Translator delegate to add.</param>
    public void AddTranslator(Type from, Type to, Translator translator)
        => _translators[(from, to)] = translator;

    /// <summary>
    /// Remove a translator entry.
    /// </summary>
    /// <param name="from">Input type.</param>
    /// <param name="to">Output type.</param>
    public void RemoveTranslator(Type from, Type to)
        => _translators.Remove((from, to));

    /// <summary>
    /// Scan and add all static <see cref="Translator"/> delegate fields of the specified class.
    /// </summary>
    /// <param name="provider">Type to scan.</param>
    /// <exception cref="NullReferenceException">
    /// Throw if the content of any static <see cref="Translator"/> field is null.
    /// </exception>
    public void AddTranslator(Type provider)
    {
        var fields =
            provider.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            if (field.FieldType.IsAssignableTo(typeof(Translator)))
                continue;
            if (field.FieldType.GetCustomAttribute<TranslatorAttribute>() is not { } attribute)
                continue;
            if (field.GetValue(null) is not Translator translator)
                throw new NullReferenceException($"Value of a translator field {field.Name} is null.");
            AddTranslator(attribute.InputType, attribute.OutputType, translator);
        }
    }

    /// <summary>
    /// Scan and remove all entries matching the input and output types
    /// of static <see cref="Translator"/> delegate fields of the specified class.
    /// </summary>
    /// <param name="provider">Type to scan.</param>
    public void RemoveTranslator(Type provider)
    {
        var fields =
            provider.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var field in fields)
        {
            if (field.FieldType.IsAssignableTo(typeof(Translator)))
                continue;
            if (field.FieldType.GetCustomAttribute<TranslatorAttribute>() is not { } attribute)
                continue;
            RemoveTranslator(attribute.InputType, attribute.OutputType);
        }
    }

    public void Modify(ClassContext context)
    {
        context.Builder.AddInterfaceImplementation(typeof(IRemoteCallable));
        
        var handlerMethod = context.Builder.DefineMethod("_Prism_HandleInvocation",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual,
            CallingConventions.Standard,
            typeof(Memory<byte>), new[] { typeof(Memory<byte>) });
        context.Builder.DefineMethodOverride(handlerMethod,
            typeof(IRemoteCallable).GetMethod(nameof(IRemoteCallable.HandleInvocation))!);
        var code = handlerMethod.GetILGenerator();
        
        
    }
}