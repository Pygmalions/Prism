using Prism.Framework;

namespace Prism.Decorating;

/// <summary>
/// Methods and properties marked with this attribute will be generated method proxies for.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class DecorateAttribute : TriggerAttribute
{}