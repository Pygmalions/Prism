using Prism.Framework;

namespace Prism.Injecting;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : TriggerAttribute
{
    /// <summary>
    /// Category of the injection.
    /// If null, then the type of the field or property will be used.
    /// </summary>
    public readonly Type? Category;

    /// <summary>
    /// Optional ID of the injection.
    /// </summary>
    public readonly string? Id;

    /// <summary>
    /// If true, then an exception will be thrown if the injection is missing from the container in use.
    /// </summary>
    public readonly bool Necessary;
    
    /// <summary>
    /// Mark the category and the optional ID of the required injection.
    /// </summary>
    /// <param name="category">
    /// Category of the injection.
    /// If it is null, then the type of the field or property will be used.
    /// </param>
    /// <param name="id">
    /// Optional ID of the required injection.
    /// </param>
    /// <param name="necessary">
    /// If this injection slot is necessary, then an exception will be thrown when the injection is missing
    /// from the container in use.
    /// </param>
    public InjectAttribute(Type? category = null, string? id = null, bool necessary = true)
    {
        Category = category;
        Id = id;
        Necessary = necessary;
    }
}