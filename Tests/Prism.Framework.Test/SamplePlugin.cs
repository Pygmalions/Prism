using System.Reflection;
using Prism.Framework.Builders;

namespace Prism.Framework.Test;

[TriggerBy(typeof(SampleTrigger))]
public class SamplePlugin : IProxyPlugin
{
    public IEnumerable<KeyValuePair<MemberInfo, Attribute>>? Members;

    public void Modify(ClassContext context)
    {
        if (context.Builder.BaseType != typeof(SampleObject))
            return;
        Members = context.GetTriggerMember(typeof(SampleTrigger));
    }
}