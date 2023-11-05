namespace XamlAnimatedGif.Decoding
{
    internal interface IGifRect
    {
        int Left { get; }
        int Top { get; }
        int Width { get; }
        int Height { get; }
    }
}