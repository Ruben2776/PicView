using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using XamlAnimatedGif.Extensions;

namespace XamlAnimatedGif.Decoding
{
    // label 0x01
    internal class GifPlainTextExtension : GifExtension
    {
        internal const int ExtensionLabel = 0x01;

        public int BlockSize { get; private set; }
        public int Left { get; private set; }
        public int Top { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public int CellWidth { get; private set; }
        public int CellHeight { get; private set; }
        public int ForegroundColorIndex { get; private set; }
        public int BackgroundColorIndex { get; private set; }
        public string Text { get; private set; }

        public IList<GifExtension> Extensions { get; private set; }

        private GifPlainTextExtension()
        {
        }

        internal override GifBlockKind Kind
        {
            get { return GifBlockKind.GraphicRendering; }
        }

        internal new static async Task<GifPlainTextExtension> ReadAsync(Stream stream, IEnumerable<GifExtension> controlExtensions)
        {
            var plainText = new GifPlainTextExtension();
            await plainText.ReadInternalAsync(stream, controlExtensions).ConfigureAwait(false);
            return plainText;
        }

        private async Task ReadInternalAsync(Stream stream, IEnumerable<GifExtension> controlExtensions)
        {
            // Note: at this point, the label (0x01) has already been read

            byte[] bytes = new byte[13];
            await stream.ReadAllAsync(bytes, 0, bytes.Length).ConfigureAwait(false);

            BlockSize = bytes[0];
            if (BlockSize != 12)
                throw GifHelpers.InvalidBlockSizeException("Plain Text Extension", 12, BlockSize);

            Left = BitConverter.ToUInt16(bytes, 1);
            Top = BitConverter.ToUInt16(bytes, 3);
            Width = BitConverter.ToUInt16(bytes, 5);
            Height = BitConverter.ToUInt16(bytes, 7);
            CellWidth = bytes[9];
            CellHeight = bytes[10];
            ForegroundColorIndex = bytes[11];
            BackgroundColorIndex = bytes[12];

            var dataBytes = await GifHelpers.ReadDataBlocksAsync(stream).ConfigureAwait(false);
            Text = GifHelpers.GetString(dataBytes);
            Extensions = controlExtensions.ToList().AsReadOnly();
        }
    }
}
