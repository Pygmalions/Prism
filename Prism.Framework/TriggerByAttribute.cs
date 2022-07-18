namespace Prism.Framework;

/// <summary>
/// Plugins can be marked with this attribute to enable it to be triggered by specified types.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class TriggerByAttribute : Attribute
{
    /// <summary>
    /// Type of trigger attribute.
    /// </summary>
    public readonly Type Trigger;

    /// <summary>
    /// Discover method for the trigger type.
    /// </summary>
    public readonly TriggerMode Mode;
    
    /// <summary>
    /// Trigger this attribute with the specified trigger.
    /// </summary>
    /// <param name="trigger">Type to trigger this plugin.</param>
    /// <param name="mode">Discover method for the trigger type.</param>
    public TriggerByAttribute(Type trigger, TriggerMode mode = TriggerMode.ByAttribute)
    {
        Trigger = trigger;
        Mode = mode;
    }
}