namespace Prism.Framework.Test;

public class SampleObject
{
    [SampleTrigger]
    public virtual int Value { get => BackingValue; set => BackingValue = value; }

    public int BackingValue = 0;

    [SampleTrigger]
    public virtual int Add(int value) => BackingValue + value;
    
    public SampleObject()
    {}

    public SampleObject(int initialValue)
    {
        BackingValue = initialValue;
    }
}