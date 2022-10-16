namespace Prism.Remoting;

/// <summary>
/// Proxy class modified by <see cref="RemotingPlugin"/> will be implemented with this attribute,
/// given the ability to handle invocation data.
/// </summary>
public interface IRemoteServer
{
    /// <summary>
    /// Handle invocation data.
    /// </summary>
    /// <param name="data">Invocation data.</param>
    /// <returns>Invocation result data.</returns>
    byte[] HandleInvocation(byte[] data);
}