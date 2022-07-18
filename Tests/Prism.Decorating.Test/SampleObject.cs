namespace Prism.Decorating.Test;

public class SampleObject
{
    [Decorate]
    public virtual int Value { get => BackingValue; set => BackingValue = value; }

    public int BackingValue = 0;

    [Decorate]
    public virtual int Add(int value) => BackingValue + value;
    
    public SampleObject()
    {}

    public SampleObject(int initialValue)
    {
        BackingValue = initialValue;
    }
}