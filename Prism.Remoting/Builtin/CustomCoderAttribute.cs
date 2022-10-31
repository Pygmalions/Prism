namespace Prism.Remoting.Builtin;

/// <summary>
/// Class marked with this attribute will enable custom encoder and decoder.
/// A static method marked with this attribute will be used as the encoder or decoder,
/// according to its signature.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class CustomCoderAttribute : Attribute
{
    /// <summary>
    /// Name of the encoder static method.
    /// </summary>
    public readonly string EncoderName;
    /// <summary>
    /// Name of the decoder static method.
    /// </summary>
    public readonly string DecoderName;

    public CustomCoderAttribute(string encoderName, string decoderName)
    {
        EncoderName = encoderName;
        DecoderName = decoderName;
    }
}