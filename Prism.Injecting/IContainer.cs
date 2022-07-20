using System.Reflection;

namespace Prism.Injecting;

/// <summary>
/// Classes implement this interface can provide injections.
/// </summary>
public interface IContainer
{
    /// <summary>
    /// Get an injection from this container.
    /// </summary>
    /// <param name="category">Category of the injection</param>
    /// <param name="id">Optional ID of the injection.</param>
    /// <returns>Injection object, or null if none matches.</returns>
    object? Get(Type category, string? id = null);
}