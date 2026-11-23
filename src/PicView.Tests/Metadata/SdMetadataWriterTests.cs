using System.Buffers.Binary;
using ImageMagick;
using PicView.Core.Exif;
using PicView.Core.Metadata;

namespace PicView.Tests.Metadata;

[Collection("Sequential")]
public class SdMetadataWriterTests
{
    private const string OldPayload =
        "old prompt\nNegative prompt: blurry\nSteps: 20, Sampler: Euler, CFG scale: 7, Seed: 1, Size: 8x8";

    private const string NewPayload =
        "新しい prompt\nNegative prompt: blurry\nSteps: 20, Sampler: Euler, CFG scale: 7, Seed: 1, Size: 8x8";

    [Fact]
    public async Task WriteAsync_Jpeg_KeepsImageDataAndReadsBack()
    {
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(OldPayload));
        try
        {
            var file = new FileInfo(path);
            var current = SdMetadataReader.Read(file, ReadComment(path), null);
            Assert.NotNull(current);
            var before = File.ReadAllBytes(path);

            Assert.True(await SdMetadataWriter.WriteAsync(file, current, NewPayload));

            var after = File.ReadAllBytes(path);
            Assert.Equal(before.AsSpan(FindSos(before)).ToArray(), after.AsSpan(FindSos(after)).ToArray());
            var reread = SdMetadataReader.Read(file, ReadComment(path), null);
            Assert.NotNull(reread);
            Assert.Equal("新しい prompt", reread.Prompt);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_JpegOver64Kb_IsRefusedAndFileUnchanged()
    {
        var path = SdTestFiles.WriteJpegWithUserComment(SdTestFiles.AsciiUserComment(OldPayload));
        try
        {
            var file = new FileInfo(path);
            var current = SdMetadataReader.Read(file, ReadComment(path), null);
            Assert.NotNull(current);
            var before = File.ReadAllBytes(path);

            var result = await SdMetadataWriter.WriteAsync(file, current, new string('中', 40000));

            Assert.False(result);
            Assert.Equal(before, File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_WebP_ReadsBack()
    {
        var path = SdTestFiles.WriteWebPWithUserComment(SdTestFiles.AsciiUserComment(OldPayload));
        try
        {
            var file = new FileInfo(path);
            var current = SdMetadataReader.Read(file, ReadComment(path), null);
            Assert.NotNull(current);
            Assert.True(current.IsEditable);

            Assert.True(await SdMetadataWriter.WriteAsync(file, current, NewPayload));

            var reread = SdMetadataReader.Read(file, ReadComment(path), null);
            Assert.NotNull(reread);
            Assert.Equal("新しい prompt", reread.Prompt);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_Png_ReadsBackAndKeepsPixels()
    {
        var path = Path.Combine(Path.GetTempPath(), $"picview_sd_{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new MagickImage(MagickColors.Gray, 8, 8))
            {
                image.Format = MagickFormat.Png;
                image.Write(path);
            }

            File.WriteAllBytes(path, SdTestFiles.InsertTextChunk(File.ReadAllBytes(path), "parameters", OldPayload));
            var file = new FileInfo(path);
            var current = SdMetadataReader.Read(file, null, null);
            Assert.NotNull(current);
            Assert.True(current.IsEditable);

            Assert.True(await SdMetadataWriter.WriteAsync(file, current, "plain prompt\nSteps: 5"));

            var reread = SdMetadataReader.Read(file, null, null);
            Assert.NotNull(reread);
            Assert.Equal("plain prompt", reread.Prompt);
            using var original = new MagickImage(MagickColors.Gray, 8, 8);
            using var written = new MagickImage(path);
            Assert.Equal(0, original.Compare(written, ErrorMetric.Absolute));
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public async Task WriteAsync_NotEditable_ReturnsFalseAndFileUnchanged()
    {
        var path = SdTestFiles.WritePng(("prompt", "{\"3\":{\"class_type\":\"KSampler\",\"inputs\":{\"seed\":1}}}", false));
        try
        {
            var file = new FileInfo(path);
            var current = SdMetadataReader.Read(file, null, null);
            Assert.NotNull(current);
            Assert.False(current.IsEditable);
            var before = File.ReadAllBytes(path);

            Assert.False(await SdMetadataWriter.WriteAsync(file, current, "anything"));

            Assert.Equal(before, File.ReadAllBytes(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string ReadComment(string path)
    {
        // A full read: Ping does not give the profile of a WebP
        using var image = new MagickImage(path);
        return ExifReader.GetUserComment(image.GetExifProfile());
    }

    /// <summary>The offset of the first SOS marker, found by walking the segments.</summary>
    private static int FindSos(byte[] jpeg)
    {
        var offset = 2;
        while (offset + 4 <= jpeg.Length)
        {
            Assert.Equal(0xFF, jpeg[offset]);
            if (jpeg[offset + 1] == 0xDA)
            {
                return offset;
            }

            offset += 2 + BinaryPrimitives.ReadUInt16BigEndian(jpeg.AsSpan(offset + 2));
        }

        throw new InvalidOperationException("No SOS marker");
    }
}
