using System.Threading.Tasks;

namespace XamlAnimatedGif.Decoding
{
    internal class GifTrailer : GifBlock
    {
        internal const int TrailerByte = 0x3B;

        private GifTrailer()
        {
        }

        internal override GifBlockKind Kind
        {
            get { return GifBlockKind.Other; }
        }

        internal static Task<GifTrailer> ReadAsync()
        {
            return Task.FromResult(new GifTrailer());
        }
    }
}
