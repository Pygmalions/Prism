using System.Reflection;
using System.Reflection.Emit;
using System.Reflection.Metadata;

namespace Prism.Framework.Builders;

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