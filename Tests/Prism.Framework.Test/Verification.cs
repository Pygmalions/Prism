using NUnit.Framework;

namespace Prism.Framework.Test;

[TestFixture]
public class VerificationTest
{
    [Test]
    public void ProxyClassVerify()
    {
        var generator = new Generator();
        Type proxyClass = null!;
        Assert.DoesNotThrow(() => proxyClass = generator.GetProxy<SampleObject>());

        var proxyInstance = Activator.CreateInstance(proxyClass) as SampleObject;
        
        Assert.That(proxyInstance, Is.Not.Null);
        
        Assert.That(proxyInstance!.Add(1), Is.EqualTo(proxyInstance.BackingValue + 1));
    }
    
    [Test]
    public void ConstructorFunction()
    {
        var generator = new Generator();

        SampleObject? instance = null;
        
        Assert.DoesNotThrow(() => 
            instance =  Activator.CreateInstance(generator.GetProxy<SampleObject>(), 2) as SampleObject);
        
        Assert.That(instance, Is.Not.Null);
        
        Assert.That(instance!.BackingValue, Is.EqualTo(2));
    }

    [Test]
    public void MethodFunction()
    {
        var generator = new Generator();
        Type proxyClass = null!;
        Assert.DoesNotThrow(() => proxyClass = generator.GetProxy<SampleObject>());

        var proxyInstance = Activator.CreateInstance(proxyClass) as SampleObject;
        
        Assert.That(proxyInstance, Is.Not.Null);
        
        Assert.That(proxyInstance!.Add(1), Is.EqualTo(proxyInstance.BackingValue + 1));
    }
}