using Prism.Framework;

namespace Prism.Remoting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property)]
public class RemoteAttribute : TriggerAttribute
{}