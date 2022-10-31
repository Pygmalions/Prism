using Prism.Framework.Builders;

namespace Prism.Framework;

/// <summary>
/// Attributes inherit from this base class can be discovered by the generator.
/// </summary>
public abstract class TriggerAttribute : Attribute
{
    protected internal virtual void Apply(ClassContext context)
    {}
}