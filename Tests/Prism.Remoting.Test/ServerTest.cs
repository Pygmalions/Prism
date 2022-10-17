using System;
using System.IO;
using NUnit.Framework;
using Prism.Framework;

namespace Prism.Remoting.Test;

public class ServerTest
{
    private readonly Generator _generator = new();
    
    [SetUp]
    public void Setup()
    {
        _generator.RegisterPlugin(new RemotingPlugin());
    }

    [Test]
    public void VerifyInterface()
    {
        var instance = _generator.New<SampleObject>();
        Assert.DoesNotThrow(() => instance.As<IRemoteServer>());
    }

    [Test]
    public void HandleInvocation()
    {
        var instance = _generator.New<SampleObject>();
        var server = instance.As<IRemoteServer>();
        
        var addStream = new MemoryStream();
        addStream.Write(BitConverter.GetBytes(typeof(SampleObject).GetMethod(nameof(SampleObject.Add))!.MetadataToken));
        addStream.Write(BitConverter.GetBytes(1));
        addStream.Write(BitConverter.GetBytes(2));
        addStream.Position = 0;
        
        var addResult = server.HandleInvocation(addStream.ToArray());
        var addValue = BitConverter.ToInt32(addResult);
        Assert.AreEqual(3, addValue);
        
        var subStream = new MemoryStream();
        subStream.Write(BitConverter.GetBytes(typeof(SampleObject).GetMethod(nameof(SampleObject.Sub))!.MetadataToken));
        subStream.Write(BitConverter.GetBytes(3));
        subStream.Write(BitConverter.GetBytes(1));
        subStream.Position = 0;
        
        var subResult = server.HandleInvocation(subStream.ToArray());
        var subValue = BitConverter.ToInt32(subResult);
        Assert.AreEqual(2, subValue);
    }
    
    [Test]
    public void HandleTaskInvocation()
    {
        var instance = _generator.New<SampleObject>();
        var server = instance.As<IRemoteServer>();
        
        var addStream = new MemoryStream();
        addStream.Write(BitConverter.GetBytes(typeof(SampleObject).GetMethod(nameof(SampleObject.AddAsTask))!.MetadataToken));
        addStream.Write(BitConverter.GetBytes(1));
        addStream.Write(BitConverter.GetBytes(2));
        addStream.Position = 0;
        
        var addResult = server.HandleInvocation(addStream.ToArray());
        var addValue = BitConverter.ToInt32(addResult);
        Assert.AreEqual(3, addValue);
    }
    
    [Test]
    public void HandleValueTaskInvocation()
    {
        var instance = _generator.New<SampleObject>();
        var server = instance.As<IRemoteServer>();
        
        var addStream = new MemoryStream();
        addStream.Write(BitConverter.GetBytes(typeof(SampleObject).GetMethod(nameof(SampleObject.AddAsValueTask))!.MetadataToken));
        addStream.Write(BitConverter.GetBytes(1));
        addStream.Write(BitConverter.GetBytes(2));
        addStream.Position = 0;
        
        var addResult = server.HandleInvocation(addStream.ToArray());
        var addValue = BitConverter.ToInt32(addResult);
        Assert.AreEqual(3, addValue);
    }
}