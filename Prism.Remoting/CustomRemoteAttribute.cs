namespace Prism.Remoting;

[AttributeUsage(AttributeTargets.Class)]
public class CustomRemoteAttribute : Attribute
{
    public readonly Type? RemoteClient;

    public CustomRemoteAttribute(Type? remoteClient = null)
    {
        RemoteClient = remoteClient;
    }
}