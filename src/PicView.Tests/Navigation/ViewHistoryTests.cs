using PicView.Core.Models;
using PicView.Core.Navigation;

namespace PicView.Tests.Navigation;

public class ViewHistoryTests
{
    private static readonly string Root = Path.Combine(Path.GetTempPath(), "PicViewViewHistory");
    private readonly HashSet<string> _missing = new(StringComparer.OrdinalIgnoreCase);
    private readonly ViewHistory _history;

    public ViewHistoryTests() => _history = new ViewHistory(path => !_missing.Contains(path));

    [Fact]
    public void Record_NewEntries_AddsThemWithTheCursorOnTheNewest()
    {
        RecordAll("a.jpg", "b.jpg");

        Assert.Equal(["a.jpg", "b.jpg"], Names());
        Assert.Equal(1, _history.Cursor);
    }

    [Fact]
    public void Record_TheCursorsEntryAgain_AddsNothing()
    {
        RecordAll("a.jpg");

        Assert.False(_history.Record(FileEntry("a.jpg")));
        Assert.Equal(1, _history.Count);
    }

    [Fact]
    public void Record_SamePathInAnotherCase_IsARepeat()
    {
        _history.Record(ViewHistoryEntry.ForFile(@"C:\Pics\A.jpg"));

        Assert.False(_history.Record(ViewHistoryEntry.ForFile(@"c:\pics\a.jpg")));
    }

    [Fact]
    public void Record_AnotherPageOfTheSameTiff_IsANewEntry()
    {
        _history.Record(FileEntry("t.tif", page: 0));

        Assert.True(_history.Record(FileEntry("t.tif", page: 1)));
    }

    [Fact]
    public void Record_SomethingNewAfterGoingBack_DropsTheEntriesAfterTheCursor()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg");
        _history.Record(_history.BeginStep(forwards: false)!.Value.Target);

        _history.Record(FileEntry("d.jpg"));

