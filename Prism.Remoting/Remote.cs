namespace Prism.Remoting;

public static class Remote
{
    private static readonly Lazy<RemoteClientGenerator> SharedGenerator = 
        new(() => new RemoteClientGenerator());

    public static ICoderProvider CoderProvider
    {
        get => SharedGenerator.Value.CoderProvider;
        set => SharedGenerator.Value.CoderProvider = value;
    }
    
    public static Type GetClient(Type proxiedClass)
        => SharedGenerator.Value.GetClient(proxiedClass);
    
    public static object New(Type proxiedClass, ITransporter transporter)
        => SharedGenerator.Value.New(proxiedClass, transporter);

    public static TClass New<TClass>(ITransporter transporter)
        => (TClass)SharedGenerator.Value.New(typeof(TClass), transporter);
}