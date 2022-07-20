using NUnit.Framework;
using Prism.Framework;

namespace Prism.Injecting.Test;

[TestFixture]
public class InterfaceTest
{
    [Test]
    public void VerifyCode()
    {
        var generator = new Generator();
        
        generator.RegisterPlugin(new InjectionPlugin());

        SampleObject? instance = null;
        
        Assert.DoesNotThrow(() => 
            instance =  Activator.CreateInstance(generator.GetProxy<SampleObject>()) as SampleObject);
        
        Assert.That((IInjectable)instance, Is.Not.Null);
    }
}