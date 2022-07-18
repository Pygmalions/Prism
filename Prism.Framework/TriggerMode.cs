namespace Prism.Framework;

public enum TriggerMode
{
    /// <summary>
    /// Trigger the plugin if any member (field, method, property) is marked with this trigger attribute.
    /// </summary>
    ByAttribute,
    /// <summary>
    /// Trigger the plugin if the proxied class is derived from this trigger type,
    /// or if the proxy class implements this interface.
    /// </summary>
    ByHierarchy
}