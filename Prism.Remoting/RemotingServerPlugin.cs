using System.Reflection;
using Prism.Framework;
using Prism.Framework.Builders;

namespace Prism.Remoting;

/// <summary>
/// This plugin will implement <see cref="IRemoteServer"/> interface for the proxied class.
/// <br/><br/>
/// Currently, async methods are not supported.
/// </summary>
[TriggerBy(typeof(RemoteAttribute))]
public class RemotingServerPlugin : RemotingPlugin<DataDecoder>
{
    public override void Modify(ClassContext context)
    {
        context.Builder.AddInterfaceImplementation(typeof(IRemoteServer));
        
        var handlerMethod = context.Builder.DefineMethod("_Prism_HandleInvocation",
            MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.NewSlot |
            MethodAttributes.SpecialName | MethodAttributes.Virtual,
            CallingConventions.Standard,
            typeof(Memory<byte>), new[] { typeof(Memory<byte>) });
        context.Builder.DefineMethodOverride(handlerMethod,
            typeof(IRemoteServer).GetMethod(nameof(IRemoteServer.HandleInvocation))!);
        var code = handlerMethod.GetILGenerator();
        
        // Todo: Unpack invocation data.
        
        // Todo: Pack return value data.
    }
}