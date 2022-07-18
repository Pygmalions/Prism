using NUnit.Framework;
using Prism.Framework;

namespace Prism.Decorating.Test;

[TestFixture]
public class DecoratorTest
{
    public Generator TestGenerator = null!;
    
    [SetUp]
    public void InitializeGenerator()
    {
        TestGenerator = new Generator();
        TestGenerator.RegisterPlugin(new DecorationPlugin());
    }
    
    [Test]
    public void VerifyInvocationContextInInvokingEvent()
    {
        var instance = Activator.CreateInstance(TestGenerator.GetProxy<SampleObject>());

        var proxy = (IDecorated)instance!;
        Assert.That(proxy, Is.Not.Null);
        
        var sample = (SampleObject)instance!;
        Assert.That(sample, Is.Not.Null);

        var decorator = 
            proxy.GetMethodDecorator(typeof(SampleObject).GetMethod(nameof(SampleObject.Add))!);
        Assert.That(decorator, Is.Not.Null);

        var contextArgument = -1;

        decorator!.Invoking += (ref Invocation invocation) =>
        {
            contextArgument = (int)invocation.Arguments[0]!;
        };

        sample.Add(3);
        
        Assert.That(contextArgument, Is.EqualTo(3));
    }
    
    [Test]
    public void VerifyInvocationContextInInvokedEvent()
    {
        var instance = Activator.CreateInstance(TestGenerator.GetProxy<SampleObject>());

        var proxy = (IDecorated)instance!;
        Assert.That(proxy, Is.Not.Null);
        
        var sample = (SampleObject)instance!;
        Assert.That(sample, Is.Not.Null);

        var decorator = 
            proxy.GetMethodDecorator(typeof(SampleObject).GetMethod(nameof(SampleObject.Add))!);
        Assert.That(decorator, Is.Not.Null);

        var contextResult = -1;

        decorator!.Invoked += (ref Invocation invocation) =>
        {
            contextResult = (int)invocation.Result!;
        };

        var result = sample.Add(3);
        
        Assert.That(contextResult, Is.EqualTo(result));
    }
    
    [Test]
    public void SkipInInvokingEvent()
    {
        var instance = Activator.CreateInstance(TestGenerator.GetProxy<SampleObject>());

        var proxy = (IDecorated)instance!;
        Assert.That(proxy, Is.Not.Null);
        
        var sample = (SampleObject)instance!;
        Assert.That(sample, Is.Not.Null);

        var decorator = 
            proxy.GetMethodDecorator(typeof(SampleObject).GetMethod(nameof(SampleObject.Add))!);
        Assert.That(decorator, Is.Not.Null);

        decorator!.Invoking += (ref Invocation invocation) =>
        {
            invocation.Result = 10;
            invocation.Skipped = true;
        };

        var result = sample.Add(3);
        
        Assert.That(result, Is.EqualTo(10));
    }
    
    [Test]
    public void StopInInvokingEvent()
    {
        var instance = Activator.CreateInstance(TestGenerator.GetProxy<SampleObject>());

        var proxy = (IDecorated)instance!;
        Assert.That(proxy, Is.Not.Null);
        
        var sample = (SampleObject)instance!;
        Assert.That(sample, Is.Not.Null);

        var decorator = 
            proxy.GetMethodDecorator(typeof(SampleObject).GetMethod(nameof(SampleObject.Add))!);
        Assert.That(decorator, Is.Not.Null);

        decorator!.Invoking += (ref Invocation invocation) =>
        {
            invocation.Result = 3;
            invocation.Stopped = true;
        };
        
        Assert.That(sample.BackingValue, Is.EqualTo(0));
        
        var result = sample.Add(1);
        
        Assert.That(result, Is.EqualTo(3));
    }

    [Test]
    public void PropertyProxy()
    {
        var instance = Activator.CreateInstance(TestGenerator.GetProxy<SampleObject>());

        var proxy = (IDecorated)instance!;
        Assert.That(proxy, Is.Not.Null);
        
        var sample = (SampleObject)instance!;
        Assert.That(sample, Is.Not.Null);

        var decorator = proxy.GetMethodDecorator(
            typeof(SampleObject).GetProperty(nameof(SampleObject.Value))!.GetMethod!)!;
        Assert.That(decorator, Is.Not.Null);

        decorator.Invoked += (ref Invocation invocation) =>
        {
            invocation.Result = 12;
        };
        
        Assert.That(sample.Value, Is.EqualTo(12));
    }
}