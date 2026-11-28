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
    [InlineData("elsk")] // Kind
    [InlineData("elsl")] // Tags
    [InlineData("elsC")] // Comments
    [InlineData("elsv")] // Version
    public void ToFileOrder_ColumnWithNoField_IsNotFollowed(string column) =>
        Assert.Null(FinderSort.ToFileOrder(ListView(column)));

    [Theory]
    [InlineData("icnv")] // icon view
    [InlineData("clvw")] // column view
    [InlineData("flvw")] // gallery view
    public void ToFileOrder_ViewOtherThanTheListView_IsNotFollowed(string view)
    {
        var window = ListView("elsn") with { View = FinderSort.Code(view) };

        Assert.Null(FinderSort.ToFileOrder(window));
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
        FinderWindow[] windows = [ListView("elsk"), ListView("elsn")];

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
    public void Followed_SubfolderOfTheFolder_IsNotAMatch()
    {
        FinderWindow[] windows = [ListView("elsn", folder: Folder + "/Avatar")];

        Assert.Null(FinderSort.Followed(windows, Folder));
    }
}
#endif
