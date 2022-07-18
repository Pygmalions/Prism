using System.Reflection;

namespace Prism.Decorating;

public class MethodDecorator
{
    /// <summary>
    /// Delegate type of invocation handlers.
    /// </summary>
    public delegate void Handler(ref Invocation invocation);
    
    /// <summary>
    /// Reflection information of the proxied method.
    /// </summary>
    public readonly MethodInfo ProxiedMethod;

    /// <summary>
    /// Object instance which owns this proxy.
    /// </summary>
    public readonly object ProxiedObject;

    /// <summary>
    /// Whether the returning value is nullable or not.
    /// </summary>
    public readonly bool ResultNullable;
    
    public MethodDecorator(object instance, MethodInfo method)
    {
        ProxiedMethod = method;
        ProxiedObject = instance;

        ResultNullable = method.ReturnType.IsGenericType &&
                         method.ReturnType.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    private readonly HashSet<Handler> _invoking = new();
    private readonly HashSet<Handler> _invoked = new();
    
    /// <summary>
    /// Event triggered when the method is to be invoked.
    /// </summary>
    public event Handler Invoking
    {
        add => _invoking.Add(value);
        remove => _invoking.Remove(value);
    }
    
    /// <summary>
    /// Event triggered when the method is invoked.
    /// </summary>
    public event Handler Invoked
    {
        add => _invoked.Add(value);
        remove => _invoked.Remove(value);
    }

    /// <summary>
    /// Trigger the invoking event.
    /// </summary>
    /// <param name="invocation">Invocation context.</param>
    internal void TriggerInvokingEvent(ref Invocation invocation)
    {
        foreach (var handler in _invoking)
        {
            handler(ref invocation);
        }
    }
    
    /// <summary>
    /// Trigger the invoked event.
    /// </summary>
    /// <param name="invocation">Invocation context.</param>
    internal void TriggerInvokedEvent(ref Invocation invocation)
    {
        foreach (var handler in _invoked)
        {
            handler(ref invocation);
        }
    }

    internal Invocation CreateInvocationContext(object?[] arguments)
        => new Invocation(ProxiedObject, ProxiedMethod, ResultNullable, arguments);
}