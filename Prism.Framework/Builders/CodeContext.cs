using System.Reflection;
using System.Reflection.Emit;

namespace Prism.Framework.Builders;

public ref struct CodeContext
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

    public CodeContext(MethodBuilder builder, MethodInfo method)
    {
        Method = method;
        Code = builder.GetILGenerator();
        Skipped = Code.DeclareLocal(typeof(bool));
        Result = builder.ReturnType != typeof(void) ? Code.DeclareLocal(builder.ReturnType) : null;
    }
}