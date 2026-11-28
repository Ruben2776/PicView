#if MACOS
using PicView.Core.MacOS.FileSorting;
using static PicView.Tests.FileSorting.DsStoreBytes;

namespace PicView.Tests.FileSorting;

/// <summary>
/// A folder's saved Finder view, read from the <c>.DS_Store</c> of its parent (ADR-0050, On macOS): the four values
/// taken for one name, and that anything malformed reads as no answer.
/// </summary>
public class DsStoreTests
{
    private const string Folder = "Characters";

    private static readonly FinderSavedView NewList =
        new("Date Last Opened", "dateLastOpened", "dateLastOpened", false);

    private static (string, string, string, byte[]) R(string name, string code, string type, byte[] value) =>
        (name, code, type, value);

    private static (string, string, string, byte[]) Group(string name = Folder) =>
        R(name, "GRP0", "ustr", Ustr("Date Last Opened"));

    private static (string, string, string, byte[]) IconView(string name = Folder) =>
        R(name, "icvp", "blob", Blob(FinderPlistFixtures.Icon));

    private static (string, string, string, byte[]) NewListView(string name = Folder) =>
        R(name, "lsvC", "blob", Blob(FinderPlistFixtures.ListNew));

    private static (string, string, string, byte[]) OldListView(string name = Folder) =>
        R(name, "lsvp", "blob", Blob(FinderPlistFixtures.ListOld));

    private static byte[] TwoLevelFile() => Build(
        [Group("Alpha"), IconView("Alpha"), R("Beta", "vSrn", "long", Long(1))],
        [IconView("Delta"), NewListView("Delta")],
        [Group(), IconView(), NewListView(), OldListView("Other")]);

    [Fact]
    public void ViewOf_GivesTheFoldersGroupArrangementAndListSort()
    {
        var file = Build([Group(), IconView(), NewListView(), IconView("Other")]);

        Assert.Equal(NewList, DsStore.ViewOf(file, Folder));
    }

    [Fact]
    public void ViewOf_FallsBackToTheOlderListRecord()
    {
        var file = Build([OldListView()]);

        Assert.Equal(new FinderSavedView(null, null, "name", true), DsStore.ViewOf(file, Folder));
    }

    [Fact]
    public void ViewOf_PrefersTheNewerListRecord()
    {
        var file = Build([OldListView(), NewListView()]);

        Assert.Equal(new FinderSavedView(null, null, "dateLastOpened", false), DsStore.ViewOf(file, Folder));
    }

    [Fact]
    public void ViewOf_FolderWithNoRecords_IsNull()
    {
        var file = Build([Group("Other"), IconView("Other"), NewListView("Other")]);

        Assert.Null(DsStore.ViewOf(file, Folder));
    }

    [Fact]
    public void ViewOf_FindsTheFolderAcrossAnInnerNode()
    {
        // The middle leaf's last record, the folder's group, is moved up into the root.
        var file = Build(
            [Group("Alpha"), IconView("Alpha")],
            [IconView("Delta"), Group()],
            [IconView(), NewListView(), OldListView("Other")]);

        Assert.Equal(NewList, DsStore.ViewOf(file, Folder));
        Assert.Equal(NewList, DsStore.ViewOf(TwoLevelFile(), Folder));
    }

    [Theory]
    [InlineData("characters")]
    [InlineData("CHARACTERS")]
    public void ViewOf_MatchesTheNameWithoutCase(string asked)
    {
        var file = Build([Group(), IconView(), NewListView()]);

        Assert.Equal(NewList, DsStore.ViewOf(file, asked));
    }

    [Fact]
    public void ViewOf_MatchesAnAccentedNameHoweverItIsWritten()
    {
        const string decomposed = "Blå", composed = "Blå";

        Assert.Equal("dateLastOpened", DsStore.ViewOf(Build([IconView(decomposed)]), composed)?.IconArrangeBy);
        Assert.Equal("dateLastOpened", DsStore.ViewOf(Build([IconView(composed)]), decomposed)?.IconArrangeBy);
    }

    [Fact]
    public void ViewOf_SkipsRecordsOfOtherTypesBetween()
    {
        var file = Build([
            R(Folder, "Iloc", "blob", Blob(new byte[16])),
            Group(),
            R(Folder, "vSrn", "long", Long(1)),
            IconView(),
            R(Folder, "moDD", "blob", Blob(new byte[8])),
            R(Folder, "lg1S", "comp", new byte[8]),
            NewListView(),
            R(Folder, "dscl", "bool", [1]),
            R("Other", "ph1S", "comp", new byte[8])
        ]);

        Assert.Equal(NewList, DsStore.ViewOf(file, Folder));
    }

    [Fact]
    public void ViewOf_IgnoresABlobThatIsNoPlist()
    {
        var noise = new byte[40];
        new Random(328).NextBytes(noise);
        var file = Build([R(Folder, "icvp", "blob", Blob(noise)), Group()]);

        Assert.Equal(new FinderSavedView("Date Last Opened", null, null, null), DsStore.ViewOf(file, Folder));
    }

    [Fact]
    public void ViewOf_NotADsStore_IsNull()
    {
        var renamed = Build([Group()]);
        "Bud2"u8.CopyTo(renamed.AsSpan(4));

        Assert.Null(DsStore.ViewOf([], Folder));
        Assert.Null(DsStore.ViewOf(new byte[100], Folder));
        Assert.Null(DsStore.ViewOf(renamed, Folder));
    }

    [Fact]
    public void ViewOf_FileCutShort_IsNull_AndDoesNotThrow()
    {
        var file = TwoLevelFile();

        for (var length = 0; length < file.Length; length++)
        {
            Assert.Null(DsStore.ViewOf(file.AsSpan(0, length), Folder));
        }
    }

    [Fact]
    public void ViewOf_NodesThatPointAtEachOther_ReturnsNull()
    {
        var file = TwoLevelFile();
        PointTheRootsFirstChildAtItself(file);

        Assert.Null(DsStore.ViewOf(file, Folder));
    }

    /// <summary>Whatever one byte of a file is changed to, the read ends without throwing.</summary>
    [Fact]
    public void ViewOf_AnyByteChanged_DoesNotThrow()
    {
        var file = TwoLevelFile();
        byte[] replacements = [0x00, 0x01, 0x1F, 0x7F, 0xFF];

        for (var i = 0; i < file.Length; i++)
        {
            var original = file[i];
            foreach (var replacement in replacements)
            {
                file[i] = replacement;
                _ = DsStore.ViewOf(file, Folder);
            }

            file[i] = original;
        }
    }
}
#endif
