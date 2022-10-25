namespace Prism.Injecting;

public class InjectionContainer : IContainer
{
    private readonly Dictionary<Type, Dictionary<string, object>> _injections = new();

    /// <summary>
    /// Query chain for this container to use.
    /// If this container can not find an injection,
    /// it will try to look up in containers in this chain.
    /// </summary>
    public List<InjectionContainer> Chain;
    
    /// <summary>
    /// Get the injection with the specified category and optional ID.
    /// </summary>
    /// <param name="category">Category of the injection.</param>
    /// <param name="id">Optional ID.</param>
    /// <returns>Injection object, or null if none matches.</returns>
    public object? Get(Type category, string? id = null)
    {
        if (!_injections.TryGetValue(category, out var group) ||
            !group.TryGetValue(id ?? "", out var instance))
        {
            return Chain.Select(chainedContainer => chainedContainer.Get(category, id)).
                FirstOrDefault(result => result != null);
        }
        var instanceType = instance.GetType();
        if (instanceType.IsGenericType && instanceType.GetGenericTypeDefinition() == typeof(Func<>) 
                                       && instanceType.GetGenericArguments().Length == 1 
                                       && instanceType.GetGenericArguments()[0].IsAssignableTo(category))
        {
            return instanceType.GetMethod("Invoke")!.Invoke(instance, Array.Empty<object?>());
        }

        return instance;
    }

    /// <summary>
    /// Add a injection to the specified category with the specified name.
    /// </summary>
    /// <param name="category">Category for the injection to add.</param>
    /// <param name="instance">Instance to add to the container.</param>
    /// <param name="name">Optional name, if it is null, then an empty string will be used.</param>
    public void Add(Type category, object instance, string? name = null)
    {
        if (!_injections.TryGetValue(category, out var group))
        {
            group = new Dictionary<string, object>();
            _injections[category] = group;
        }

        group[name ?? ""] = instance;
    }

    /// <summary>
    /// Remove a injection from the specified category with the given name.
    /// </summary>
    /// <param name="category">Category of the injection to remove.</param>
    /// <param name="name">Optional name, if it is null, then an empty string will be used.</param>
    public void Remove(Type category, string? name = null)
    {
        if (!_injections.TryGetValue(category, out var group))
            return;
        group.Remove(name ?? "");
    }
}