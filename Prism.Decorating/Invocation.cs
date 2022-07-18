using System.Reflection;

namespace Prism.Decorating;

/// <summary>
/// Invocation context structure.
/// </summary>
public ref struct Invocation
{
    /// <summary>
    /// Arguments for this invocation.
    /// </summary>
    public readonly object?[] Arguments;
    
    /// <summary>
    /// Whether the result is nullable or not.
    /// </summary>
    public readonly bool Nullable;
    
    /// <summary>
    /// Result to return.
    /// </summary>
    private object? _result = null;
    
    /// <summary>
    /// Result of this returning value.
    /// </summary>
    public object? Result 
    { 
        get => _result;
        set
        {
            if (value == null && !Nullable)
                throw new Exception("Failed to set invocation returning value: null is not acceptable.");
            _result = value;
        } 
    }

    /// <summary>
    /// Exception to throw in the caller context.
    /// This field will be checked after invoking each handler,
    /// and will interrupt the invocation procedure immediately.
    /// </summary>
    public Exception? Exception = null;

    private bool _skipped = false;
    /// <summary>
    /// Whether the proxied method is skipped or not.
    /// </summary>
    /// <exception cref="Exception">
    /// Throw if the <see cref="Result"/> is not nullable but null.
    /// </exception>
    public bool Skipped
    {
        get => _skipped;
        set
        {
            if (value && _result == null && !Nullable)
                throw new Exception("Failed to stop the invocation: null result is not acceptable.");
            _skipped = value;
        }
    }

    private bool _stopped = false;
    /// <summary>
    /// Stop the invocation procedure immediately.
    /// </summary>
    /// <exception cref="Exception">
    /// Throw if the <see cref="Result"/> is not nullable but null.
    /// </exception>
    public bool Stopped
    {
        get => _stopped;
        set
        {
            if (value && _result == null && !Nullable)
                throw new Exception("Failed to stop the invocation: null result is not acceptable.");
            _stopped = value;
        }
    }

    public readonly object ProxiedObject;

    public readonly MethodInfo ProxiedMethod;
    
    public Invocation(object instance, MethodInfo method, bool resultNullable, object?[] arguments)
    {
        ProxiedObject = instance;
        ProxiedMethod = method;
        Nullable = resultNullable;
        Arguments = arguments;
    }
}