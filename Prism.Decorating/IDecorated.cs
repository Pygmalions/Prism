using System.Buffers;
using System.Reflection;

namespace Prism.Decorating;

/// <summary>
/// Instances of proxy classes all implement this interface, and can be queried method proxies through this interface.
/// </summary>
public interface IDecorated
{
    /// <summary>
    /// Get the method proxy of the given method.
    /// </summary>
    /// <param name="method">Method to get proxy with.</param>
    /// <returns>Method proxy, or null if no corresponding proxy is found.</returns>
    MethodDecorator? GetMethodDecorator(MethodInfo method);

    protected internal class PrismHelper
    {
        /// <summary>
        /// Trigger the invoking event of a method proxy.
        /// This method is designed for the generated method.
        /// </summary>
        /// <param name="proxy">Proxy to trigger.</param>
        /// <param name="invocation">Invocation context.</param>
        public static void TriggerMethodDecoratorInvokingEvent(MethodDecorator proxy, ref Invocation invocation)
            => proxy.TriggerInvokingEvent(ref invocation);

        /// <summary>
        /// Trigger the invoked event of a method proxy.
        /// This method is designed for the generated method.
        /// </summary>
        /// <param name="proxy">Proxy to trigger.</param>
        /// <param name="invocation">Invocation context.</param>
        public static void TriggerMethodDecoratorInvokedEvent(MethodDecorator proxy, ref Invocation invocation)
            => proxy.TriggerInvokedEvent(ref invocation);

        public static MethodDecorator? GetMethodDecorator(Dictionary<MethodInfo, MethodDecorator> decorators,
            MethodInfo method) => decorators.TryGetValue(method, out var decorator) ? decorator : null;

        public static void SetMethodDecorator(Dictionary<MethodInfo, MethodDecorator> decorators,
            MethodInfo method, MethodDecorator decorator) => decorators[method] = decorator;

        public static Invocation CreateInvocationContext(MethodDecorator decorator, object?[] arguments)
            => decorator.CreateInvocationContext(arguments);
    }
}