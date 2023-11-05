using System;
using System.Runtime.Serialization;

namespace XamlAnimatedGif.Decoding
{
    [Serializable]
    public class UnknownExtensionTypeException : GifDecoderException
    {
        internal UnknownExtensionTypeException(string message) : base(message) { }
        internal UnknownExtensionTypeException(string message, Exception inner) : base(message, inner) { }

        protected UnknownExtensionTypeException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        { }
    }
}