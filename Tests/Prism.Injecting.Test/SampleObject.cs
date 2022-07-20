namespace Prism.Injecting.Test;

public class SampleObject
{
    [Inject(necessary: false)] public int IntValue = -1;

    [Inject(id: "IntInjection", necessary: false)] public int IntValueWithId = -1;

    [Inject(typeof(string), necessary: false)] public string StringText = "";
}