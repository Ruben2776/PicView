#if MACOS
using PicView.Core.FileSorting;
using PicView.Core.MacOS.FileSorting;

namespace PicView.Tests.FileSorting;

/// <summary>
/// Which Finder windows give a File Order, and which do not (ADR-0050, On macOS). The read of a live Finder window is
/// not tested here: it needs one.
/// </summary>
public class FinderSortTests
{
    private const string Folder = "/Users/someone/Pictures";

    public static TheoryData<string, SortFilesBy> Columns => new()
    {
        { "elsn", SortFilesBy.Name },
        { "elss", SortFilesBy.FileSize },
        { "elsm", SortFilesBy.LastWriteTime },
        { "elsc", SortFilesBy.CreationTime },
    };

    private static FinderWindow ListView(string column, bool ascending = true, string? folder = Folder) =>
        new(folder, FinderSort.ListView, FinderSort.Code(column), ascending ? FinderSort.Normal : FinderSort.Reversed);

    [Theory]
    [MemberData(nameof(Columns))]
    public void ToFileOrder_ColumnWithAField_IsThatField_InTheWindowsDirection(string column, SortFilesBy field)
    {
        Assert.Equal(new FileOrder(field, true), FinderSort.ToFileOrder(ListView(column)));
        Assert.Equal(new FileOrder(field, false), FinderSort.ToFileOrder(ListView(column, ascending: false)));
    }

    [Theory]
    [InlineData("elsl")] // Tags
    [InlineData("elsC")] // Comments
    [InlineData("elsv")] // Version
    public void ToFileOrder_ColumnWithNoField_IsNotFollowed(string column) =>
        Assert.Null(FinderSort.ToFileOrder(ListView(column)));

    [Theory]
    [InlineData("clvw")] // column view
    [InlineData("flvw")] // gallery view
    public void ToFileOrder_ColumnOrGalleryView_IsNotFollowed(string view)
    {
        var window = ListView("elsn") with { View = FinderSort.Code(view) };

        Assert.Null(FinderSort.ToFileOrder(window));
    }

    public static TheoryData<string, SortFilesBy, bool> Arrangements => new()
    {
        { "nama", SortFilesBy.Name, true },
        { "kina", SortFilesBy.Extension, true },
        { "siza", SortFilesBy.FileSize, false },
        { "mdta", SortFilesBy.LastWriteTime, false },
        { "cdta", SortFilesBy.CreationTime, false },
    };

    private static FinderWindow IconView(string arrangement, string? folder = Folder) =>
        new(folder, FinderSort.IconView, 0, 0, FinderSort.Code(arrangement));

    [Theory]
    [MemberData(nameof(Arrangements))]
    public void ToFileOrder_IconView_IsItsArrangement_InFindersFixedDirection(string arrangement, SortFilesBy field, bool ascending) =>
        Assert.Equal(new FileOrder(field, ascending), FinderSort.ToFileOrder(IconView(arrangement)));

    [Theory]
    [InlineData("narr")] // not arranged
    [InlineData("grda")] // snap to grid
    [InlineData("laba")] // by tags
    public void ToFileOrder_IconViewWithNoArrangementAFieldGives_IsNotFollowed(string arrangement) =>
        Assert.Null(FinderSort.ToFileOrder(IconView(arrangement)));

    [Fact]
    public void ToFileOrder_IconView_IgnoresTheListViewsSortColumn()
    {
        var window = IconView("mdta") with { SortedBy = FinderSort.Name, Direction = FinderSort.Normal };

        Assert.Equal(new FileOrder(SortFilesBy.LastWriteTime, false), FinderSort.ToFileOrder(window));
    }

    [Fact]
    public void ToFileOrder_ListViewByKind_IsExtension_InTheWindowsDirection()
    {
        Assert.Equal(new FileOrder(SortFilesBy.Extension, true), FinderSort.ToFileOrder(ListView("elsk")));
        Assert.Equal(new FileOrder(SortFilesBy.Extension, false), FinderSort.ToFileOrder(ListView("elsk", ascending: false)));
    }

    [Fact]
    public void ToFileOrder_DirectionNotRead_IsNotFollowed() =>
        Assert.Null(FinderSort.ToFileOrder(ListView("elsn") with { Direction = 0 }));

    [Fact]
    public void Followed_TakesTheFirstWindowOnTheFolder_AsTheWindowsComeFrontToBack()
    {
        FinderWindow[] windows =
        [
            ListView("elsn", folder: "/Users/someone/Downloads"),
            ListView("elss", ascending: false),
            ListView("elsm"),
        ];

        Assert.Equal(new FileOrder(SortFilesBy.FileSize, false), FinderSort.Followed(windows, Folder));
    }

    [Fact]
    public void Followed_FrontWindowInNoFileOrder_IsNotFollowed_ThoughOneBehindItIs()
    {
        FinderWindow[] windows = [ListView("elsl"), ListView("elsn")];

        Assert.Null(FinderSort.Followed(windows, Folder));
    }

    [Fact]
    public void Followed_NoWindowOnTheFolder_IsNotFollowed()
    {
        FinderWindow[] windows = [ListView("elsn", folder: "/Users/someone/Downloads"), ListView("elsn", folder: null)];

        Assert.Null(FinderSort.Followed(windows, Folder));
        Assert.Null(FinderSort.Followed([], Folder));
    }

    [Theory]
    [InlineData("/Users/someone/Pictures/")] // as Finder gives a folder
    [InlineData("/users/someone/pictures")]
    public void Followed_MatchesTheFolderWithoutCase_WhateverSeparatorEndsIt(string shown)
    {
        FinderWindow[] windows = [ListView("elsm", folder: shown)];

        Assert.Equal(new FileOrder(SortFilesBy.LastWriteTime, true), FinderSort.Followed(windows, Folder));
    }

    [Fact]
    public void Followed_MatchesAnAccentedFolder_HoweverEitherSideWritesTheAccent()
    {
        const string composed = "/Users/someone/Bl\u00E5";
        const string decomposed = "/Users/someone/Bla\u030A";
        FinderWindow[] windows = [ListView("elss", folder: decomposed)];

        Assert.Equal(new FileOrder(SortFilesBy.FileSize, true), FinderSort.Followed(windows, composed));
    }

    [Fact]
    public void Followed_SubfolderOfTheFolder_IsNotAMatch()
    {
        FinderWindow[] windows = [ListView("elsn", folder: Folder + "/Avatar")];

        Assert.Null(FinderSort.Followed(windows, Folder));
    }
}
#endif
