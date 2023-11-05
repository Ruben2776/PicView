using System;
using System.Runtime.Serialization;

namespace XamlAnimatedGif.Decoding
{
    [Serializable]
    public abstract class GifDecoderException : Exception
    {
        protected GifDecoderException(string message) : base(message) { }
        protected GifDecoderException(string message, Exception inner) : base(message, inner) { }

        protected GifDecoderException(
          SerializationInfo info,
          StreamingContext context)
            : base(info, context) { }
    }
}
