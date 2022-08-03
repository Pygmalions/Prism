# Prism Injecting

This plugin provides a mechanism to do proactive dependency injection,
which means the object to inject proactively get the injection content from the container.

# How to Use

Injections can be gotten with its type category and its optional ID.
If a field or property is marked with ```[Inject]``` attribute,
then this plugin will generate proactive getter for it; 
and if the property ```necessary``` is true (by default),
then an exception will be thrown if the corresponding injection is missing from the container.

For example, this is the code of the class to inject:
```c#
public class SampleObject
{
    [Inject(necessary: false)]
    public int IntValue = -1;

    [Inject(id: "IntInjection", necessary: false)]
    public int IntValueWithId = -1;

    [Inject(typeof(string), necessary: false)]
    public string StringText = "";
}
```
Initialize the proxy generator and load this plugin:
```c#
var generator = new Generator();
generator.RegisterPlugin(new InjectionPlugin());
```
Create an instance of the proxy class:
```c#
var instance = Activator.CreateInstance(generator.GetProxy<SampleObject>()) as SampleObject;
```
Convert it to the ```IInjectable``` interface:
```c#
var proxy = (IInjectable)instance;
```
Prepare the container:
```c#
var container = new InjectionContainer();
container.Add(typeof(int), 3);
```
Inject the object with the container:
```c#
proxy.Inject(container);
```
Then, the field ```IntValue``` is set to 3.