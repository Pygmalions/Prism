namespace Prism.Injecting;

/// <summary>
/// Classes implement this interface can customize injection behavior.
/// </summary>
public interface IInjectable
{
    /// <summary>
    /// Inject this object with the given container.
    /// </summary>
    /// <param name="container">
    /// Container to use to inject this object.
    /// </param>
    void Inject(IContainer container);
}