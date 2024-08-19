using System.Runtime.Serialization;

namespace PicView.Avalonia.AnimatedImage;

[Serializable]
internal class InvalidGifStreamException(string message) : Exception
{
    public override string Message { get; } = message;
}

