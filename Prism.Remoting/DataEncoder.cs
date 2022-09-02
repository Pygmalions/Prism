using System.Reflection.Emit;

namespace Prism.Remoting;

public delegate void DataEncoder(ILGenerator code, LocalBuilder writer);