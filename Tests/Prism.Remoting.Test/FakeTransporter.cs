using System.Threading.Tasks;

namespace Prism.Remoting.Test;

public class FakeTransporter : ITransporter
{
    public byte[]? Data;
    
    public byte[] Transport(byte[] data)
    {
        Data = data;
        return data;
    }
}