namespace Prism.Remoting;

/// <summary>
/// Remote proxy classes will implement this interface,
/// and will pass the data generated from remote method invocations to the transporter.
/// <br/><br/>
/// A remote client has the abilities to pack the invocation data and unpack the return value data.
/// Though the data transported is limited to basic value types and their combination by default.
/// </summary>
public interface IRemoteClient
{
    ITransporter Transporter { get; set; }
}