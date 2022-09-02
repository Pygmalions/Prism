namespace Prism.Remoting;

public interface IRemoteProxy
{
    ITransporter? Transporter { get; set; }
}