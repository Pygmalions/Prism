using System.Reflection.Emit;

namespace Prism.Remoting;

public delegate void DataDecoder(ILGenerator code, LocalBuilder reader);