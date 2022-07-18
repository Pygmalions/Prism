using NUnit.Framework;

namespace Prism.Framework.Test;

[TestFixture]
public class PluginTest
{
    [Test]
    public void ProxyClassVerify()
    {
        var generator = new Generator();
        var plugin = new SamplePlugin();
        generator.RegisterPlugin(plugin);
        
        Assert.DoesNotThrow(() => generator.GetProxy<SampleObject>());
        
        Assert.That(plugin.Members!.ToArray(), Has.Length.EqualTo(2));
    }
}