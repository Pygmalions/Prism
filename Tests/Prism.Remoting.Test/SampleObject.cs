using System.Threading.Tasks;

namespace Prism.Remoting.Test;

public class SampleObject
{
    [Remote]
    public virtual int Add(int a, int b) => a + b;
    
    [Remote]
    public virtual int Sub(int a, int b) => a - b;

    [Remote]
    public virtual Task<int> AddAsTask(int a, int b) => Task.FromResult(a + b);
    
    [Remote]
    public virtual ValueTask<int> AddAsValueTask(int a, int b) => new (a + b);
}