# Prism Decorating

This plugin provides a mechanism to modify the behavior of functions in run-time.

# How to Use

By simply mark ```[Decorate]``` attribute on any non-final virtual method of a class,
this plugin will implement the ```IDecorated``` interface in the proxy class.
Method decorators can be get through ```GetMethodDecorator(MethodInfo)``` method provided by ```IDecorated``` interface.

For example, this is the code of a sample class:
```c#
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
```
Firstly, initialize a proxy generator and load this plugin:
```c#
// Initialize a proxy generator.
var generator = new Generator();
// Load decoration plugin.
generator.RegisterPlugin(new DecorationPlugin());
```
Secondly, create a instance of the proxy class:
```c#
var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>());
```
Convert this instance into the original type and the ```IDecorated``` interface separately:
```c#
var proxy = (IDecorated)instance!;
var sample = (SampleObject)instance!;
```
Get the decorator of ```Add(int)``` method. 
In this example, the code in that method will be skipped, and the return value will be set to 10. 
```c#
var decorator = 
    proxy.GetMethodDecorator(typeof(SampleObject).GetMethod(nameof(SampleObject.Add))!);

decorator!.Invoking += (ref Invocation invocation) =>
{
    invocation.Result = 10;
    invocation.Skipped = true;
};
```
Invoke the decorated method, the ```result``` here will be 10,
no matter what the argument is given.
```c#
var result = sample.Add(3);
```
