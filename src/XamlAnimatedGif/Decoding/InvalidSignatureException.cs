using System;
using System.Runtime.Serialization;

namespace XamlAnimatedGif.Decoding
{
    [Serializable]
    public class InvalidSignatureException : GifDecoderException
    {
        internal InvalidSignatureException(string message) : base(message) { }
        internal InvalidSignatureException(string message, Exception inner) : base(message, inner) { }

        protected InvalidSignatureException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        { }
    }
}