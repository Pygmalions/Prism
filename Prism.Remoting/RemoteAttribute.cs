using Prism.Framework;

namespace Prism.Remoting;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Class)]
public class RemoteAttribute : TriggerAttribute
{}