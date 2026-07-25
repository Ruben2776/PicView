using System.Text;
using ImageMagick;
using PicView.Core.Exif;

namespace PicView.Tests.Exif;

public class ExifReaderCommentTests
{
    private const string UnicodeComment = "Unicode comment: 中文";

    [Fact]
    public void GetUserComment_CurrentUtf8Comment_ReturnsDecodedText() =>
        AssertComment(UnicodeComment, WithCharacterCode("UNICODE\0"u8, Encoding.UTF8.GetBytes(UnicodeComment)),
            "0300");

    [Fact]
    public void GetUserComment_CurrentUtf8CommentWithNullTerminator_ReturnsDecodedText() =>
        AssertComment(UnicodeComment,
            WithCharacterCode("UNICODE\0"u8, Encoding.UTF8.GetBytes(UnicodeComment + '\0')), "0300");

    [Theory]
    [InlineData("A")]
    [InlineData("AB")]
    public void GetUserComment_CurrentUtf8CommentWithNullTerminatorAndEitherParity_ReturnsDecodedText(
        string expected) =>
        AssertComment(expected, WithCharacterCode("UNICODE\0"u8, Encoding.UTF8.GetBytes(expected + '\0')), "0300");

    [Fact]
    public void GetUserComment_CurrentUtf8CommentWithEmbeddedNulls_ReturnsDecodedText() =>
        AssertComment("A\0B\0C", WithCharacterCode("UNICODE\0"u8, Encoding.UTF8.GetBytes("A\0B\0C")), "0300");

    [Fact]
    public void GetUserComment_LegacyLittleEndianUnicodeComment_ReturnsDecodedText() =>
        AssertComment(UnicodeComment,
            WithCharacterCode("UNICODE\0"u8, Encoding.Unicode.GetBytes(UnicodeComment + '\0')), "0232");

    [Fact]
    public void GetUserComment_LegacyLittleEndianUnicodeWithoutNullPattern_ReturnsDecodedText() =>
        AssertComment("中", WithCharacterCode("UNICODE\0"u8, Encoding.Unicode.GetBytes("中")), "0232");

    [Fact]
    public void GetUserComment_LegacyLittleEndianBytesThatAreValidUtf8_ReturnsDecodedText() =>
        AssertComment("āĂ", WithCharacterCode("UNICODE\0"u8, Encoding.Unicode.GetBytes("āĂ")), "0232");

    [Fact]
    public void GetUserComment_LegacyBigEndianUnicodeBom_ReturnsDecodedText() =>
        AssertComment(UnicodeComment, WithCharacterCode("UNICODE\0"u8,
            Encoding.BigEndianUnicode.GetPreamble()
                .Concat(Encoding.BigEndianUnicode.GetBytes(UnicodeComment + '\0'))
                .ToArray()), "0232");

    [Fact]
    public void GetUserComment_LegacyBigEndianUnicodeWithoutBom_ReturnsDecodedText() =>
        AssertComment("Big endian comment", WithCharacterCode("UNICODE\0"u8,
            Encoding.BigEndianUnicode.GetBytes("Big endian comment\0")), "0232");

    [Fact]
    public void GetUserComment_LegacyBigEndianUnicodeWithoutBomOrNullPattern_ReturnsDecodedText()
    {
        var profile = new ExifProfile(Convert.FromHexString(
            "4578696600004D4D002A00000008000187690004000000010000001A000000000002900000070000000430323332928600070000000C0000003800000000554E49434F4445004E2D6587"));

        var actual = ExifReader.GetUserComment(profile);

        Assert.Equal("中文", actual);
    }

    [Fact]
    public void GetUserComment_AsciiComment_ReturnsTextWithoutCharacterCode() =>
        AssertComment("ASCII comment",
            WithCharacterCode("ASCII\0\0\0"u8, Encoding.ASCII.GetBytes("ASCII comment\0")));

    [Fact]
    public void GetUserComment_AsciiMarkedUtf8Comment_ReturnsDecodedText() =>
        AssertComment(UnicodeComment,
            WithCharacterCode("ASCII\0\0\0"u8, Encoding.UTF8.GetBytes(UnicodeComment + '\0')));

    [Fact]
    public void GetUserComment_AsciiMarkedUtf16Comment_ReturnsDecodedText() =>
        AssertComment(UnicodeComment,
            WithCharacterCode("ASCII\0\0\0"u8, Encoding.Unicode.GetBytes(UnicodeComment + '\0')));

    [Fact]
    public void GetUserComment_UndefinedUtf8Comment_ReturnsDecodedText() =>
        AssertComment(UnicodeComment, WithCharacterCode(new byte[8], Encoding.UTF8.GetBytes(UnicodeComment)));

    [Fact]
    public void GetUserComment_UnsupportedJisComment_ReturnsEmpty() =>
        AssertComment(string.Empty, WithCharacterCode("JIS\0\0\0\0\0"u8, [0x1b, 0x24, 0x42, 0x24, 0x22]));

    [Theory]
    [InlineData("Legacy comment without prefix")]
    [InlineData("Short")]
    public void GetUserComment_LegacyPrefixlessComment_PreservesEntireText(string expected) =>
        AssertComment(expected, Encoding.ASCII.GetBytes(expected));

    private static void AssertComment(string expected, byte[] value, string? exifVersion = null)
    {
        var profile = new ExifProfile();
        profile.SetValue(ExifTag.UserComment, value);
        if (exifVersion is not null)
        {
            profile.SetValue(ExifTag.ExifVersion, Encoding.ASCII.GetBytes(exifVersion));
        }

        var actual = ExifReader.GetUserComment(profile);

        Assert.Equal(expected, actual);
    }

    private static byte[] WithCharacterCode(ReadOnlySpan<byte> characterCode, byte[] comment)
    {
        var value = new byte[characterCode.Length + comment.Length];
        characterCode.CopyTo(value);
        comment.CopyTo(value, characterCode.Length);
        return value;
    }
}
