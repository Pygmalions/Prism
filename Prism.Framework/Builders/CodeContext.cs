using System.Reflection;
using System.Reflection.Emit;

namespace Prism.Framework.Builders;

/// <summary>
/// Context to use in the terminal stage of the code generation procedure.
/// This context contains local variable which can be used to control the proxy method behavior.
/// </summary>
public readonly ref struct CodeContext
{
    /// <summary>
    /// Reflection information of the proxy method.
    /// </summary>
    public readonly MethodInfo Method;
    
    /// <summary>
    /// IL stream of the generated method.
    /// </summary>
    public readonly ILGenerator Code;

    /// <summary>
    /// Variable which stores the returning value.
    /// </summary>
    public readonly LocalBuilder? Result;

    /// <summary>
    /// Whether the proxied method should be skipped or not.
    /// If its value is set to true, then the proxied method will be skipped.
    /// </summary>
    public readonly LocalBuilder Skipped;

    /// Addtional data dictionary.
    public readonly Dictionary<string, object> Data;

    /// <summary>
    /// Construct a code context to generate IL codes.
    /// The method context will share the additional data dictionary with this code context.
    /// </summary>
    /// <param name="code">IL stream to append codes to.</param>
    /// <param name="method">Method context.</param>
    public CodeContext(ILGenerator code, MethodContext method)
    {
        Method = method.ProxiedMethod;
        Code = code;
        Skipped = Code.DeclareLocal(typeof(bool));
        Result = method.ProxiedMethod.ReturnType != typeof(void) ? 
            Code.DeclareLocal(method.ProxiedMethod.ReturnType) : null;
        Data = method.Data;
    }
    
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
}