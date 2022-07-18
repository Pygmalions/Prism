using Prism.Framework.Builders;

namespace Prism.Framework;

public interface IProxyPlugin
{
    void Modify(ClassContext context);
}