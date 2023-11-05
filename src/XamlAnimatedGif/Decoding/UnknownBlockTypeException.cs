using System;
using System.Runtime.Serialization;

namespace XamlAnimatedGif.Decoding
{
    [Serializable]
    public class UnknownBlockTypeException : GifDecoderException
    {
        internal UnknownBlockTypeException(string message) : base(message) { }
        internal UnknownBlockTypeException(string message, Exception inner) : base(message, inner) { }

        protected UnknownBlockTypeException(
            SerializationInfo info,
            StreamingContext context)
            : base(info, context)
        { }
    }
}