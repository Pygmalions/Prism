namespace Prism.Remoting;

public enum EndianMode
{
    /// <summary>
    /// Where 0x1234 is stored as 0x34, 0x12.
    /// </summary>
    Little,
    /// <summary>
    /// Where 0x1234 is stored as 0x12, 0x34.
    /// </summary>
    Big
}

public static class EndianTool
{
    private static readonly Lazy<EndianMode> LazyLocalEndian = new(
        () => BitConverter.GetBytes(1)[0] == 1 ? EndianMode.Little : EndianMode.Big);

    /// <summary>
    /// The endian mode of the local machine.
    /// </summary>
    public static EndianMode LocalEndian => LazyLocalEndian.Value;

    /// <summary>
    /// If the local endian mode is not same as the specified endian mode,
    /// then reverse the given data, to convert it into the local endian mode or the specified endian mode.
    /// <br/><br/>
    /// Operation happens in place.
    /// </summary>
    /// <param name="mode">Mode to compare.</param>
    /// <param name="data">Data to coordinate.</param>
    public static void Coordinate(EndianMode mode, byte[] data)
    {
        if (LocalEndian != mode)
            Array.Reverse(data);
    }
}