        Assert.Equal(["a.jpg", "b.jpg", "d.jpg"], Names());
        Assert.Equal(2, _history.Cursor);
    }

    [Fact]
    public void Record_101stEntry_DropsTheOldest()
    {
        for (var i = 0; i <= ViewHistory.MaxEntries; i++)
        {
            _history.Record(FileEntry($"{i}.jpg"));
        }

        Assert.Equal(ViewHistory.MaxEntries, _history.Count);
        Assert.Equal("1.jpg", Names()[0]);
        Assert.Equal(ViewHistory.MaxEntries - 1, _history.Cursor);
    }

    [Fact]
    public void Record_11thSingleImage_DropsTheOldestSingleImageWhereverItIs()
    {
        var first = Clipboard();
        _history.Record(FileEntry("a.jpg"));
        _history.Record(first);
        _history.Record(FileEntry("b.jpg"));
        for (var i = 1; i <= ViewHistory.MaxSingleImages; i++)
        {
            _history.Record(Clipboard());
        }

        Assert.Equal(ViewHistory.MaxSingleImages, _history.Entries.Count(e => e.Kind is ViewHistoryKind.SingleImage));
        Assert.DoesNotContain(_history.Entries, e => ReferenceEquals(e.Model, first.Model));
        Assert.Equal(["a.jpg", "b.jpg"],
            _history.Entries.Where(e => e.Kind is ViewHistoryKind.File).Select(e => Path.GetFileName(e.Path)));
    }

    [Fact]
    public void Record_11thSingleImage_DroppedBetweenTwoViewsOfTheSameFile_MergesThoseViews()
    {
        _history.Record(FileEntry("a.jpg"));
        _history.Record(Clipboard());
        _history.Record(FileEntry("a.jpg"));
        for (var i = 1; i <= ViewHistory.MaxSingleImages; i++)
        {
            _history.Record(Clipboard());
        }

        Assert.Equal(1, _history.Entries.Count(e => e.Kind is ViewHistoryKind.File));
        Assert.Equal(ViewHistory.MaxSingleImages, _history.Entries.Count(e => e.Kind is ViewHistoryKind.SingleImage));
        Assert.Equal(_history.Count - 1, _history.Cursor);
    }

    [Fact]
    public void Record_SameUrlAgain_IsARepeat_AndAnotherClipboardImageIsNot()
    {
        const string url = "https://example.com/a.png";
        _history.Record(ViewHistoryEntry.ForSingleImage(new ImageModel(), SingleImageType.Url, url, url));

        Assert.False(_history.Record(ViewHistoryEntry.ForSingleImage(new ImageModel(), SingleImageType.Url, url, url)));
        _history.Record(Clipboard());
        Assert.True(_history.Record(Clipboard()));
    }

    [Fact]
    public void BeginStep_Back_MarksTheTargetWithoutMovingTheCursor()
    {
        RecordAll("a.jpg", "b.jpg");

        Assert.Equal("a.jpg", TargetName(forwards: false));
        Assert.Equal(1, _history.Cursor);
    }

    [Fact]
    public void Record_TheStepsTarget_MovesTheCursorThere()
    {
        RecordAll("a.jpg", "b.jpg");
        var step = _history.BeginStep(forwards: false)!.Value;

        Assert.True(_history.Record(step.Target));
        Assert.Equal(0, _history.Cursor);
        Assert.Equal(2, _history.Count);
    }

    [Fact]
    public void EndStep_TargetNeverShown_LeavesTheCursorAndCountsFromItAgain()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg");
        _history.EndStep(_history.BeginStep(forwards: false)!.Value);

        Assert.Equal("b.jpg", TargetName(forwards: false));
        Assert.Equal(2, _history.Cursor);
    }

    [Fact]
    public void BeginStep_ThreeTimesQuickly_GoesBackThree()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg", "d.jpg");
        _history.BeginStep(forwards: false);
        _history.BeginStep(forwards: false);

        var third = _history.BeginStep(forwards: false)!.Value;
        _history.Record(third.Target);

        Assert.Equal("a.jpg", Path.GetFileName(third.Target.Path));
        Assert.Equal(0, _history.Cursor);
        Assert.Equal(4, _history.Count);
    }

    [Fact]
    public void Record_EntryOnTheWayToTheTarget_MovesTheCursorToIt()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg", "d.jpg");
        _history.BeginStep(forwards: false);
        _history.BeginStep(forwards: false);

        Assert.True(_history.Record(FileEntry("c.jpg")));
        Assert.Equal(2, _history.Cursor);
        Assert.Equal(4, _history.Count);
    }

    [Fact]
    public void Record_SomethingElseDuringAStep_EndsTheStepAndIsAdded()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg");
        _history.BeginStep(forwards: false);

        _history.Record(FileEntry("x.jpg"));
        _history.Record(FileEntry("b.jpg"));

        Assert.Equal(["a.jpg", "b.jpg", "c.jpg", "x.jpg", "b.jpg"], Names());
        Assert.Equal(4, _history.Cursor);
    }

    [Fact]
    public void Record_StepsTargetIsAnotherViewOfTheShownEntry_TwoBacks_MovesTheCursorToIt()
    {
        RecordAll("a.jpg", "b.jpg", "a.jpg");
        _history.BeginStep(forwards: false);
        _history.BeginStep(forwards: false);

        // The load for b.jpg was overtaken, so only a.jpg shows
        Assert.True(_history.Record(FileEntry("a.jpg")));
        Assert.Equal(0, _history.Cursor);
        Assert.Equal(3, _history.Count);
    }

    [Fact]
    public void Record_StepsTargetIsAnotherViewOfTheShownEntry_TwoForwards_MovesTheCursorToIt()
    {
        RecordAll("a.jpg", "b.jpg", "a.jpg");
        _history.Record(_history.BeginStep(forwards: false)!.Value.Target);
        _history.Record(_history.BeginStep(forwards: false)!.Value.Target);
        _history.BeginStep(forwards: true);
        _history.BeginStep(forwards: true);

        // The load for b.jpg was overtaken, so only a.jpg shows
        Assert.True(_history.Record(FileEntry("a.jpg")));
        Assert.Equal(2, _history.Cursor);
        Assert.Equal(3, _history.Count);
    }

    [Fact]
    public void Record_ShownEntryAgainDuringAStep_IsARepeat_AndTheStepStaysPending()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg");
        _history.BeginStep(forwards: false);

        Assert.False(_history.Record(FileEntry("c.jpg")));
        // The step is still under way, so its target showing moves the cursor instead of adding an entry
        Assert.True(_history.Record(FileEntry("b.jpg")));
        Assert.Equal(1, _history.Cursor);
        Assert.Equal(3, _history.Count);
    }

    [Fact]
    public void BeginStep_MissingFileBehind_SkipsAndRemovesIt()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg");
        _missing.Add(PathOf("b.jpg"));

        Assert.Equal("a.jpg", TargetName(forwards: false));
        Assert.Equal(["a.jpg", "c.jpg"], Names());
        Assert.Equal(1, _history.Cursor);
    }

    [Fact]
    public void BeginStep_MissingFileAhead_SkipsAndRemovesIt()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg");
        _history.BeginStep(forwards: false);
        _history.Record(_history.BeginStep(forwards: false)!.Value.Target);
        _missing.Add(PathOf("b.jpg"));

        Assert.Equal("c.jpg", TargetName(forwards: true));
        Assert.Equal(["a.jpg", "c.jpg"], Names());
        Assert.Equal(0, _history.Cursor);
    }

    [Fact]
    public void BeginStep_MissingFileBetweenTwoViewsOfTheSameFile_MergesThemSoBackGoesOn()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg");
        // c.jpg was deleted, so the tab showed b.jpg again
        _history.Record(FileEntry("b.jpg"));
        _missing.Add(PathOf("c.jpg"));

        Assert.Equal("a.jpg", TargetName(forwards: false));
        Assert.Equal(["a.jpg", "b.jpg"], Names());
        Assert.Equal(1, _history.Cursor);
    }

    [Fact]
    public void BeginStep_MissingFileMergesTheEntryAStepIsGoingTo_CountsOnFromTheMergedEntry()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg", "b.jpg", "d.jpg");
        // The step is going to the second b.jpg, and c.jpg before it is gone
        _history.BeginStep(forwards: false);
        _missing.Add(PathOf("c.jpg"));

        Assert.Equal("a.jpg", TargetName(forwards: false));
        Assert.Equal(["a.jpg", "b.jpg", "d.jpg"], Names());
        Assert.Equal(2, _history.Cursor);
    }

    [Fact]
    public void BeginStep_AtEitherEnd_ReturnsNothing()
    {
        Assert.Null(_history.BeginStep(forwards: false));
        RecordAll("a.jpg");

        Assert.Null(_history.BeginStep(forwards: false));
        Assert.Null(_history.BeginStep(forwards: true));
    }

    [Fact]
    public void BeginStep_AfterTheShownFileWasDeleted_StepsToTheNewestEarlierEntryThatExists()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg");
        // The tab's files were deleted, so it shows the start-up menu
        _missing.Add(PathOf("c.jpg"));

        var back = _history.BeginStep(forwards: false)!.Value;
        _history.Record(back.Target);

        Assert.Equal("b.jpg", Path.GetFileName(back.Target.Path));
        Assert.Null(_history.BeginStep(forwards: true));
        Assert.Equal(["a.jpg", "b.jpg"], Names());
    }

    [Fact]
    public void BeginStep_ForwardBeforeABackShowed_TargetsTheShownEntryWithNoStepLeft()
    {
        RecordAll("a.jpg", "b.jpg", "c.jpg");
        _history.BeginStep(forwards: false);

        var forward = _history.BeginStep(forwards: true)!.Value;

        Assert.Equal("c.jpg", Path.GetFileName(forward.Target.Path));
        Assert.False(_history.Record(forward.Target));
        Assert.Equal(2, _history.Cursor);
    }

    [Fact]
    public void BeginStep_CursorBeforeTheFirstEntry_ForwardTargetsTheFirstEntry()
    {
        LeaveTheCursorBeforeTheFirstEntry();

        Assert.Equal(-1, _history.Cursor);
        Assert.Equal("b.jpg", TargetName(forwards: true));
        Assert.True(_history.Record(FileEntry("b.jpg")));
        Assert.Equal(0, _history.Cursor);
        Assert.Equal(["b.jpg"], Names());
    }

    [Fact]
    public void Record_CursorBeforeTheFirstEntry_SomethingNew_StartsTheHistoryOver()
    {
        LeaveTheCursorBeforeTheFirstEntry();

        Assert.True(_history.Record(FileEntry("x.jpg")));
        Assert.Equal(["x.jpg"], Names());
        Assert.Equal(0, _history.Cursor);
    }

    [Fact]
    public async Task Record_FromSeveralThreads_WhileAnotherReadsAndSteps_NeverThrows()
    {
        var recording = Enumerable.Range(0, 4).Select(thread => Task.Run(() =>
        {
            for (var i = 0; i < 2000; i++)
            {
                _history.Record(FileEntry($"{thread}-{i}.jpg"));
            }
        })).ToArray();

        while (!recording.All(task => task.IsCompleted))
        {
            _ = _history.Entries;
            _ = _history.Current;
            if (_history.BeginStep(forwards: false) is { } step)
            {
                _history.EndStep(step);
            }
        }

        await Task.WhenAll(recording);
        Assert.Equal(ViewHistory.MaxEntries, _history.Count);
    }

    private static string PathOf(string name) => Path.Combine(Root, name);

    private static ViewHistoryEntry FileEntry(string name, int page = -1) => ViewHistoryEntry.ForFile(PathOf(name), page);

    private static ViewHistoryEntry Clipboard() =>
        ViewHistoryEntry.ForSingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard, "Clipboard image");

    private void RecordAll(params string[] names)
    {
        foreach (var name in names)
        {
            _history.Record(FileEntry(name));
        }
    }

    private string[] Names() =>
        _history.Entries.Select(e => e.Path is null ? "(image)" : Path.GetFileName(e.Path)).ToArray();

    private string TargetName(bool forwards) => Path.GetFileName(_history.BeginStep(forwards)!.Value.Target.Path)!;

    // A step to b.jpg is under way when a Back finds a.jpg gone and removes it, and b.jpg never shows.
    // That leaves the cursor before the first entry, with b.jpg the only one
    private void LeaveTheCursorBeforeTheFirstEntry()
    {
        RecordAll("a.jpg", "b.jpg");
        _history.Record(_history.BeginStep(forwards: false)!.Value.Target);
        var forward = _history.BeginStep(forwards: true)!.Value;
        _missing.Add(PathOf("a.jpg"));
        _history.BeginStep(forwards: false);
        _history.EndStep(forward);
    }
}
