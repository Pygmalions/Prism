namespace Prism.Remoting;

public interface ICoderProvider
{
    /// <summary>
    /// Get an encoder of the specified data type.
    /// </summary>
    /// <param name="dataType">Type of the data to encode.</param>
    /// <returns>Data encoder of the specified type.</returns>
    DataCoder GetEncoder(Type dataType);

    /// <summary>
    /// Get an decoder of the specified data type.
    /// </summary>
    /// <param name="dataType">Type of the data to decode.</param>
    /// <returns>Data decoder of the specified type.</returns>
    DataCoder GetDecoder(Type dataType);
}