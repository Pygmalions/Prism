namespace Prism.Framework;

public static class InterfaceHelper
{
    /// <summary>
    /// Force to cast an instance into a specified class type.
    /// This extensive method will throw an <see cref="InvalidCastException"/> if it fails to cast the given instance
    /// into the demanded type.
    /// </summary>
    /// <param name="instance">Instance to cast.</param>
    /// <typeparam name="TClass">Class type to cast to.</typeparam>
    /// <returns>Casted instance.</returns>
    /// <exception cref="InvalidCastException">
    /// Throw if the cast failed.
    /// </exception>
    public static TClass As<TClass>(this object instance) where TClass : class
        => instance as TClass ??
           throw new InvalidCastException(
               $"Can not cast an instance {instance.GetType()} into {typeof(TClass)}.");

    /// <summary>
    /// Try to cast an instance into the specified type.
    /// </summary>
    /// <param name="instance">Instance to cast.</param>
    /// <typeparam name="TClass">Class type to cast to.</typeparam>
    /// <returns>Casted instance, or null if the cast failed.</returns>
    public static TClass? IfIs<TClass>(this object instance) where TClass : class
        => instance as TClass;
}