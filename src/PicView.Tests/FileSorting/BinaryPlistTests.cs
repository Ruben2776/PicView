#if MACOS
using PicView.Core.MacOS.FileSorting;

namespace PicView.Tests.FileSorting;

/// <summary>
/// The binary property lists Finder keeps its view settings in (ADR-0050, On macOS): what is read from the two shapes,
/// and that anything malformed reads as no answer.
/// </summary>
public class BinaryPlistTests
{
    [Fact]
    public void TryReadString_GivesTheStringUnderTheKey()
    {
        Assert.True(BinaryPlist.TryReadString(FinderPlistFixtures.Icon, "arrangeBy", out var value));
        Assert.Equal("dateLastOpened", value);
    }

    [Theory]
    [InlineData("nope")]
    [InlineData("iconSize")]
    [InlineData("showItemInfo")]
    [InlineData("viewOptionsVersion")]
    public void TryReadString_KeyThatIsMissingOrNotAString_IsFalse(string key)
    {
        Assert.False(BinaryPlist.TryReadString(FinderPlistFixtures.Icon, key, out var value));
        Assert.Null(value);
    }

    [Fact]
    public void TryReadColumnAscending_ColumnsAsAnArray() => AssertColumns(FinderPlistFixtures.ListNew);

    [Fact]
    public void TryReadColumnAscending_ColumnsAsADictionary() => AssertColumns(FinderPlistFixtures.ListOld);

    [Fact]
    public void TryReadColumnAscending_ListWithNoColumns_IsFalse() =>
        Assert.False(BinaryPlist.TryReadColumnAscending(FinderPlistFixtures.Icon, "name", out _));

    private static void AssertColumns(byte[] plist)
    {
        Assert.True(BinaryPlist.TryReadColumnAscending(plist, "dateLastOpened", out var ascending));
        Assert.False(ascending);
        Assert.True(BinaryPlist.TryReadColumnAscending(plist, "name", out ascending));
        Assert.True(ascending);
        Assert.False(BinaryPlist.TryReadColumnAscending(plist, "size", out _));
    }

    public static TheoryData<string, byte[]> Malformed => new()
    {
        { "empty", [] },
        { "header only", "bplist00"u8.ToArray() },
        { "not a plist", "<?xml version=\"1.0\"?><plist/>padding padding padding padding"u8.ToArray() },
        { "trailer zeroed", Mutate(FinderPlistFixtures.Icon, b => b.AsSpan(b.Length - 32).Clear()) },
        { "offset table 0xFF", Mutate(FinderPlistFixtures.Icon, b => OffsetTable(b).Fill(0xFF)) },
        { "offset table zero", Mutate(FinderPlistFixtures.Icon, b => OffsetTable(b).Clear()) },
        { "huge object count", Mutate(FinderPlistFixtures.Icon, b => b.AsSpan(b.Length - 24, 8).Fill(0xFF)) },
        { "huge table position", Mutate(FinderPlistFixtures.Icon, b => b.AsSpan(b.Length - 8, 8).Fill(0x7F)) },
        { "zero offset size", Mutate(FinderPlistFixtures.Icon, b => b[^26] = 0) },
        { "huge offset size", Mutate(FinderPlistFixtures.Icon, b => b[^26] = 0xFF) },
        { "huge reference size", Mutate(FinderPlistFixtures.Icon, b => b[^25] = 0xFF) },
        { "top object out of range", Mutate(FinderPlistFixtures.Icon, b => b.AsSpan(b.Length - 16, 8).Fill(0x7F)) },
        { "every byte of the body 0xFF", Mutate(FinderPlistFixtures.Icon, b => b.AsSpan(8, b.Length - 40).Fill(0xFF)) },
        { "every byte of the body 0xDF", Mutate(FinderPlistFixtures.ListNew, b => b.AsSpan(8, b.Length - 40).Fill(0xDF)) },
    };

    [Theory]
    [MemberData(nameof(Malformed))]
    public void MalformedInput_IsFalse_AndDoesNotThrow(string reason, byte[] plist)
    {
        Assert.False(BinaryPlist.TryReadString(plist, "arrangeBy", out var value), reason);
        Assert.Null(value);
        Assert.False(BinaryPlist.TryReadColumnAscending(plist, "name", out _), reason);
    }

    [Fact]
    public void TruncatedAtEveryLength_NeverGivesAWrongValue()
    {
        var whole = FinderPlistFixtures.Icon;
        for (var length = 0; length < whole.Length; length++)
        {
            var found = BinaryPlist.TryReadString(whole.AsSpan(0, length), "arrangeBy", out var value);
            Assert.False(found && value != "dateLastOpened", $"length {length} gave {value}");
        }

        Assert.True(BinaryPlist.TryReadString(whole, "arrangeBy", out _));
    }

    [Fact]
    public void EveryByteChanged_NeverThrows()
    {
        foreach (var plist in new[] { FinderPlistFixtures.Icon, FinderPlistFixtures.ListNew, FinderPlistFixtures.ListOld })
        {
            foreach (var replacement in new byte[] { 0x00, 0x01, 0x7F, 0x80, 0xAF, 0xDF, 0xFF })
            {
                for (var at = 0; at < plist.Length; at++)
                {
                    var changed = (byte[])plist.Clone();
                    changed[at] = replacement;
                    BinaryPlist.TryReadString(changed, "arrangeBy", out _);
                    BinaryPlist.TryReadColumnAscending(changed, "dateLastOpened", out _);
                }
            }
        }
    }

    private static byte[] Mutate(byte[] plist, Action<byte[]> change)
    {
        var copy = (byte[])plist.Clone();
        change(copy);
        return copy;
    }

    /// <summary>The offset table of <paramref name="plist"/>, as its trailer says where it is.</summary>
    private static Span<byte> OffsetTable(byte[] plist)
    {
        var offsetSize = plist[^26];
        var count = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(plist.AsSpan(plist.Length - 24));
        var start = (int)System.Buffers.Binary.BinaryPrimitives.ReadUInt64BigEndian(plist.AsSpan(plist.Length - 8));
        return plist.AsSpan(start, count * offsetSize);
    }
}
#endif
