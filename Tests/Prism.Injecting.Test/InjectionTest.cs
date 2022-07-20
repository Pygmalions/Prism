using NUnit.Framework;
using Prism.Framework;

namespace Prism.Injecting.Test;

[TestFixture]
public class InjectionTest
{
    [Test]
    public void DefaultInject()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new InjectionPlugin());

        var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>()) as SampleObject;

        var proxy = (IInjectable)instance;

        var container = new InjectionContainer();
        
        container.Add(typeof(int), 3);

        proxy.Inject(container);

        Assert.That(instance.IntValue, Is.EqualTo(3));
    }
    
    [Test]
    public void InjectWithName()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new InjectionPlugin());

        var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>()) as SampleObject;

        var proxy = (IInjectable)instance;

        var container = new InjectionContainer();
        
        container.Add(typeof(int), 4, "IntInjection");

        proxy.Inject(container);

        Assert.That(instance.IntValueWithId, Is.EqualTo(4));
    }
    
    [Test]
    public void InjectWithCategory()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new InjectionPlugin());

        var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>()) as SampleObject;

        var proxy = (IInjectable)instance;

        var container = new InjectionContainer();
        
        container.Add(typeof(string), "Test");

        proxy.Inject(container);

        Assert.That(instance.StringText, Is.EqualTo("Test"));
    }

    [Test]
    public void InjectFromConstructor()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new InjectionPlugin());

        var container = new InjectionContainer();
        container.Add(typeof(string), "Test");
        
        var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>(), container) as SampleObject;

        Assert.That(instance.StringText, Is.EqualTo("Test"));
    }

    [Test]
    public void NecessaryMissing()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new InjectionPlugin());

        var instance = Activator.CreateInstance(generator.GetProxy<SampleObjectWithNecessary>()) 
            as SampleObjectWithNecessary;

        var proxy = (IInjectable)instance;

        var container = new InjectionContainer();
        
        container.Add(typeof(string), "Test");

        Assert.Throws<Exception>(() => proxy.Inject(container));
    }
}