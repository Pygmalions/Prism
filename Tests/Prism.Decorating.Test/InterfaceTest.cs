using NUnit.Framework;
using Prism.Framework;

namespace Prism.Decorating.Test;

[TestFixture]
public class InterfaceTest
{
    [Test]
    public void CodeVerification()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new DecorationPlugin());
        
        var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>()) as IDecorated;
        
        Assert.That(instance, Is.Not.Null);
    }
    
    [Test]
    public void ProxiedConstructorFunction()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new DecorationPlugin());

        SampleObject? instance = null;
        
        Assert.DoesNotThrow(() => 
            instance =  Activator.CreateInstance(generator.GetProxy<SampleObject>(), 2) as SampleObject);
        
        Assert.That(instance, Is.Not.Null);
        
        Assert.That(instance!.BackingValue, Is.EqualTo(2));
    }
    
    [Test]
    public void GetMethodDecorator()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new DecorationPlugin());
        
        var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>()) as IDecorated;

        Assert.That(instance, Is.Not.Null);
        
        var methodProxy = 
            instance!.GetMethodDecorator(typeof(SampleObject).GetMethod(nameof(SampleObject.Add))!);
        
        Assert.That(methodProxy, Is.Not.Null);
    }
    
    [Test]
    public void ProxiedMethodFunction()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new DecorationPlugin());

        var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>()) as SampleObject;
        
        var result = 0;
        
        Assert.DoesNotThrow(() => result = instance!.Add(1));
        
        Assert.That(result, Is.EqualTo(instance!.BackingValue + 1));
    }
}