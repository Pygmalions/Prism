namespace Prism.Remoting;

public static class Remote
{
    private static readonly Lazy<RemoteClientGenerator> SharedGenerator = 
        new(() => new RemoteClientGenerator());

    public static void AddEncoder(Type dataType, DataCoder coder) 
        => SharedGenerator.Value.AddEncoder(dataType, coder);
    
    public static void AddDecoder(Type dataType, DataCoder coder) 
        => SharedGenerator.Value.AddDecoder(dataType, coder);
    
    public static void RemoveEncoder(Type dataType, DataCoder? coder = null) 
        => SharedGenerator.Value.RemoveEncoder(dataType, coder);
    
    public static void RemoveDecoder(Type dataType, DataCoder? coder = null) 
        => SharedGenerator.Value.RemoveDecoder(dataType, coder);

    public static void AddCoderProvider(Type provider) => SharedGenerator.Value.AddCoderProvider(provider);

    public static void RemoveCoderProvider(Type provider) => SharedGenerator.Value.RemoveCoderProvider(provider);
    
    public static Type GetClient(Type proxiedClass)
        => SharedGenerator.Value.GetClient(proxiedClass);
    
    public static object New(Type proxiedClass, ITransporter transporter)
        => SharedGenerator.Value.New(proxiedClass, transporter);

    public static TClass New<TClass>(ITransporter transporter)
        => (TClass)SharedGenerator.Value.New(typeof(TClass), transporter);
}