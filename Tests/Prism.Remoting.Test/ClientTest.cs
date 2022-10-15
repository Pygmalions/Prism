using NUnit.Framework;
using Prism.Framework;

namespace Prism.Remoting.Test;

public class ClientTest
{
    private readonly FakeTransporter _transporter = new();
    
    [Test]
    public void VerifyInterface()
    {
        var instance = Remote.New<SampleObject>(_transporter);
        Assert.DoesNotThrow(() => instance.As<IRemoteClient>());
    }

    [Test]
    public void TestClient()
    {
        var instance = Remote.New<SampleObject>(_transporter);
        var result = instance.Add(1, 2);
        Assert.AreEqual(result, typeof(SampleObject).GetMethod(nameof(SampleObject.Add))!.MetadataToken);
    }
}