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
    internal readonly Dictionary<Type, Dictionary<MemberInfo, Attribute>> TriggerMembers = new();

    /// <summary>
    /// Register a member to a trigger.
    /// This method will invoke the <see cref="TriggerAttribute.Apply"/> method
    /// if the attribute is a <see cref="TriggerAttribute"/>.
    /// </summary>
    /// <param name="attribute">Attribute to register to.</param>
    /// <param name="member">The member where this trigger is marked.</param>
    public void RegisterTriggerMember(Attribute attribute, MemberInfo member)
    {
        if (!TriggerMembers.TryGetValue(attribute.GetType(), out var group))
        {
            group = new Dictionary<MemberInfo, Attribute>();
            TriggerMembers[attribute.GetType()] = group;
        }
        group[member] = attribute;
        if (attribute is TriggerAttribute trigger)
            trigger.Apply(this);
    }

    /// <summary>
    /// Unregister a trigger member.
    /// Attention, this will not erase the side effect of <see cref="TriggerAttribute"/>'s
    /// <see cref="TriggerAttribute.Apply"/> method.
    /// </summary>
    /// <param name="attribute">Trigger of the member to unregister.</param>
    /// <param name="member">Member to unregister.</param>
    public void UnregisterTriggerMember(Type attribute, MemberInfo member)
    {
        if (!TriggerMembers.TryGetValue(attribute, out var group))
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
    public IEnumerable<KeyValuePair<MemberInfo, Attribute>> GetTriggerMember(Type trigger)
        => TriggerMembers.TryGetValue(trigger, out var group) ? 
            group : Array.Empty<KeyValuePair<MemberInfo, Attribute>>();
    
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