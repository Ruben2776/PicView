using System.Buffers.Binary;
using System.Text;
using ImageMagick;
using PicView.Core.Metadata;

namespace PicView.Tests.Metadata;

[Collection("Sequential")]
public class PngTextChunkWriterTests
{
    private const string Cjk = "一只猫 🐱";

    [Fact]
    public void ReplaceText_KeepsEveryOtherChunkBytesAndOrder()
    {
        var original = ReadAndDelete(SdTestFiles.WritePng(
            ("Comment", "hello", false), ("parameters", "old", false), ("prompt", "zipped", true)));

        var result = PngTextChunkWriter.ReplaceText(original, "parameters", "new");

        Assert.NotNull(result);
        var before = Walk(original);
        var after = Walk(result);
        Assert.Equal(before.Count, after.Count);
        for (var i = 0; i < before.Count; i++)
        {
            if (i == 2)
            {
                Assert.Equal("tEXt", after[i].Type);
                Assert.Equal("parameters\0new", Encoding.Latin1.GetString(after[i].Data));
                continue;
            }

            Assert.Equal(before[i].Type, after[i].Type);
            Assert.Equal(before[i].Data, after[i].Data);
        }
    }

    [Fact]
    public void ReplaceText_Latin1_WritesTExt()
    {
        var original = ReadAndDelete(SdTestFiles.WritePng(("parameters", "old", false)));

        var result = PngTextChunkWriter.ReplaceText(original, "parameters", "café");

        Assert.NotNull(result);
        Assert.Equal("tEXt", Walk(result)[1].Type);
        Assert.Equal("café", ReadBack(result)["parameters"]);
    }

    [Fact]
    public void ReplaceText_Cjk_WritesITXt()
    {
        var original = ReadAndDelete(SdTestFiles.WritePng(("parameters", "old", false)));

        var result = PngTextChunkWriter.ReplaceText(original, "parameters", Cjk);

        Assert.NotNull(result);
        Assert.Equal("iTXt", Walk(result)[1].Type);
        Assert.Equal(Cjk, ReadBack(result)["parameters"]);
    }

    [Fact]
    public void ReplaceText_ZTxtSource_IsReplaced()
    {
        var original = ReadAndDelete(SdTestFiles.WritePng(("parameters", "zipped old", true)));

        var result = PngTextChunkWriter.ReplaceText(original, "parameters", "new");

        Assert.NotNull(result);
        Assert.Equal("tEXt", Walk(result)[1].Type);
        Assert.Equal("new", ReadBack(result)["parameters"]);
    }

    [Fact]
    public void ReplaceText_KeywordCase_MatchesAndKeepsSpelling()
    {
        var original = ReadAndDelete(SdTestFiles.WritePng(("Parameters", "old", false)));

        var result = PngTextChunkWriter.ReplaceText(original, "parameters", "new");

        Assert.NotNull(result);
        Assert.StartsWith("Parameters\0", Encoding.Latin1.GetString(Walk(result)[1].Data));
    }

    [Fact]
    public void ReplaceText_ResultDecodes()
    {
        using var image = new MagickImage(MagickColors.Gray, 5, 3);
        image.Format = MagickFormat.Png;
        var png = SdTestFiles.InsertTextChunk(image.ToByteArray(), "parameters", "old");

        var result = PngTextChunkWriter.ReplaceText(png, "parameters", Cjk);

        Assert.NotNull(result);
        using var decoded = new MagickImage(result);
        Assert.Equal(image.Width, decoded.Width);
        Assert.Equal(image.Height, decoded.Height);
        Assert.Equal(Cjk, ReadBack(result)["parameters"]);
    }

    [Fact]
    public void ReplaceText_NotPng_ReturnsNull()
    {
        Assert.Null(PngTextChunkWriter.ReplaceText("not a png at all, really not"u8, "parameters", "x"));
    }

    [Fact]
    public void ReplaceText_NoKeyword_ReturnsNull()
    {
        var original = ReadAndDelete(SdTestFiles.WritePng(("Comment", "hello", false)));

        Assert.Null(PngTextChunkWriter.ReplaceText(original, "parameters", "x"));
    }

    [Fact]
    public void ReplaceText_BadCrc_ReturnsNull()
    {
        var original = ReadAndDelete(SdTestFiles.WritePng(("parameters", "old", false)));
        original[Walk(original)[0].Data.Length + 8 + 12 + 8 + 2] ^= 0xFF; // a data byte of the text chunk

        Assert.Null(PngTextChunkWriter.ReplaceText(original, "parameters", "x"));
    }

    [Fact]
    public void ReplaceText_Truncated_ReturnsNull()
    {
        var original = ReadAndDelete(SdTestFiles.WritePng(("parameters", "old", false)));

        Assert.Null(PngTextChunkWriter.ReplaceText(original.AsSpan(0, original.Length - 20), "parameters", "x"));
        Assert.Null(PngTextChunkWriter.ReplaceText(original.AsSpan(0, original.Length - 12), "parameters", "x"));
    }

    private static byte[] ReadAndDelete(string path)
    {
        try
        {
            return File.ReadAllBytes(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static Dictionary<string, string> ReadBack(byte[] png)
    {
        using var stream = new MemoryStream(png);
        return PngTextChunkReader.Read(stream)!;
    }

    private static List<(string Type, byte[] Data)> Walk(byte[] png)
    {
        var chunks = new List<(string, byte[])>();
        var position = 8;
        while (position + 12 <= png.Length)
        {
            var length = (int)BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(position));
            chunks.Add((Encoding.ASCII.GetString(png, position + 4, 4), png.AsSpan(position + 8, length).ToArray()));
            position += 12 + length;
        }

        return chunks;
    }
}
