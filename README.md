# Prism

Prism Framework is an AOP framework based on IL dynamic weaving,
which provides interfaces and classes to generate proxy classes in run-time.

This project also contains some common mechanisms based on the Prism Framework,
such as Decorating (Run-time Method Decorator) and Injecting (IoC).

# Goals

This project is still under rapid development towards goals as below:
- **Convenient to use**. This requires developers of this project to lower the complexity of its API. 
- **High performance**. All AOP implementations have efficiency loss in varying degrees. 
This project aims at reducing the RAM and time consumption of the proxy procedure. 
- **Extensible to other scenarios**. The Prism Framework is an AOP framework, 
and its designed to help developers to develop other techniques based on AOP,
rather than let users directly use this framework, which might be hard.

# Road Map

- [X] **Decorating**: A plugin which allows users to decorate (modify) the behavior of methods in run-time.
- [X] **Injecting**: A plugin which implements dependency injection (IoC) in proxy classes, 
which can significantly improve the performance.
- [ ] **Logging**: A plugin which provides a log interface based on AOP. It provides a report-style log model and
can be easily connected to other data sources to send logs.

If you have any ideas or suggestions, please open a new issue to let us know.
Hereby allow us to express our heartfelt gratefulness for your help.

# Contributing

Since it is a open-source project, we appreciate and welcome contribution in any forms,
including:
- **Bug reporting**. Please report any bugs you met via issues.
- **Function suggestions**. Please share your ideas or suggestion via issues, 
or email to our official email *pygmalions@hotmail.com* or manager email *jia.vincent@hotmail.com* .
- **Pull requests**. Please create pull requests in this repository, and we can continue the following procedure.
- **Plugins**. You can submit your plugins as pull requests, or tell us the plugin repository address, 
and we are happy and grateful to add a link to your plugin.

# Related Efforts

This framework is still under rapid development, thus its API may be unstable.
If you are looking for mature AOP frameworks, please allow us to recommend other projects for you:

## AOP

[Castle](https://github.com/castleproject/): A widely recognized AOP framework, also based on dynamic IL weaving.

[PostSharp](https://www.postsharp.net/): A mature commercial AOP project, which is based on source weaving. 
This framework inject codes in the compiling stage as a MSBuild task.

## IoC

[Ninject](http://www.ninject.org/): A widely used convenient IoC library. 
It is easy to use and it have many useful extensions.

 [Spring.NET](https://springframework.net/): A .NET implementation of the famous Java framework [Spring](https://spring.io/).
 It is a heavyweight yet comprehensive framework, including but not limited to AOP and IoC.