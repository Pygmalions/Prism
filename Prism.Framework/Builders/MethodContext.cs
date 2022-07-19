using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace Prism.Framework.Builders;

/// <summary>
/// Context for plugins to insert code generation operations.
/// Those stored operations will be invoked after all plugins have registered their operations.
/// Then operations will be performed in groups as triggering pre-invoking event, invoking proxied method,
/// and triggering post-invoking event in sequence.
/// </summary>
public class MethodContext
{
    /// <summary>
    /// Reflection information of the proxied method.
    /// </summary>
    public readonly MethodInfo ProxiedMethod;

    /// <summary>
    /// Id of this proxy method.
    /// </summary>
    public readonly int Id;
    
    /// <summary>
    /// Additional data attached to this method context.
    /// </summary>
    public readonly Dictionary<string, object> Data = new();
    
    /// <summary>
    /// Additional data accessor.
    /// </summary>
    /// <param name="name">Name of the data entry.</param>
    public object? this[string name]
    {
        get => Data.TryGetValue(name, out var data) ? data : null;
        set
        {
            if (value != null)
                Data[name] = value;
            else Data.Remove(name);
        }
    }
    
    public MethodContext(MethodInfo method, int id)
    {
        ProxiedMethod = method;
        Id = id;
    }

    /// <summary>
    /// Delegate type for actions which will insert IL codes to the IL stream.
    /// </summary>
    public delegate void CodeInserter(ref CodeContext context);
    
    /// <summary>
    /// Registered operations to apply before the invoker code.
    /// </summary>
    internal readonly List<CodeInserter> OperationsBeforeInvoking = new();
    
    /// <summary>
    /// Registered operations to apply after the invoker code.
    /// </summary>
    internal readonly List<CodeInserter> OperationsAfterInvoking = new();
    
    /// <summary>
    /// Insert IL code before invoking the proxied method.
    /// </summary>
    /// <param name="operation">Action which will insert code into the IL generator.</param>
    public void InsertBeforeInvoking(CodeInserter operation)
        => OperationsBeforeInvoking.Add(operation);

    /// <summary>
    /// Insert IL code after invoking the proxied method.
    /// </summary>
    /// <param name="operation">Action which will insert code into the IL generator.</param>
    public void InsertAfterInvoking(CodeInserter operation)
        => OperationsAfterInvoking.Add(operation);
}