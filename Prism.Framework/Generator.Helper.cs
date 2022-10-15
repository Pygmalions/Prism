namespace Prism.Framework;

public static class GeneratorHelper
{
    /// <summary>
    /// Get the proxy class of the specified class.
    /// </summary>
    /// <param name="generator">Generator to use.</param>
    /// <typeparam name="TClass">Class type to generate proxy for.</typeparam>
    /// <returns>Proxy class.</returns>
    /// <exception cref="InvalidOperationException">
    /// Throw if the base type is an interface, or an abstract class.
    /// </exception>
    public static Type GetProxy<TClass>(this Generator generator) where TClass : class
        => generator.GetProxy(typeof(TClass));

    public static TClass New<TClass>(this Generator generator, params object[] arguments) where TClass : class
        => Activator.CreateInstance(generator.GetProxy(typeof(TClass)), arguments) as TClass ??
           throw new Exception($"Failed to instantiate a proxy instance of {typeof(TClass)}.");
}