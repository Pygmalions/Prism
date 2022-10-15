namespace Prism.Remoting.Test;

public class SampleObject
{
    [Remote]
    public virtual int Add(int a, int b) => a + b;
    
    [Remote]
    public virtual int Sub(int a, int b) => a - b;
}