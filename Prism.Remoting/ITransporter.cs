namespace Prism.Remoting;

/// <summary>
/// Implement this interface to transport invocation data to the remote object.
/// </summary>
public interface ITransporter
{
    /// <summary>
    /// Transport a pack of invocation data.
    /// </summary>
    /// <param name="data">Invocation result data.</param>
    /// <returns>Invocation result data, can be parsed and used by a remote proxy.</returns>
    byte[] Transport(byte[] data);
}