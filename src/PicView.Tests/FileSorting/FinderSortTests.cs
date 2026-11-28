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

    private static FinderSavedView Saved(string? group = null, string? icon = null, string? list = null, bool? ascending = null) =>
        new(group, icon, list, ascending);

    private static FinderWindow ViewOf(string view) => ListView("elsn") with { View = FinderSort.Code(view) };

    public static TheoryData<string, SortFilesBy, bool> Groups => new()
    {
        { "Date Last Opened", SortFilesBy.LastAccessTime, false },
        { "Date Modified", SortFilesBy.LastWriteTime, false },
        { "Date Created", SortFilesBy.CreationTime, false },
        { "Size", SortFilesBy.FileSize, false },
        { "Kind", SortFilesBy.Extension, true },
        { "Name", SortFilesBy.Name, true },
    };

    [Theory]
    [MemberData(nameof(Groups))]
    public void Grouped_IsFollowedByItsGroupField_WhateverTheViewAndItsSort(string group, SortFilesBy field, bool ascending)
    {
        var expected = new FileOrder(field, ascending);
        var saved = Saved(group);

        Assert.Equal(expected, FinderSort.ToFileOrder(ListView("elss"), saved));
        Assert.Equal(expected, FinderSort.ToFileOrder(IconView("mdta"), saved));
        Assert.Equal(expected, FinderSort.ToFileOrder(ViewOf("clvw"), saved));
        Assert.Equal(expected, FinderSort.ToFileOrder(ViewOf("flvw"), saved));
    }

    [Theory]
    [InlineData("Date Added")]
    [InlineData("Application")]
    [InlineData("Tags")]
    [InlineData("Dato sidst \u00e5bnet")]
    [InlineData("")]
    public void Grouped_ByAFieldWithNoCounterpartOrAnUnknownOne_IsNotFollowed(string group) =>
        Assert.Null(FinderSort.ToFileOrder(ListView("elsn"), Saved(group)));

    [Theory]
    [InlineData("None")]
    [InlineData(null)]
    public void NotGrouped_IsFollowedAsTheViewIs(string? group) =>
        Assert.Equal(new FileOrder(SortFilesBy.FileSize, false),
            FinderSort.ToFileOrder(ListView("elss", ascending: false), Saved(group)));

    [Fact]
    public void IconView_NotArranged_SavedAsDateLastOpened_IsLastAccessTimeNewestFirst() =>
        Assert.Equal(new FileOrder(SortFilesBy.LastAccessTime, false),
            FinderSort.ToFileOrder(IconView("narr"), Saved(icon: "dateLastOpened")));

    [Theory]
    [InlineData("dateAdded")]
    [InlineData("none")]
    [InlineData(null)]
    public void IconView_NotArranged_SavedAsAnythingElse_IsNotFollowed(string? icon) =>
        Assert.Null(FinderSort.ToFileOrder(IconView("narr"), Saved(icon: icon)));

    [Theory]
    [InlineData("dateModified")]
    [InlineData("name")]
    [InlineData("size")]
    public void IconView_NotArranged_SavedValueScriptingWouldHaveNamed_IsStale_AndNotFollowed(string icon) =>
        Assert.Null(FinderSort.ToFileOrder(IconView("narr"), Saved(icon: icon)));

    [Fact]
    public void IconView_Arranged_IgnoresTheSavedArrangement() =>
        Assert.Equal(new FileOrder(SortFilesBy.LastWriteTime, false),
            FinderSort.ToFileOrder(IconView("mdta"), Saved(icon: "dateLastOpened")));

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(null, false)]
    public void ListView_Name_SavedAsDateLastOpened_IsLastAccessTime_InTheSavedDirection(bool? saved, bool expected) =>
        Assert.Equal(new FileOrder(SortFilesBy.LastAccessTime, expected),
            FinderSort.ToFileOrder(ListView("elsn"), Saved(list: "dateLastOpened", ascending: saved)));

    [Theory]
    [InlineData("dateAdded")]
    [InlineData("label")]
    [InlineData("comments")]
    [InlineData("version")]
    public void ListView_Name_SavedAsAColumnWithNoCounterpart_IsNotFollowed(string list) =>
        Assert.Null(FinderSort.ToFileOrder(ListView("elsn"), Saved(list: list)));

    [Theory]
    [InlineData("name")]
    [InlineData("dateModified")]
    [InlineData("size")]
    [InlineData("somethingNew")]
    [InlineData(null)]
    public void ListView_Name_SavedAsNameOrStaleOrUnknown_StaysName_InTheWindowsDirection(string? list)
    {
        Assert.Equal(new FileOrder(SortFilesBy.Name, true), FinderSort.ToFileOrder(ListView("elsn"), Saved(list: list)));
        Assert.Equal(new FileOrder(SortFilesBy.Name, false),
            FinderSort.ToFileOrder(ListView("elsn", ascending: false), Saved(list: list)));
    }

    [Fact]
    public void ListView_ColumnOtherThanName_IgnoresTheSavedColumn() =>
        Assert.Equal(new FileOrder(SortFilesBy.LastWriteTime, true),
            FinderSort.ToFileOrder(ListView("elsm"), Saved(list: "dateLastOpened")));

    [Fact]
    public void NoSavedView_IsAsScriptingAloneSays()
    {
        Assert.Equal(new FileOrder(SortFilesBy.Name, true), FinderSort.ToFileOrder(ListView("elsn"), null));
        Assert.Equal(new FileOrder(SortFilesBy.LastWriteTime, false), FinderSort.ToFileOrder(IconView("mdta"), null));
        Assert.Null(FinderSort.ToFileOrder(IconView("narr"), null));
    }

    [Fact]
    public void Followed_UsesTheSavedViewForTheFirstWindowOnTheFolder()
    {
        const string other = "/Users/someone/Other";
        FinderWindow[] windows = [ListView("elsn", folder: other), IconView("narr"), ListView("elss")];

        Assert.Equal(new FileOrder(SortFilesBy.LastAccessTime, false),
            FinderSort.Followed(windows, Folder, Saved(icon: "dateLastOpened")));
    }
}
#endif
