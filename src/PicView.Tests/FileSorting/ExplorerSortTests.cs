#if WINDOWS
using PicView.Core.FileSorting;
using PicView.Core.WindowsNT.FileSorting;

namespace PicView.Tests.FileSorting;

/// <summary>
/// Which Explorer views give a File Order, and which do not (ADR-0050). The read of a live Explorer window is not
/// tested here: it needs one.
/// </summary>
public class ExplorerSortTests
{
    private static readonly ShellPropertyKey DateTaken = new(new Guid("14B81DA1-0135-4D31-96D9-6CBFC9671A99"), 36867);
    private static readonly ShellPropertyKey ItemDate = new(new Guid("F7DB74B4-4287-4103-AFBA-F1B13DCD75CF"), 100);

    public static TheoryData<string, SortFilesBy> Columns => new()
    {
        { nameof(ExplorerSort.Name), SortFilesBy.Name },
        { nameof(ExplorerSort.Size), SortFilesBy.FileSize },
        { nameof(ExplorerSort.DateModified), SortFilesBy.LastWriteTime },
        { nameof(ExplorerSort.DateCreated), SortFilesBy.CreationTime },
        { nameof(ExplorerSort.DateAccessed), SortFilesBy.LastAccessTime },
    };

    private static ShellPropertyKey KeyOf(string column) => column switch
    {
        nameof(ExplorerSort.Name) => ExplorerSort.Name,
        nameof(ExplorerSort.Size) => ExplorerSort.Size,
        nameof(ExplorerSort.DateModified) => ExplorerSort.DateModified,
        nameof(ExplorerSort.DateCreated) => ExplorerSort.DateCreated,
        nameof(ExplorerSort.DateAccessed) => ExplorerSort.DateAccessed,
        _ => throw new ArgumentOutOfRangeException(nameof(column))
    };

    [Theory]
    [MemberData(nameof(Columns))]
    public void ToFileOrder_ColumnWithAField_IsThatField_InTheViewsDirection(string column, SortFilesBy field)
    {
        var key = KeyOf(column);

        Assert.Equal(new FileOrder(field, true), ExplorerSort.ToFileOrder(new ExplorerView(key, true, null)));
        Assert.Equal(new FileOrder(field, false), ExplorerSort.ToFileOrder(new ExplorerView(key, false, null)));
    }

    [Fact]
    public void ToFileOrder_ColumnWithoutAField_IsNull()
    {
        // Type sorts by the type's name, so .jpg and .jpeg are one, which the Extension field does not give
        Assert.Null(ExplorerSort.ToFileOrder(new ExplorerView(ExplorerSort.TypeText, true, null)));
        Assert.Null(ExplorerSort.ToFileOrder(new ExplorerView(DateTaken, true, null)));
        Assert.Null(ExplorerSort.ToFileOrder(new ExplorerView(ItemDate, false, null)));
    }

    [Fact]
    public void ToFileOrder_ViewWithoutASortColumn_IsNull() =>
        Assert.Null(ExplorerSort.ToFileOrder(new ExplorerView(null, true, null)));

    [Fact]
    public void ToFileOrder_GroupedByTheColumnItIsSortedBy_InTheSameDirection_IsThatOrder()
    {
        // As Downloads is by default: grouped and sorted by date modified, newest first
        var view = new ExplorerView(ExplorerSort.DateModified, false, ExplorerSort.DateModified, false);

        Assert.Equal(new FileOrder(SortFilesBy.LastWriteTime, false), ExplorerSort.ToFileOrder(view));
    }

    [Fact]
    public void ToFileOrder_GroupedByAnotherColumn_IsNull()
    {
        var view = new ExplorerView(ExplorerSort.Name, true, ExplorerSort.DateModified, false);

        Assert.Null(ExplorerSort.ToFileOrder(view));
    }

    [Fact]
    public void ToFileOrder_GroupsInTheOtherDirectionThanTheSort_IsNull()
    {
        // The groups run oldest first while each group's files run newest first, which no one field and direction gives
        var view = new ExplorerView(ExplorerSort.DateModified, false, ExplorerSort.DateModified, true);

        Assert.Null(ExplorerSort.ToFileOrder(view));
    }

    [Fact]
    public void Agreed_NoView_IsNull() => Assert.Null(ExplorerSort.Agreed([]));

    [Fact]
    public void Agreed_ViewsThatGiveOneOrder_IsThatOrder()
    {
        var bySize = new ExplorerView(ExplorerSort.Size, false, null);

        Assert.Equal(new FileOrder(SortFilesBy.FileSize, false), ExplorerSort.Agreed([bySize]));
        Assert.Equal(new FileOrder(SortFilesBy.FileSize, false), ExplorerSort.Agreed([bySize, bySize]));
    }

    [Fact]
    public void Agreed_ViewsThatDiffer_IsNull()
    {
        var bySize = new ExplorerView(ExplorerSort.Size, false, null);

        Assert.Null(ExplorerSort.Agreed([bySize, new ExplorerView(ExplorerSort.Size, true, null)]));
        Assert.Null(ExplorerSort.Agreed([bySize, new ExplorerView(ExplorerSort.Name, false, null)]));
    }

    [Fact]
    public void Agreed_OneViewWithoutAFileOrder_IsNull()
    {
        var bySize = new ExplorerView(ExplorerSort.Size, false, null);

        Assert.Null(ExplorerSort.Agreed([bySize, new ExplorerView(DateTaken, false, null)]));
        Assert.Null(ExplorerSort.Agreed([new ExplorerView(DateTaken, false, null), bySize]));
    }

    [Fact]
    public void OrderOf_AFolderNoExplorerWindowShows_IsNull_AndThrowsNothing()
    {
        // The one call that reaches the real shell: a folder made a moment ago, which no window can show
        var folder = Path.Combine(Path.GetTempPath(), "PicViewExplorerSortTests_" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            Assert.Null(new ExplorerFolderOrder().OrderOf(folder));
        }
        finally
        {
            Directory.Delete(folder);
        }
    }
}
#endif
