using System;
using System.IO;
using System.Threading.Tasks;
using XamlAnimatedGif.Extensions;

namespace XamlAnimatedGif.Decoding
{
    // label 0xF9
    internal class GifGraphicControlExtension : GifExtension
    {
        internal const int ExtensionLabel = 0xF9;

        public int BlockSize { get; private set; }
        public GifFrameDisposalMethod DisposalMethod { get; private set; }
        public bool UserInput { get; private set; }
        public bool HasTransparency { get; private set; }
        public int Delay { get; private set; }
        public int TransparencyIndex { get; private set; }

        private GifGraphicControlExtension()
        {

        }

        internal override GifBlockKind Kind
        {
            get { return GifBlockKind.Control; }
        }

        internal static async Task<GifGraphicControlExtension> ReadAsync(Stream stream)
        {
            var ext = new GifGraphicControlExtension();
            await ext.ReadInternalAsync(stream).ConfigureAwait(false);
            return ext;
        }

        private async Task ReadInternalAsync(Stream stream)
        {
            // Note: at this point, the label (0xF9) has already been read

            byte[] bytes = new byte[6];
            await stream.ReadAllAsync(bytes, 0, bytes.Length).ConfigureAwait(false);
            BlockSize = bytes[0]; // should always be 4
            if (BlockSize != 4)
                throw GifHelpers.InvalidBlockSizeException("Graphic Control Extension", 4, BlockSize);
            byte packedFields = bytes[1];
            DisposalMethod = (GifFrameDisposalMethod) ((packedFields & 0x1C) >> 2);
            UserInput = (packedFields & 0x02) != 0;
            HasTransparency = (packedFields & 0x01) != 0;
            Delay = BitConverter.ToUInt16(bytes, 2) * 10; // milliseconds
            TransparencyIndex = bytes[4];
        }
    }
}
