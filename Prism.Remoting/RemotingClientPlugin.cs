using Prism.Framework;
using Prism.Framework.Builders;

namespace Prism.Remoting;

[TriggerBy(typeof(TriggerAttribute))]
public class RemotingClientPlugin : IProxyPlugin
{
    public void Modify(ClassContext context)
    {
        throw new NotImplementedException();
    }
}