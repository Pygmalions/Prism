using System.Reflection.Emit;

namespace Prism.Remoting;

public delegate void DataCoder(ILGenerator code, LocalBuilder stream);