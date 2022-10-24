using NUnit.Framework;
using Prism.Framework;

namespace Prism.Injecting.Test;

[TestFixture]
public class FunctorTest
{
    [Test]
    public void DefaultInject()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new InjectionPlugin());

        var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>()) as SampleObject;

        var proxy = (IInjectable)instance;

        var container = new InjectionContainer();

        container.Add(typeof(int), () => 3);

        proxy.Inject(container);

        Assert.That(instance.IntValue, Is.EqualTo(3));
    }
}