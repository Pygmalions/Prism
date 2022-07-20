namespace Prism.Injecting.Test;

public class SampleObjectWithNecessary
{
    [Inject(necessary: true)] public int IntValue = -1;
}