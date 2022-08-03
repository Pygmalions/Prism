# Prism Framework

This framework provides a generator to generate proxy class which
can override non-final virtual methods of base classes in run-time.

The generator is implemented with dynamic IL weaving via System.Reflection.Emit library.
It is designed in plugin model, which allows users to easily extend its function.