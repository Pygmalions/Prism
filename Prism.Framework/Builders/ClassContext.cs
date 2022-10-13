using System.Reflection;
using System.Reflection.Emit;

namespace Prism.Framework.Builders;

public class ClassContext
{
    /// <summary>
    /// Dynamic type builder.
    /// </summary>
    public readonly TypeBuilder Builder;

    /// <summary>
    /// Method builder for initializer method.
    /// </summary>
    public readonly MethodBuilder Initializer;
    
    /// <summary>
    /// Detected proxy methods which are categorized according to their signatures.
    /// </summary>
    internal readonly Dictionary<MethodInfo, MethodContext> MethodContexts = new();
    
    /// <summary>
    /// Detected proxy methods which are categorized according to triggers marked on them.
    /// </summary>
    internal readonly Dictionary<Type, HashSet<MemberInfo>> TriggerMembers = new();

    internal void RegisterTriggerMember(Type trigger, MemberInfo member)
    {
        if (!TriggerMembers.TryGetValue(trigger, out var group))
        {
            group = new HashSet<MemberInfo>();
            TriggerMembers[trigger] = group;
        }
        group.Add(member);
    }

    internal void UnregisterTriggerMember(Type trigger, MemberInfo member)
    {
        if (!TriggerMembers.TryGetValue(trigger, out var group))
            return;
        group.Remove(member);
    }
    
    /// <summary>
    /// Get a method context according the specified method reflection information.
    /// </summary>
    /// <param name="method">Method reflection information.</param>
    /// <returns>Method context, or null if no corresponding context is found.</returns>
    public MethodContext? GetMethodContext(MethodInfo method)
        => MethodContexts.TryGetValue(method, out var builder) ? builder : null;

    /// <summary>
    /// Get method contexts according the specified trigger type.
    /// </summary>
    /// <param name="trigger">Trigger type.</param>
    /// <returns>Members which comply the specified trigger.</returns>
    public IEnumerable<MemberInfo> GetTriggerMember(Type trigger)
        => TriggerMembers.TryGetValue(trigger, out var methods) ? 
            methods : Array.Empty<MemberInfo>();
    
    public ClassContext(ModuleBuilder module, Type baseClass)
    {
        Builder = module.DefineType(baseClass.Name + "Proxy",
            TypeAttributes.Public | TypeAttributes.Class | TypeAttributes.AnsiClass | TypeAttributes.AutoClass |
            TypeAttributes.BeforeFieldInit);
        Builder.SetParent(baseClass);
        
        Initializer = Builder.DefineMethod("_Prism_Initialize",
            MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName,
            CallingConventions.Standard,
            null, Type.EmptyTypes);
    }
}