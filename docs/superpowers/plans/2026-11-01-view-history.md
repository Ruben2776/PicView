# Back and Forward Follow What Each Tab Shows: Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give each tab a View History that Back and Forward walk, including Single Images and the pages of TIFFs and archives. File History keeps its newest views last behind a lock, the Last File gets one writer, and renames reach all three.

**Architecture:** A tab records what it shows in the three methods that change it: `Display`, `DisplaySingleImage` and the new `ShowPage`. The record goes into the tab's own `ViewHistory`, into File History and into the Last File. Back and Forward only mark a target, and that same recording moves the cursor when the target shows. `FileRenames` applies a rename to all three before a tab shows the renamed file.

**Tech Stack:** C# on `net11.0` (`LangVersion` preview), Avalonia 12 with Avalonia.Headless, R3, xUnit v3, SharpCompress, Magick.NET.

**Spec:** `docs/superpowers/specs/2026-11-01-view-history-design.md`

The tasks follow the spec's build order, with one change: the `ViewHistory` class (Task 2) comes before the recording seam (Task 3), so the seam is written once. Task 7 also touches `TabOverviewViewModel` and `SourceOpener`, which the spec's file list leaves out, to pass an archive's start entry through.

## Global Constraints

- `PicView.Core` stays Native AOT compatible: no reflection-heavy code in it.
- File-scoped namespaces, braces on every `if` and loop, and `_camelCase` private fields, static ones included, as `FileHistoryManager` does.
- Test names follow `MethodOrScenario_Condition_ExpectedResult`. Tests don't import Xunit, since it's a global using.
- A test class that touches `Settings`, `FileHistoryManager` or `ObservableSystem.DefaultFrameProvider` carries `[Collection("Sequential")]`.
- Comments match the surrounding code: short, and they say why.
- View History holds up to 100 entries (`ViewHistory.MaxEntries`), at most 10 of them Single Images (`ViewHistory.MaxSingleImages`). File History keeps 50 unpinned entries and 5 pinned ones, as today.
- Paths compare with `StringComparison.OrdinalIgnoreCase`, as the file list does.
- Showing comes first.
  - Recording runs after the image is shown. It does no disk access and never waits for the UI thread.
  - It holds a lock only for a few in-memory list operations.
  - No lock is held across disk access or an `await`.
- `ViewHistory.Record`, and `FileHistoryManager.Add` of a path that's already listed, allocate nothing: no LINQ, lambdas or string building on those paths.
- The Actions keep their names, `FileHistoryPrev` and `FileHistoryNext`. FileHistory.json keeps its format.
- Run tests from the repository root with `dotnet test src/PicView.Tests/PicView.Tests.csproj`, adding `--filter`. Never schedule polling or timeout timers around a test run; wait for it to finish.
- Work on a feature branch, not on `dev`.
- Each commit message is one plain sentence in the repository's style, ending with the line `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`.

## Review Focus

These follow from the spec and are the likeliest to bite a user. Each has its test in the task named.

1. **Paths that differ only in case**, as Windows and Explorer produce them: they're the same entry, and renames match them. Tests: Task 2 `Record_SamePathInAnotherCase_IsARepeat`, Task 8 `Rename_IgnoresCase_AndNeverMatchesAPathThatOnlyContainsTheOld`.
2. **Side by side:** a pair is one entry, and showing the same first image with another partner adds nothing. Test: Task 3 `Display_SamePrimaryWithAnotherPartner_AddsNothing`.
3. **File history turned off:** Back, Forward and the Last File keep working, and nothing reaches File History. Tests: Task 3 `Display_WithFileHistoryOff_StillRecordsViewHistory`, Task 4 `Display_WithFileHistoryOff_StillWritesTheLastFile`.
4. **An archive with the same file name in two folders** (`a/1.jpg`, `b/1.jpg`), which extraction flattens: each page maps back to its own entry. Test: Task 7 `ExtractEntriesAsync_SameFileNameInTwoFolders_RemembersEachFilesEntry`.
5. **A tab whose files were all deleted** shows the start-up menu but keeps its View History. Back shows the newest earlier entry that still exists, and Forward skips the deleted one. Test: Task 2 `BeginStep_AfterTheShownFileWasDeleted_StepsToTheNewestEarlierEntryThatExists`.

## Found while planning: TIFF pages turn on jumps

`ImageIterator.IterateToIndexAsync` and `IterateToIndicesAsync` check the tab first. When it shows a multi-page TIFF, they turn that file's page instead of going to the index they were given.

Jumps go through these two methods too:
- `LoadFromFileAsync`'s found index,
- `SkipToIndexAsync`, which gallery clicks, Next 10 and Next 100 use,
- `ReloadAsync`,
- `FolderChanges`.

So Back from a TIFF page to an earlier file in its folder would only turn the page.

Task 6 moves the page turn into `NavigateAsync` and `RepeatNavigateAsync`, so only stepping by one turns pages. The spec doesn't mention this, but its success criteria 1 and 2 need it.

---

### Task 1: File History keeps its newest views last, behind one lock

**Files:**
- Modify: `src/PicView.Core/FileHistory/FileHistoryManager.cs`
- Modify: `src/PicView.Core/ViewModels/FileHistoryViewModel.cs` (`UpdateHistory`)
- Test: `src/PicView.Tests/FileHistory/FileHistoryManagerTests.cs`
- Create: `src/PicView.Tests/FileHistory/HistoryAllocationTests.cs`

**Interfaces:**
- Produces:
  - `FileHistoryManager.AllEntries : Entry[]`, a copy, oldest first.
  - `FileHistoryManager.PinnedEntries : Entry[]`, a copy.
  - `UnPinnedEntries` is removed, since nothing uses it.
  - `FileHistoryManager.Add(string path)`:
    - an unpinned path that's already listed moves to the newest end;
    - a pinned one stays where it is;
    - a listed path allocates nothing.

- [ ] **Step 1: Write the failing tests.** Add these to `FileHistoryManagerTests`:

```csharp
[Fact]
public void Add_PathAlreadyListed_MovesItToTheNewestEnd()
{
    FileHistoryManager.Add("/path/a.jpg");
    FileHistoryManager.Add("/path/b.jpg");
    FileHistoryManager.Add("/path/c.jpg");

    FileHistoryManager.Add("/path/a.jpg");

    Assert.Equal(["/path/b.jpg", "/path/c.jpg", "/path/a.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
}

[Fact]
public void Add_PinnedPathAgain_KeepsItsPlace()
{
    FileHistoryManager.Add("/path/a.jpg");
    FileHistoryManager.Pin("/path/a.jpg");
    FileHistoryManager.Add("/path/b.jpg");

    FileHistoryManager.Add("/path/a.jpg");

    Assert.Equal(["/path/a.jpg", "/path/b.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
}

[Fact]
public void Add_SamePathInAnotherCase_DoesNotAddItAgain()
{
    FileHistoryManager.Add(@"C:\Pics\A.jpg");

    FileHistoryManager.Add(@"c:\pics\a.jpg");

    Assert.Equal(1, FileHistoryManager.Count);
}

[Fact]
public void AllEntries_IsACopy_ThatLaterAddsLeaveAlone()
{
    FileHistoryManager.Add("/path/a.jpg");
    var copy = FileHistoryManager.AllEntries;

    FileHistoryManager.Add("/path/b.jpg");

    Assert.Single(copy);
}

[Fact]
public async Task AllEntries_WhileOtherThreadsAdd_NeverThrows()
{
    var adding = Enumerable.Range(0, 4).Select(thread => Task.Run(() =>
    {
        for (var i = 0; i < 2000; i++)
        {
            FileHistoryManager.Add($"/thread{thread}/{i % 80}.jpg");
        }
    })).ToArray();

    while (!adding.All(task => task.IsCompleted))
    {
        foreach (var entry in FileHistoryManager.AllEntries)
        {
            Assert.NotNull(entry.Path);
        }
        Assert.True(FileHistoryManager.PinnedEntries.Length <= 5);
    }

    await Task.WhenAll(adding);
    Assert.Equal(50, FileHistoryManager.Count);
}
```

Create `HistoryAllocationTests`:

```csharp
using PicView.Core.FileHistory;

namespace PicView.Tests.FileHistory;

/// <summary>
/// A tab records every image it shows, so recording what it showed before must not allocate.
/// </summary>
[Collection("Sequential")]
public class HistoryAllocationTests : IDisposable
{
    private const int Records = 100;
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _configPath =
        Path.Combine(Path.GetTempPath(), "PicView_HistoryAllocation_" + Guid.NewGuid().ToString("N") + ".json");

    public HistoryAllocationTests()
    {
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        FileHistoryManager.Initialize(_configPath);
    }

    public void Dispose()
    {
        File.Delete(_configPath);
        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void FileHistoryAdd_PathsAlreadyListed_AllocatesNothing()
    {
        FileHistoryManager.Add("/path/a.jpg");
        FileHistoryManager.Add("/path/b.jpg");

        Assert.Equal(0, AllocatedBytes(() =>
        {
            FileHistoryManager.Add("/path/a.jpg");
            FileHistoryManager.Add("/path/b.jpg");
        }));
    }

    private static long AllocatedBytes(Action record)
    {
        record();
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < Records; i++)
        {
            record();
        }
        return GC.GetAllocatedBytesForCurrentThread() - before;
    }
}
```

- [ ] **Step 2: Run the tests and watch them fail.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.FileHistory"`
Expected: the build fails on `PinnedEntries.Length` (CS1061), since `PinnedEntries` isn't an array yet.

- [ ] **Step 3: Implement the lock and the newest-end order in `FileHistoryManager`.**
  - Add `private static readonly Lock _lock = new();`. Every member that reads or writes `_entries` takes it.
  - `SaveToFileAsync` copies the entries under the lock and saves outside it.
  - `CurrentIndex`'s clamp reads `_entries` directly, since its callers already hold the lock.
  - `AllEntries` returns a copy as `Entry[]`. `PinnedEntries` returns a copy of the pinned entries as `Entry[]`. Delete `UnPinnedEntries`.
  - `Add` finds the path with a `for` loop and `OrdinalIgnoreCase`:
    - A listed pinned entry stays where it is.
    - A listed unpinned entry is removed, and the same `Entry` object is added at the end.
    - A new path evicts the oldest unpinned entry at the cap, as today, and is appended.
    - In every case `CurrentIndex` points at the entry. `CurrentIndex` stays until Task 5.
    - `Add` uses no LINQ, lambdas or string building.
  - In `FileHistoryViewModel.UpdateHistory`, use `pinnedEntries.Length > 0`, `entries.Length` and `entries[i]`. Drop the `ZLinq` using if nothing else needs it.

- [ ] **Step 4: Run the tests and watch them pass.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.FileHistory"`
Expected: all pass, the existing `FileHistoryManagerTests` included.

- [ ] **Step 5: Commit.**

```bash
git add src/PicView.Core/FileHistory/FileHistoryManager.cs src/PicView.Core/ViewModels/FileHistoryViewModel.cs src/PicView.Tests/FileHistory
git commit -m "Keep File History newest last, behind one lock" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 2: A View History for each tab

**Files:**
- Create: `src/PicView.Core/Navigation/ViewHistoryEntry.cs`
- Create: `src/PicView.Core/Navigation/ViewHistory.cs`
- Create: `src/PicView.Tests/Navigation/ViewHistoryTests.cs`
- Modify: `src/PicView.Tests/FileHistory/HistoryAllocationTests.cs`

**Interfaces:**
- Produces, in `PicView.Core.Navigation`:
  - `public enum ViewHistoryKind { File, ArchivePage, SingleImage }`
  - `public readonly record struct ViewHistoryEntry(ViewHistoryKind Kind, string? Path, int Page = -1, string? ArchiveEntry = null, ImageModel? Model = null, SingleImageType SingleImageType = SingleImageType.None, string? Title = null)`, with:
    - `static ViewHistoryEntry ForFile(string path, int page = -1)`
    - `static ViewHistoryEntry ForArchivePage(string archivePath, string? entry)`
    - `static ViewHistoryEntry ForSingleImage(ImageModel model, SingleImageType type, string? title, string? url = null)`
    - `string? HistoryPath`: the path, but null for a Single Image that isn't a URL download.
    - `string? FilePath`: the path of a file or archive entry, null for a Single Image.
    - `bool Matches(in ViewHistoryEntry other)`
  - `public readonly record struct ViewHistoryStep(ViewHistoryEntry Target, int Id)`
  - `public sealed class ViewHistory(Func<string, bool>? fileExists = null)`, where `fileExists` defaults to `File.Exists`, with:
    - `const int MaxEntries = 100`, `const int MaxSingleImages = 10`
    - `int Count`, `int Cursor` (-1 when empty), `ViewHistoryEntry? Current`, and `ViewHistoryEntry[] Entries` (a copy)
    - `bool Record(in ViewHistoryEntry entry)`, which returns false for a repeat
    - `ViewHistoryStep? BeginStep(bool forwards)`
    - `void EndStep(ViewHistoryStep step)`

- [ ] **Step 1: Write the failing tests.**

```csharp
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
}
```

Add this test to `HistoryAllocationTests`, with `using PicView.Core.Navigation;`:

```csharp
[Fact]
public void ViewHistoryRecord_NewFilesAndRepeats_AllocatesNothing()
{
    var history = new ViewHistory();
    var a = ViewHistoryEntry.ForFile("/path/a.jpg");
    var b = ViewHistoryEntry.ForFile("/path/b.jpg");

    Assert.Equal(0, AllocatedBytes(() =>
    {
        history.Record(a);
        history.Record(a);
        history.Record(b);
    }));
}
```

- [ ] **Step 2: Run the tests and watch them fail.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation.ViewHistoryTests"`
Expected: the build fails, because `ViewHistory` doesn't exist (CS0246).

- [ ] **Step 3: Implement `ViewHistoryEntry`.**
  - The factories set the fields their names say: `ForSingleImage` puts `url` in `Path`.
  - `Matches` needs the same `Kind`, then:
    - for a file, the same path, ignoring case, and the same page;
    - for an archive page, the same archive path, ignoring case, and the same entry (ordinal);
    - for a Single Image, both URL downloads with the same URL (ordinal), or else the same `Model` instance.
  - Inside the struct, the `Path` property hides `System.IO.Path`, so don't call `Path.*` there.

- [ ] **Step 4: Implement `ViewHistory`.**

  State: a `List<ViewHistoryEntry>` created with capacity `MaxEntries + 1`, the cursor, the target (-1 when no step is pending), the pending step's id (0 when none), a counter for step ids, `_fileExists` (holding `fileExists ?? File.Exists`) and `Lock _lock`. Every member takes the lock, and `Entries` copies under it. "Ending the step" (`EndPendingStep()`) sets the target to -1 and the pending id to 0.

  `Record`, under the lock:
  1. If the cursor's entry `Matches`, return false. A pending step stays pending.
  2. If a step is pending, look from the entry next to the cursor towards the target, the target included. The first entry that `Matches` becomes the cursor. Reaching the target ends the step. Return true.
  3. Otherwise:
     - end any step;
     - remove the entries after the cursor;
     - add the entry and put the cursor on it;
     - drop the oldest entry past `MaxEntries`;
     - when the new entry is a Single Image, also drop the oldest Single Image past `MaxSingleImages`.

     Each removal before the cursor moves the cursor down by one. Return true.

  Removing an entry, whether a missing file or a cap:
  - The cursor and the target move down by one when they were after it.
  - Removing the cursor's own entry moves the cursor to the entry before it.
  - A cursor that lands on the target ends the step.

  `BeginStep` checks files outside the lock, so a load that records in the meantime never waits for the disk:

```csharp
public ViewHistoryStep? BeginStep(bool forwards)
{
    while (true)
    {
        int index;
        ViewHistoryEntry candidate;
        lock (_lock)
        {
            var from = _target >= 0 ? _target : _cursor;
            index = forwards ? from + 1 : from - 1;
            if (from < 0 || index < 0 || index >= _entries.Count)
            {
                return null;
            }
            candidate = _entries[index];
        }

        // Outside the lock, so a load that records meanwhile never waits for the disk
        var exists = candidate.FilePath is not { } path || _fileExists(path);

        lock (_lock)
        {
            if (index >= _entries.Count || !_entries[index].Matches(candidate))
            {
                // The list changed meanwhile; look again
                continue;
            }
            if (!exists)
            {
                RemoveAt(index);
                continue;
            }
            if (index == _cursor)
            {
                // The tab already shows it, so no target is left to reach
                EndPendingStep();
                return new ViewHistoryStep(candidate, 0);
            }
            _target = index;
            _targetId = ++_lastStepId;
            return new ViewHistoryStep(candidate, _targetId);
        }
    }
}
```

  `EndStep(step)`: when `step.Id` isn't 0 and is the pending step's id, end the step.

- [ ] **Step 5: Run the tests and watch them pass.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation.ViewHistoryTests|FullyQualifiedName~PicView.Tests.FileHistory.HistoryAllocationTests"`
Expected: all pass.

- [ ] **Step 6: Commit.**

```bash
git add src/PicView.Core/Navigation/ViewHistory.cs src/PicView.Core/Navigation/ViewHistoryEntry.cs src/PicView.Tests/Navigation/ViewHistoryTests.cs src/PicView.Tests/FileHistory/HistoryAllocationTests.cs
git commit -m "Add a View History for each tab to step back and forward through" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 3: Tabs record what they show

**Files:**
- Modify: `src/PicView.Core/ViewModels/TabViewModel.cs`
- Modify: `src/PicView.Core/Navigation/NavigationService.cs`
- Modify: `src/PicView.Core/Navigation/ImageIterator.cs`
- Modify: `src/PicView.Avalonia/StartUp/QuickLoad.cs`
- Modify: `src/PicView.Core/ViewModels/FileHistoryViewModel.cs`
- Create: `src/PicView.Tests/Navigation/ViewHistoryRecordingTests.cs`
- Modify: `src/PicView.Tests/Navigation/NavigationServiceTests.cs`

**Interfaces:**
- Consumes: `ViewHistory` and `ViewHistoryEntry` (Task 2); `FileHistoryManager.Add` (Task 1).
- Produces:
  - `TabViewModel.ViewHistory : ViewHistory`, one per tab, created with it.
  - `TabViewModel.DisplaySingleImage(ImageModel model, SingleImageType type, string? title = null)`. It keeps the title for showing the image again.
  - `Display` and `DisplaySingleImage` record through a private `RecordShown()`. Task 4 adds the Last File to it, Task 6 the TIFF page, and Task 7 the archive entry.

- [ ] **Step 1: Write the failing tests.** Create `ViewHistoryRecordingTests`. Its private `MockThumbnailCache`, `MockThumbnailLoader` and `MockFrameProvider` are copies of the ones in `TiffNavigationTests`.

```csharp
using System.IO.Compression;
using ImageMagick;
using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.ViewModels;
using PicView.Tests.FileHistory;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class ViewHistoryRecordingTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewRecording_" + Guid.NewGuid().ToString("N"));
    private readonly TabViewModel _tab;

    public ViewHistoryRecordingTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        Directory.CreateDirectory(_folder);
        FileHistoryManager.Initialize(Path.Combine(_folder, "history.json"));
        _tab = new TabViewModel(null!);
    }

    public void Dispose()
    {
        _tab.ArchiveExtractionService!.Cleanup();
        Directory.Delete(_folder, true);
        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void Display_File_RecordsItsPathInViewHistoryAndFileHistory()
    {
        var path = PathOf("a.jpg");

        _tab.Display(Model(path));

        var entry = Assert.Single(_tab.ViewHistory.Entries);
        Assert.Equal(ViewHistoryKind.File, entry.Kind);
        Assert.Equal(path, entry.Path);
        Assert.Equal(path, Assert.Single(FileHistoryManager.AllEntries).Path);
    }

    [Fact]
    public void Display_SideBySide_RecordsOnlyTheFirst()
    {
        _tab.Display(Model(PathOf("a.jpg")), Model(PathOf("b.jpg")));

        Assert.Equal(PathOf("a.jpg"), Assert.Single(_tab.ViewHistory.Entries).Path);
        Assert.Single(FileHistoryManager.AllEntries);
    }

    [Fact]
    public void Display_SamePrimaryWithAnotherPartner_AddsNothing()
    {
        _tab.Display(Model(PathOf("a.jpg")), Model(PathOf("b.jpg")));

        _tab.Display(Model(PathOf("a.jpg")), Model(PathOf("c.jpg")));

        Assert.Single(_tab.ViewHistory.Entries);
    }

    [Fact]
    public void Display_SameFileAgain_AddsNothing()
    {
        // A reload, a crop or a file changed on disk shows the same file again
        _tab.Display(Model(PathOf("a.jpg")));

        _tab.Display(Model(PathOf("a.jpg")));

        Assert.Single(_tab.ViewHistory.Entries);
    }

    [Fact]
    public async Task Display_FileInTheArchiveExtractionFolder_RecordsTheArchive()
    {
        var zip = CreateZip("a.jpg", "b.jpg");
        await PrepareArchiveAsync(zip);

        _tab.Display(Model(Path.Combine(_tab.ArchiveExtractionService!.TempZipDirectory!, "a.jpg")));

        var entry = Assert.Single(_tab.ViewHistory.Entries);
        Assert.Equal(ViewHistoryKind.ArchivePage, entry.Kind);
        Assert.Equal(zip, entry.Path);
        Assert.Equal(zip, Assert.Single(FileHistoryManager.AllEntries).Path);
    }

    [Fact]
    public async Task Display_FirstFileAfterLeavingTheArchive_RecordsTheFile()
    {
        // Leaving an archive shows the next file before the extraction is cleaned up
        await PrepareArchiveAsync(CreateZip("a.jpg"));

        _tab.Display(Model(PathOf("outside.jpg")));

        var entry = Assert.Single(_tab.ViewHistory.Entries);
        Assert.Equal(ViewHistoryKind.File, entry.Kind);
        Assert.Equal(PathOf("outside.jpg"), entry.Path);
    }

    [Fact]
    public void DisplaySingleImage_ClipboardImage_RecordsTheImageButNotFileHistory()
    {
        var model = new ImageModel { Image = new object() };

        _tab.DisplaySingleImage(model, SingleImageType.Clipboard, "Clipboard image");

        var entry = Assert.Single(_tab.ViewHistory.Entries);
        Assert.Same(model, entry.Model);
        Assert.Equal("Clipboard image", entry.Title);
        Assert.Empty(FileHistoryManager.AllEntries);
    }

    [Fact]
    public void DisplaySingleImage_UrlDownload_RecordsTheUrlInBoth()
    {
        const string url = "https://example.com/a.png";
        _tab.SourceURL = url;

        _tab.DisplaySingleImage(Model(PathOf("download.png")), SingleImageType.Url, url);

        Assert.Equal(url, Assert.Single(_tab.ViewHistory.Entries).Path);
        Assert.Equal(url, Assert.Single(FileHistoryManager.AllEntries).Path);
    }

    [Fact]
    public void Display_EmptyModel_RecordsNothing()
    {
        // As when a tab becomes fresh again
        _tab.Display(new ImageModel());

        Assert.Equal(0, _tab.ViewHistory.Count);
    }

    [Fact]
    public void Display_WithFileHistoryOff_StillRecordsViewHistory()
    {
        Settings.Navigation.IsFileHistoryEnabled = false;

        _tab.Display(Model(PathOf("a.jpg")));

        Assert.Single(_tab.ViewHistory.Entries);
        Assert.Empty(FileHistoryManager.AllEntries);
    }

    [Fact]
    public async Task Display_InSeveralTabsAtOnce_WhileTheDropdownReads_NeverThrows()
    {
        var tabs = Enumerable.Range(0, 4).Select(_ => new TabViewModel(null!)).ToArray();
        var showing = tabs.Select((tab, t) => Task.Run(() =>
        {
            for (var i = 0; i < 500; i++)
            {
                tab.Display(Model(PathOf($"{t}-{i % 70}.jpg")));
            }
        })).ToArray();

        while (!showing.All(task => task.IsCompleted))
        {
            _ = FileHistoryManager.AllEntries;
            _ = FileHistoryManager.PinnedEntries;
        }

        await Task.WhenAll(showing);
        Assert.All(tabs, tab => Assert.Equal(ViewHistory.MaxEntries, tab.ViewHistory.Count));
    }

    [Fact]
    public async Task RepeatNavigation_RecordsEachImageItShows_AndNoneItPassedBeforeItLoaded()
    {
        var files = new[] { "a.jpg", "b.jpg", "c.jpg", "d.jpg" }.Select(name => new FileInfo(PathOf(name))).ToList();
        var slow = new TaskCompletionSource<ImageModel>();
        var cache = new SharedImageCache(file => file.Name == "b.jpg"
            ? new ValueTask<ImageModel>(slow.Task)
            : ValueTask.FromResult(new ImageModel { FileInfo = file }));
        var iterator = new ImageIterator(cache, new MockThumbnailCache(), new MockThumbnailLoader(), _tab);
        iterator.Initialize(files);
        var cts = new CancellationTokenSource();

        // b is still loading when the held key moves on
        var passed = iterator.RepeatNavigateAsync(NavigateTo.Next, TimeSpan.Zero, cts).AsTask();
        await iterator.RepeatNavigateAsync(NavigateTo.Next, TimeSpan.Zero, cts);
        await iterator.RepeatNavigateAsync(NavigateTo.Next, TimeSpan.Zero, cts);
        slow.SetResult(new ImageModel { FileInfo = files[1] });
        await passed;

        Assert.Equal(["c.jpg", "d.jpg"], _tab.ViewHistory.Entries.Select(e => Path.GetFileName(e.Path)));
    }

    private string PathOf(string name) => Path.Combine(_folder, name);

    private static ImageModel Model(string path) => new() { FileInfo = new FileInfo(path), Image = new object() };

    private string CreateZip(params string[] images)
    {
        var source = Directory.CreateDirectory(Path.Combine(_folder, "zip_" + Guid.NewGuid().ToString("N"))).FullName;
        foreach (var image in images)
        {
            File.WriteAllBytes(Path.Combine(source, image), [0xFF, 0xD8, 0xFF]);
        }
        var zip = Path.Combine(_folder, Guid.NewGuid().ToString("N") + ".zip");
        ZipFile.CreateFromDirectory(source, zip);
        return zip;
    }

    private Task PrepareArchiveAsync(string zip) =>
        _tab.ArchiveExtractionService!.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal);
}
```

Add this test and helper to `NavigationServiceTests`:

```csharp
[Fact]
public async Task LoadFromArchiveAsync_RecordsTheArchiveAndNoneOfItsTempFiles()
{
    Settings.Navigation.IsFileHistoryEnabled = true;
    FileHistoryManager.Initialize(Path.Combine(_testDirectory, "history.json"));
    var zip = await CreateZipAsync("recorded.zip", 3);
    var tab = CreateTab(_testDirectory);

    try
    {
        await _navigationService.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource());

        Assert.Equal([zip], FileHistoryManager.AllEntries.Select(e => e.Path));
    }
    finally
    {
        tab.ArchiveExtractionService.Cleanup();
    }
}

private async Task<string> CreateZipAsync(string name, int images)
{
    var source = Directory.CreateDirectory(
        Path.Combine(_testDirectory, Path.GetFileNameWithoutExtension(name) + "_source")).FullName;
    for (var i = 1; i <= images; i++)
    {
        await File.WriteAllBytesAsync(Path.Combine(source, $"img_{i:D2}.jpg"), [0xFF, 0xD8, 0xFF],
            TestContext.Current.CancellationToken);
    }
    var zip = Path.Combine(_testDirectory, name);
    System.IO.Compression.ZipFile.CreateFromDirectory(source, zip);
    return zip;
}
```

- [ ] **Step 2: Run the tests and watch them fail.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation.ViewHistoryRecordingTests|FullyQualifiedName~PicView.Tests.Navigation.NavigationServiceTests"`
Expected: the build fails, because `TabViewModel.ViewHistory` doesn't exist (CS1061).

- [ ] **Step 3: Implement the recording in `TabViewModel`.**
  - Add `public ViewHistory ViewHistory { get; } = new();`.
  - `DisplaySingleImage(ImageModel model, SingleImageType type, string? title = null)` keeps `title` in a private field before it shows the model.
  - `Display` and `DisplaySingleImage` both call `RecordShown()` after `ShowModels`.
  - `RecordShown()` builds the entry from the spec's table:
    - a Single Image: `ForSingleImage(Model, SingleImageType, title, SingleImageType is Url ? SourceURL : null)`;
    - no `Model.FileInfo`: nothing;
    - a file inside `ArchiveExtractionService.TempZipDirectory`: `ForArchivePage(LastOpenedArchive, entry: null)`. Task 7 fills in the entry.
    - anything else: `ForFile(path)`. Task 6 adds the page.
  - When `ViewHistory.Record` returns true and the entry has a `HistoryPath`, it calls `FileHistoryManager.Add(historyPath)`.
  - "Inside the folder" compares spans, so recording a file allocates nothing:

```csharp
private static bool IsInFolder(string path, string folder)
{
    var trimmed = Path.TrimEndingDirectorySeparator(folder.AsSpan());
    return path.Length > trimmed.Length + 1
           && path.AsSpan().StartsWith(trimmed, StringComparison.OrdinalIgnoreCase)
           && path[trimmed.Length] is '\\' or '/';
}
```

- [ ] **Step 4: Remove the scattered `Add` calls and show URL downloads as Single Images.**
  - `NavigationService`: remove its five `FileHistoryManager.Add` calls: the one at the end of `RepopulateIterator`, the three in `LoadFromArchiveAsync` and the one in `LoadFromUrlAsync`.
  - In `LoadFromUrlAsync`, set `tab.SourceURL = url` and show the image with `tab.DisplaySingleImage(model, SingleImageType.Url, url)`. That replaces `tab.Display(model)` and the later `SingleImageType` assignment.
  - `ImageIterator`: remove the three `FileHistoryManager.Add` calls in `UpdateModels` and `UpdateModel`, the `IsArchived` checks around them, and the `PicView.Core.FileHistory` using.
  - `QuickLoad.LoadFileAsync`: remove `FileHistoryManager.Add(fileInfo.FullName)` and its using.
  - `FileHistoryViewModel.UpdateHistory`: the current item is the active tab's `ViewHistory.Current?.HistoryPath`, compared ignoring case.

- [ ] **Step 5: Run the tests and watch them pass.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation|FullyQualifiedName~PicView.Tests.FileHistory"`
Expected: all pass. Then run the whole suite, `dotnet test src/PicView.Tests/PicView.Tests.csproj`, and expect `Failed: 0`.

- [ ] **Step 6: Commit.**

```bash
git add src/PicView.Core src/PicView.Avalonia/StartUp/QuickLoad.cs src/PicView.Tests/Navigation
git commit -m "Record what a tab shows where it shows it" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 4: The Last File has one writer

**Files:**
- Create: `src/PicView.Core/Navigation/LastFile.cs`
- Modify: `src/PicView.Core/ViewModels/TabViewModel.cs` (`RecordShown`)
- Modify: `src/PicView.Avalonia/CustomControls/MainWindow.cs` (`OnDeactivated`)
- Modify: `src/PicView.Avalonia/StartUp/MainWindowLifecycle.cs` (`RememberFile`, and a comment in `CloseAsync`)
- Modify: `src/PicView.Avalonia/StartUp/AppLifecycle.cs` (a comment in `ShutdownAsync`)
- Modify: `src/PicView.Avalonia/Navigation/UINavigationHelper.cs` (`OpenLastFile`)
- Modify: `src/PicView.Core/Navigation/NavigationService.cs` and `src/PicView.Core/Navigation/Interfaces/INavigationService.cs`
- Delete: `src/PicView.Core/Navigation/NavigationMode.cs`
- Create: `src/PicView.Tests/Navigation/LastFileTests.cs`

**Interfaces:**
- Consumes: `TabViewModel.ViewHistory` and `RecordShown` (Task 3).
- Produces: `public static class LastFile` in `PicView.Core.Navigation`, with:
  - `static void Record(TabViewModel tab, string path)`
  - `static void Remember(TabViewModel tab)`

  Task 8 adds `Rename` and `RenameFolder`.

- [ ] **Step 1: Write the failing tests.** The private `MockFrameProvider` is the same as in Task 3.

```csharp
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class LastFileTests
{
    private readonly CoreViewModel _core;

    public LastFileTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.StartUp.LastFile = null;
        _core = new CoreViewModel(null, file => ValueTask.FromResult(new ImageModel { FileInfo = file }));
    }

    [Fact]
    public void Display_InTheActiveTabOfTheActiveMainWindow_WritesTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;

        tab.Display(Model("a.jpg"));

        Assert.Equal(PathOf("a.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void Display_InATabThatIsNotActive_LeavesTheLastFile()
    {
        var window = OpenWindow();
        var first = window.WindowTabs.ActiveTab.Value;
        var second = window.WindowTabs.CreateTab();
        window.WindowTabs.SelectTab(first);

        second.Display(Model("b.jpg"));

        Assert.Null(Settings.StartUp.LastFile);
    }

    [Fact]
    public void Display_InAMainWindowThatIsNotActive_LeavesTheLastFile()
    {
        var background = OpenWindow();
        OpenWindow();

        background.WindowTabs.ActiveTab.Value.Display(Model("c.jpg"));

        Assert.Null(Settings.StartUp.LastFile);
    }

    [Fact]
    public void DisplaySingleImage_ClipboardImage_LeavesTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg"));

        tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard, "Clipboard image");

        Assert.Equal(PathOf("a.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void DisplaySingleImage_UrlDownload_WritesTheUrl()
    {
        const string url = "https://example.com/a.png";
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.SourceURL = url;

        tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Url, url);

        Assert.Equal(url, Settings.StartUp.LastFile);
    }

    [Fact]
    public void Display_WithFileHistoryOff_StillWritesTheLastFile()
    {
        Settings.Navigation.IsFileHistoryEnabled = false;
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;

        tab.Display(Model("a.jpg"));

        Assert.Equal(PathOf("a.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void Remember_TabOfAMainWindowThatIsNotActive_WritesWhatItShows()
    {
        var background = OpenWindow();
        var tab = background.WindowTabs.ActiveTab.Value;
        OpenWindow();
        tab.Display(Model("d.jpg"));

        // As closing its Main Window does
        LastFile.Remember(tab);

        Assert.Equal(PathOf("d.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void Remember_TabShowingAClipboardImage_KeepsTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model("a.jpg"));
        tab.DisplaySingleImage(new ImageModel { Image = new object() }, SingleImageType.Clipboard, "Clipboard image");

        LastFile.Remember(tab);

        Assert.Equal(PathOf("a.jpg"), Settings.StartUp.LastFile);
    }

    private MainWindowViewModel OpenWindow()
    {
        var window = new MainWindowViewModel(_core, null!, null!, null!, null!);
        _core.MainWindows.MainWindows.Add(window);
        _core.MainWindows.ActiveWindow.Value = window;
        return window;
    }

    private static string PathOf(string name) => Path.Combine(Path.GetTempPath(), "PicViewLastFile", name);

    private static ImageModel Model(string name) => new() { FileInfo = new FileInfo(PathOf(name)), Image = new object() };
}
```

- [ ] **Step 2: Run the tests and watch them fail.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation.LastFileTests"`
Expected: the build fails, because `LastFile` doesn't exist (CS0103).

- [ ] **Step 3: Implement `LastFile` and route every writer and reader through it.**
  - `LastFile.Record(tab, path)` sets `Settings.StartUp.LastFile = path` only when all of these hold:
    - `tab.ParentWindowContext` is a Main Window,
    - that window's `WindowTabs.ActiveTab.CurrentValue` is the tab,
    - and the window is its `Core.MainWindows.ActiveWindow.CurrentValue`.

    A tab without a Main Window, or a Main Window without a core (as in tests), never writes.
  - `LastFile.Remember(tab)` writes the tab's `ViewHistory.Current?.HistoryPath` when it has one.
  - `TabViewModel.RecordShown` calls `LastFile.Record(this, historyPath)` right after `FileHistoryManager.Add`.
  - `MainWindowLifecycle.RememberFile(window)` becomes `LastFile.Remember(vm.WindowTabs.ActiveTab.CurrentValue)`. Drop its `PicView.Core.FileHistory` using.
  - `MainWindow.OnDeactivated` no longer writes `Settings.StartUp.LastFile`. Drop its `PicView.Core.FileHistory` using.
  - The comments before `RememberFile` in `MainWindowLifecycle.CloseAsync` and `AppLifecycle.ShutdownAsync` lose their "since deactivating a window remembers the file history's current entry instead". The calls stay where they are.
  - `UINavigationHelper.OpenLastFile` opens `Settings.StartUp.LastFile` and nothing else.
  - Delete `NavigationService.LoadLastFileAsync`, its `INavigationService` member, and then `NavigationService`'s `PicView.Core.FileHistory` using. Delete `NavigationMode.cs`.

- [ ] **Step 4: Run the tests and watch them pass.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation.LastFileTests|FullyQualifiedName~PicView.Tests.StartUp.AppLifecycleTests"`
Expected: all pass, including `ShutdownAsync_ClosesEveryMainWindowAndRemembersTheMostRecentlyActiveOne` and `RestartAsync_RunsAppShutdownBeforeStartingANewPicView`.

- [ ] **Step 5: Commit.**

```bash
git add -A src/PicView.Core src/PicView.Avalonia src/PicView.Tests/Navigation/LastFileTests.cs
git commit -m "Write the Last File in one place" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 5: Back and Forward step through the tab's View History

**Files:**
- Modify: `src/PicView.Avalonia/Navigation/UINavigationHelper.cs`
- Modify: `src/PicView.Avalonia/Navigation/UpdateImage.cs`
- Modify: `src/PicView.Core/FileHistory/FileHistoryManager.cs`
- Modify: `src/PicView.Tests/FileHistory/FileHistoryManagerTests.cs`
- Modify: `src/PicView.Tests/StartUp/AppLifecycleTests.cs`

**Interfaces:**
- Consumes:
  - `ViewHistory.BeginStep` and `EndStep` (Task 2);
  - `TabViewModel.ViewHistory` and `DisplaySingleImage(model, type, title)` (Task 3);
  - `Settings.StartUp.LastFile`, which only `LastFile` writes (Task 4).
- Produces:
  - `UpdateImage.SetSingleImage(MainWindowViewModel vm, MainWindow mainWindow, ImageModel model, SingleImageType type, string title)`.
  - `UINavigationHelper.OpenPreviousFileHistoryEntry` and `OpenNextFileHistoryEntry` step the active tab's View History. `FunctionsMapper` keeps calling them.
  - A private `ShowAgainAsync(MainWindow mainWindow, MainWindowViewModel vm, TabViewModel tab, ViewHistoryEntry entry)`, which Tasks 6 and 7 extend.

- [ ] **Step 1: Write the failing end-to-end test** in `AppLifecycleTests`. Add the usings `Avalonia.Media.Imaging`, `Avalonia.Platform` and `PicView.Core.Keybindings`.

```csharp
[AvaloniaFact]
public async Task FileHistoryPrev_AfterAClipboardImage_ShowsTheFile_AndFileHistoryNextTheSameImageAgain()
{
    var app = CreateApp();
    await app.StartAsync(settingsExists: true, []);
    var window = app.MainWindows.Open[0];
    var vm = ViewModelOf(window);
    var tab = vm.WindowTabs.ActiveTab.CurrentValue;
    var file = Path.Combine(_folder, "viewed.png");
    await File.WriteAllBytesAsync(file, [0x89, 0x50, 0x4E, 0x47], TestContext.Current.CancellationToken);
    Application.Current!.DataContext = _core;
    try
    {
        await SourceOpener.OpenAsync(window, _core, file);
        var bitmap = new WriteableBitmap(new PixelSize(4, 4), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);
        UpdateImage.SetSingleImage(vm, window, bitmap, SingleImageType.Clipboard, "Clipboard image");
        var pasted = tab.Model;

        await vm.ActionRunner!.Run(Actions.FileHistoryPrev);
        var shownAfterBack = tab.Model.FileInfo?.FullName;
        await vm.ActionRunner.Run(Actions.FileHistoryNext);

        Assert.Equal(file, shownAfterBack);
        Assert.Same(pasted, tab.Model);
        Assert.Equal(SingleImageType.Clipboard, tab.SingleImageType);
        Assert.Equal(2, tab.ViewHistory.Count);
    }
    finally
    {
        Application.Current.DataContext = null;
    }
}
```

In `FileHistoryManagerTests`, delete `Add_AndNavigatePreviousAndNext_MovesCursorAndDeduplicates`; the newest-end tests from Task 1 cover what it checked. Replace `Rename_AndRemove_UpdatesEntriesAndClampsCursor` with:

```csharp
[Fact]
public void Rename_AndRemove_UpdateTheEntries()
{
    FileHistoryManager.Add("/path/old.jpg");
    FileHistoryManager.Add("/path/second.jpg");

    FileHistoryManager.Rename("/path/old.jpg", "/path/new.jpg");
    var removed = FileHistoryManager.Remove("/path/second.jpg");

    Assert.True(removed);
    Assert.Equal(["/path/new.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
}
```

- [ ] **Step 2: Run the end-to-end test and watch it fail.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.StartUp.AppLifecycleTests.FileHistoryPrev_AfterAClipboardImage_ShowsTheFile_AndFileHistoryNextTheSameImageAgain"`
Expected: `Assert.Same` fails. Forward falls back to the last file, so the tab shows `viewed.png` again, not the pasted image.

- [ ] **Step 3: Implement the steps.**
  - `UpdateImage.SetSingleImage(vm, mainWindow, ImageModel model, SingleImageType type, string title)` runs today's body with the given model. Its size comes from `model.PixelWidth` and `PixelHeight`, and it calls `DisplaySingleImage(model, type, title)`. The `Bitmap` overload builds today's model and calls this one.
  - In `UINavigationHelper`, `OpenPreviousFileHistoryEntry` and `OpenNextFileHistoryEntry` call `StepAsync(mainWindow, vm, forwards: false)` and `StepAsync(mainWindow, vm, forwards: true)`.
  - `StepAsync`:
    - takes the active tab;
    - calls `BeginStep` on a pool thread, `await Task.Run(() => tab.ViewHistory.BeginStep(forwards))`, because it checks files on disk;
    - returns when there's no step;
    - otherwise shows the target with `ShowAgainAsync` inside a `try` whose `finally` calls `EndStep`, and logs a failure with `DebugHelper.LogDebug`.
  - `ShowAgainAsync`:
    - A Single Image: for a URL download, set `tab.SourceURL = entry.Path` first. Then call `UpdateImage.SetSingleImage(vm, mainWindow, entry.Model!, entry.SingleImageType, entry.Title ?? string.Empty)`.
    - A file or an archive page: `OpenPathAsync(mainWindow, vm, entry.Path)`. Until Task 7, an archive page opens its archive at the first page.
  - Drop `UINavigationHelper`'s `PicView.Core.FileHistory` using.
  - `FileHistoryManager`: delete `CurrentIndex`, `HasPrevious`, `HasNext`, `CurrentEntry`, `GetNextEntry`, `GetPreviousEntry`, `GetEntry`, `GetFirstEntry` and `GetLastEntry`, along with the cursor bookkeeping in `Initialize`, `Add`, `Clear` and `Remove`.

- [ ] **Step 4: Run the tests and watch them pass.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.StartUp.AppLifecycleTests|FullyQualifiedName~PicView.Tests.FileHistory"`
Expected: all pass. Then run the whole suite and expect `Failed: 0`.

- [ ] **Step 5: Commit.**

```bash
git add src/PicView.Avalonia/Navigation src/PicView.Core/FileHistory src/PicView.Tests
git commit -m "Step Back and Forward through the tab's View History" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 6: TIFF pages are entries, and only stepping by one turns them

**Files:**
- Modify: `src/PicView.Core/ViewModels/TabViewModel.cs`
- Modify: `src/PicView.Core/Navigation/ImageIterator.cs`
- Modify: `src/PicView.Avalonia/Navigation/UINavigationHelper.cs` (`ShowAgainAsync`)
- Modify: `src/PicView.Tests/Navigation/TiffNavigationTests.cs`
- Modify: `src/PicView.Tests/Navigation/ViewHistoryRecordingTests.cs`

**Interfaces:**
- Consumes: `RecordShown` (Task 3) and `ShowAgainAsync` (Task 5).
- Produces:
  - `TabViewModel.ShowPage(int page)`
  - `TabViewModel.RequestPage(string filePath, int page)`
  - File entries of multi-page TIFFs carry their `Page`.

- [ ] **Step 1: Write the failing tests.** Add these to `ViewHistoryRecordingTests`, with `using PicView.Core.Navigation.Tiff;`:

```csharp
[Fact]
public void Display_MultiPageTiff_RecordsItsPage_AndEachPageShowPageTurnsTo()
{
    _tab.Display(TiffModel(PathOf("t.tif"), pageCount: 3));

    _tab.ShowPage(1);
    _tab.ShowPage(2);

    Assert.Equal([0, 1, 2], _tab.ViewHistory.Entries.Select(e => e.Page));
    Assert.Single(FileHistoryManager.AllEntries);
}

[Fact]
public void Display_SinglePageTiff_RecordsNoPage()
{
    _tab.Display(TiffModel(PathOf("t.tif"), pageCount: 1));

    Assert.Equal(-1, Assert.Single(_tab.ViewHistory.Entries).Page);
}

private static ImageModel TiffModel(string path, int pageCount) => new()
{
    FileInfo = new FileInfo(path),
    Image = new object(),
    TiffNavigation = new TiffNavigationInfo
    {
        PageCount = pageCount,
        Pages = Enumerable.Range(0, pageCount).Select(_ => new object()).ToArray()
    }
};
```

Add these to `TiffNavigationTests`:

```csharp
[Fact]
public async Task RequestPage_ThenNavigatingToTheTiff_ShowsThatPageFirst()
{
    var tiffModel = CreateTiffModel(_files[1], 3);
    SetModel(1, tiffModel);
    _tab.Model = new ImageModel();
    _tab.RequestPage(_files[1].FullName, 2);

    await _iterator.NavigateAsync(NavigateTo.Next, SkipAmount.One, new CancellationTokenSource());

    Assert.Equal(2, tiffModel.TiffNavigation!.CurrentPage);
    Assert.Equal(2, Assert.Single(_tab.ViewHistory.Entries).Page);
}

[Fact]
public async Task IterateToIndexAsync_FromATiffPage_GoesToTheFileInsteadOfTurningThePage()
{
    var tiffModel = CreateTiffModel(_files[1], 3);
    _iterator.SetCurrentIndex(1);
    _tab.Model = tiffModel;
    var target = new ImageModel { FileInfo = _files[0] };
    SetModel(0, target);

    await _iterator.IterateToIndexAsync(0, new CancellationTokenSource());

    Assert.Equal(0, _iterator.CurrentIndex);
    Assert.Same(target, _tab.Model);
    Assert.Equal(0, tiffModel.TiffNavigation!.CurrentPage);
}
```

- [ ] **Step 2: Run the tests and watch them fail.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation.TiffNavigationTests|FullyQualifiedName~PicView.Tests.Navigation.ViewHistoryRecordingTests"`
Expected: the build fails, because `ShowPage` and `RequestPage` don't exist (CS1061).

- [ ] **Step 3: Implement pages in `TabViewModel`.**
  - `ShowPage(int page)`:
    - does nothing when `Model.TiffNavigation` is null or the page is outside `0..PageCount - 1`;
    - otherwise sets `CurrentPage`, sets `Model.Image` to `Pages[page]` when `Pages` has that page, and records.

    The window still picks the page up from `Model.Image`.
  - `RequestPage(string filePath, int page)` keeps one request in a private `sealed record PageRequest(string FilePath, int Page)` field, swapped with `Interlocked.Exchange`.
  - `Display` takes the request and clears it either way. When the primary model is that file (ignoring case), has `TiffNavigation`, and the page is in range, `Display` sets that page (`CurrentPage` and `Image`) before showing.
  - In `RecordShown`, a file's page is `TiffNavigation.CurrentPage` when `PageCount > 1`, and -1 otherwise.

- [ ] **Step 4: Turn TIFF pages only when stepping by one, in `ImageIterator`.**
  - Replace the static `ShouldNavigateTiffEntry` with an instance method that turns the page through `_tab.ShowPage`. It follows today's rule: the previous page when `IsReversed`, else the next. It returns false when there's no such page.
  - Call it in `NavigateAsync`, in both the single and the side-by-side branch, only for `NavigateTo.Next` and `NavigateTo.Previous` with `SkipAmount.One`. Call it after `IsReversed` is set and before iterating.
  - Call it the same way in both branches of `RepeatNavigateAsync`.
  - Remove the TIFF checks from `IterateToIndexAsync` and `IterateToIndicesAsync`, and the TODO with them. `UpdateImageFromPage` stays, for `UpdateModel`.

- [ ] **Step 5: Show a TIFF page again in `UINavigationHelper.ShowAgainAsync`.** For a file with `Page >= 0`:
  - if the tab shows that file (ignoring case) and its model has `TiffNavigation`, call `tab.ShowPage(entry.Page)`;
  - otherwise call `tab.RequestPage(entry.Path!, entry.Page)`, then `OpenPathAsync`.

- [ ] **Step 6: Run the tests and watch them pass.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation"`
Expected: all pass, the existing `TiffNavigationTests` included.

- [ ] **Step 7: Commit.**

```bash
git add src/PicView.Core src/PicView.Avalonia/Navigation/UINavigationHelper.cs src/PicView.Tests/Navigation
git commit -m "Record TIFF pages, and turn pages only when stepping by one" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 7: Archive pages are entries

**Files:**
- Modify: `src/PicView.Core/ArchiveHandling/ArchiveExtractionService.cs`
- Modify: `src/PicView.Core/Navigation/NavigationService.cs` and `src/PicView.Core/Navigation/Interfaces/INavigationService.cs`
- Modify: `src/PicView.Core/ViewModels/TabOverviewViewModel.cs` (`LoadFromStringAsync`)
- Modify: `src/PicView.Avalonia/Navigation/SourceOpener.cs` (`OpenOptions`, `OpenAsync`)
- Modify: `src/PicView.Core/ViewModels/TabViewModel.cs` (`RecordShown`)
- Modify: `src/PicView.Avalonia/Navigation/UINavigationHelper.cs`
- Modify: `src/PicView.Tests/ArchiveHandling/ArchiveExtractionServiceTests.cs`
- Modify: `src/PicView.Tests/Navigation/NavigationServiceTests.cs`
- Modify: `src/PicView.Tests/Navigation/ViewHistoryRecordingTests.cs`

**Interfaces:**
- Consumes: `RecordShown` (Task 3) and `ShowAgainAsync` (Task 5).
- Produces:
  - `ArchiveExtractionService.EntryOf(string extractedPath) : string?`
  - `ArchiveExtractionService.ExtractedPathOf(string entry) : string?`
  - `INavigationService.LoadFromArchiveAsync(string archivePath, TabViewModel tab, CancellationTokenSource ct, string? startEntry = null)`
  - `INavigationService.LoadFromStringAsync(string source, TabViewModel tab, CancellationTokenSource ct, string? rootDirectory = null, string? archiveEntry = null)`
  - `TabOverviewViewModel.LoadFromStringAsync(string source, TabViewModel? senderTab = null, string? rootDirectory = null, string? archiveEntry = null)`
  - `OpenOptions(bool IsStartup = false, bool ContinueFromLeftOff = false, string? ArchiveEntry = null)`
  - `UINavigationHelper.OpenPathAsync(MainWindow mainWindow, MainWindowViewModel vm, string? path, string? archiveEntry = null)`

- [ ] **Step 1: Write the failing tests.** Add these to `ArchiveExtractionServiceTests`, with `using System.IO.Compression;`:

```csharp
[Fact]
public async Task ExtractEntriesAsync_SameFileNameInTwoFolders_RemembersEachFilesEntry()
{
    var folder = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N"));
    var source = Path.Combine(folder, "source");
    Directory.CreateDirectory(Path.Combine(source, "a"));
    Directory.CreateDirectory(Path.Combine(source, "b"));
    await File.WriteAllBytesAsync(Path.Combine(source, "a", "1.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
    await File.WriteAllBytesAsync(Path.Combine(source, "b", "1.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
    var zip = Path.Combine(folder, "nested.zip");
    ZipFile.CreateFromDirectory(source, zip);
    var service = new ArchiveExtractionService();

    try
    {
        var keys = (await service.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal))!
            .Value.EntryKeys;
        var files = await service.ExtractEntriesAsync(zip, keys, TestContext.Current.CancellationToken);

        Assert.Equal(2, files.Count);
        Assert.NotEqual(files[0], files[1]);
        Assert.Equal(keys[0], service.EntryOf(files[0]));
        Assert.Equal(keys[1], service.EntryOf(files[1]));
        Assert.Equal(files[1], service.ExtractedPathOf(keys[1]));
    }
    finally
    {
        service.Cleanup();
        Directory.Delete(folder, true);
    }
}

[Fact]
public async Task Cleanup_ForgetsWhichEntryEachFileCameFrom()
{
    var folder = Path.Combine(Path.GetTempPath(), "PicViewEntries_" + Guid.NewGuid().ToString("N"));
    var source = Directory.CreateDirectory(Path.Combine(folder, "source")).FullName;
    await File.WriteAllBytesAsync(Path.Combine(source, "1.jpg"), [0xFF, 0xD8, 0xFF], TestContext.Current.CancellationToken);
    var zip = Path.Combine(folder, "flat.zip");
    ZipFile.CreateFromDirectory(source, zip);
    var service = new ArchiveExtractionService();
    await service.PrepareArchiveAsync(zip, (_, _) => Task.FromResult(false), string.CompareOrdinal);
    var file = await service.ExtractEntryAsync(zip, "1.jpg", TestContext.Current.CancellationToken);

    service.Cleanup();

    Assert.Null(service.EntryOf(file!));
    Assert.Null(service.ExtractedPathOf("1.jpg"));
    Directory.Delete(folder, true);
}
```

Add this to `NavigationServiceTests`. It uses `CreateZipAsync` from Task 3.

```csharp
[Theory]
[InlineData(false)]
[InlineData(true)]
public async Task LoadFromArchiveAsync_StartEntry_ShowsThatPageFirst(bool alwaysUncompressEntireArchive)
{
    Settings.Navigation.AlwaysUncompressEntireArchive = alwaysUncompressEntireArchive;
    var zip = await CreateZipAsync($"start_{alwaysUncompressEntireArchive}.zip", 5);
    var tab = CreateTab(_testDirectory);

    try
    {
        var loaded = await _navigationService.LoadFromArchiveAsync(zip, tab, new CancellationTokenSource(),
            startEntry: "img_03.jpg");

        Assert.True(loaded);
        Assert.Equal("img_03.jpg", tab.Model.FileInfo!.Name);
        var entry = Assert.Single(tab.ViewHistory.Entries);
        Assert.Equal(ViewHistoryKind.ArchivePage, entry.Kind);
        Assert.Equal("img_03.jpg", entry.ArchiveEntry);
    }
    finally
    {
        tab.ArchiveExtractionService.Cleanup();
        Settings.Navigation.AlwaysUncompressEntireArchive = false;
    }
}
```

In `ViewHistoryRecordingTests`, replace `Display_FileInTheArchiveExtractionFolder_RecordsTheArchive` with:

```csharp
[Fact]
public async Task Display_ExtractedArchivePage_RecordsTheArchiveAndItsEntry()
{
    var zip = CreateZip("a.jpg", "b.jpg");
    await PrepareArchiveAsync(zip);
    var page = await _tab.ArchiveExtractionService!.ExtractEntryAsync(zip, "b.jpg");

    _tab.Display(Model(page!));

    var entry = Assert.Single(_tab.ViewHistory.Entries);
    Assert.Equal(ViewHistoryKind.ArchivePage, entry.Kind);
    Assert.Equal(zip, entry.Path);
    Assert.Equal("b.jpg", entry.ArchiveEntry);
}
```

- [ ] **Step 2: Run the tests and watch them fail.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.ArchiveHandling|FullyQualifiedName~PicView.Tests.Navigation.NavigationServiceTests|FullyQualifiedName~PicView.Tests.Navigation.ViewHistoryRecordingTests"`
Expected: the build fails, because `EntryOf` doesn't exist (CS1061).

- [ ] **Step 3: Implement the entry map in `ArchiveExtractionService`.**
  - Two `ConcurrentDictionary<string, string>` maps: file to entry with `OrdinalIgnoreCase`, and entry to file with `Ordinal`.
  - They're filled for each file `WriteEntryFlat` writes, in `ExtractEntriesAsync` and `ExtractRemaining`.
  - They're cleared at the start of `PrepareArchiveAsync` and in `Cleanup()`.
  - `EntryOf(path)` returns the map's entry. For an archive that local software extracts whole (`.7z`, `.cb7`), it returns the path relative to `TempZipDirectory`. For a file outside the folder it returns null.
  - `ExtractedPathOf(entry)` returns the map's file. For an archive extracted whole, it returns `Path.Combine(TempZipDirectory, entry)` when that file exists.

- [ ] **Step 4: Open an archive at an entry, in `NavigationService.LoadFromArchiveAsync`.**
  - **Extracted whole:** start at the file whose `EntryOf` is `startEntry`, or else at the first.
  - **Staged** (`AlwaysUncompressEntireArchive` off):
    - the start index is `startEntry`'s index in `EntryKeys` (ordinal), or 0;
    - extract that entry first, plus its side-by-side partner (`SideBySidePair.PartnerOf(start, keys.Length)`) when one is shown;
    - seed the iterator with them;
    - then extract every other key, in order, in the background.
  - **Batches** (`AlwaysUncompressEntireArchive` on): the first batch is the ten keys starting at the start index. The rest follow in order in the background.
  - Everything else stays as today.

- [ ] **Step 5: Pass the start entry through.**
  - `NavigationService.LoadFromStringAsync` passes `archiveEntry` on for archives.
  - `TabOverviewViewModel.LoadFromStringAsync` passes it on.
  - `SourceOpener.OpenAsync` passes `options.ArchiveEntry` to it.

- [ ] **Step 6: Record and show archive pages.**
  - `TabViewModel.RecordShown` records an archive page's entry as `ArchiveExtractionService.EntryOf(path)`.
  - `UINavigationHelper.OpenPathAsync` takes `archiveEntry` and passes `new OpenOptions(ContinueFromLeftOff: isFreshTab, ArchiveEntry: archiveEntry)`.
  - `ShowAgainAsync` for an archive page:
    - when the tab's `LastOpenedArchive` is the entry's archive (ignoring case), and `ExtractedPathOf(entry.ArchiveEntry)` is one of the iterator's files, call `tab.ImageIterator.SkipToIndexAsync(index, tab.GetTabCancellation())`;
    - otherwise call `OpenPathAsync(mainWindow, vm, entry.Path, entry.ArchiveEntry)`.

- [ ] **Step 7: Run the tests and watch them pass.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.ArchiveHandling|FullyQualifiedName~PicView.Tests.Navigation"`
Expected: all pass. Then run the whole suite and expect `Failed: 0`.

- [ ] **Step 8: Commit.**

```bash
git add src/PicView.Core src/PicView.Avalonia/Navigation src/PicView.Tests
git commit -m "Record archive pages and open an archive again at one" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 8: Every history follows a rename

**Files:**
- Create: `src/PicView.Core/Navigation/FileRenames.cs`
- Modify: `src/PicView.Core/Navigation/ViewHistory.cs`
- Modify: `src/PicView.Core/FileHistory/FileHistoryManager.cs`
- Modify: `src/PicView.Core/Navigation/LastFile.cs`
- Create: `src/PicView.Tests/Navigation/FileRenamesTests.cs`
- Modify: `src/PicView.Tests/Navigation/ViewHistoryTests.cs`
- Modify: `src/PicView.Tests/FileHistory/FileHistoryManagerTests.cs`

**Interfaces:**
- Consumes: `ViewHistory` (Task 2), `LastFile` (Task 4), and `FolderChange.PathInRenamedFolder` (existing).
- Produces:
  - `ViewHistory.Rename(string oldPath, string newPath)` and `ViewHistory.RenameFolder(string oldFolder, string newFolder)`
  - `FileHistoryManager.Rename(string oldPath, string newPath)` (rewritten) and `FileHistoryManager.RenameFolder(string oldFolder, string newFolder)`
  - `LastFile.Rename(string oldPath, string newPath)` and `LastFile.RenameFolder(string oldFolder, string newFolder)`
  - `public static class FileRenames` in `PicView.Core.Navigation`, with `Follow(CoreViewModel? core, string oldPath, string newPath)` and `FollowFolder(CoreViewModel? core, string oldFolder, string newFolder)`

- [ ] **Step 1: Write the failing tests.** Add these to `ViewHistoryTests`:

```csharp
[Fact]
public void Rename_File_RenamesEveryEntryForIt()
{
    _history.Record(FileEntry("t.tif", page: 0));
    _history.Record(FileEntry("t.tif", page: 1));
    _history.Record(FileEntry("b.jpg"));

    _history.Rename(PathOf("t.tif"), PathOf("u.tif"));

    Assert.Equal(["u.tif", "u.tif", "b.jpg"], Names());
    Assert.Equal(2, _history.Cursor);
}

[Fact]
public void Rename_Archive_RenamesItsPages()
{
    _history.Record(ViewHistoryEntry.ForArchivePage(PathOf("a.zip"), "1.jpg"));
    _history.Record(ViewHistoryEntry.ForArchivePage(PathOf("a.zip"), "2.jpg"));

    _history.Rename(PathOf("a.zip"), PathOf("b.zip"));

    Assert.All(_history.Entries, e => Assert.Equal(PathOf("b.zip"), e.Path));
}

[Fact]
public void Rename_NeighborsBecomeTheSame_MergesThem_AndKeepsTheForwardEntries()
{
    RecordAll("a.jpg", "b.jpg", "c.jpg", "d.jpg");
    _history.Record(_history.BeginStep(forwards: false)!.Value.Target);

    _history.Rename(PathOf("b.jpg"), PathOf("c.jpg"));

    Assert.Equal(["a.jpg", "c.jpg", "d.jpg"], Names());
    Assert.Equal(1, _history.Cursor);
}

[Fact]
public void Rename_IgnoresCase_AndNeverMatchesAPathThatOnlyContainsTheOld()
{
    _history.Record(ViewHistoryEntry.ForFile(PathOf("A.jpg")));
    _history.Record(ViewHistoryEntry.ForFile(PathOf("a.jpg.bak")));

    _history.Rename(PathOf("a.jpg"), PathOf("z.jpg"));

    Assert.Equal(["z.jpg", "a.jpg.bak"], Names());
}

[Fact]
public void RenameFolder_MovesEveryEntryUnderIt()
{
    _history.Record(ViewHistoryEntry.ForFile(Path.Combine(Root, "x", "a.jpg")));
    _history.Record(ViewHistoryEntry.ForFile(Path.Combine(Root, "x", "sub", "b.jpg")));
    _history.Record(ViewHistoryEntry.ForFile(Path.Combine(Root, "xy", "c.jpg")));

    _history.RenameFolder(Path.Combine(Root, "x"), Path.Combine(Root, "y"));

    Assert.Equal(
        [Path.Combine(Root, "y", "a.jpg"), Path.Combine(Root, "y", "sub", "b.jpg"), Path.Combine(Root, "xy", "c.jpg")],
        _history.Entries.Select(e => e.Path));
}
```

Add these to `FileHistoryManagerTests`:

```csharp
[Fact]
public void Rename_KeepsTheEntrysPlaceAndPin()
{
    FileHistoryManager.Add("/path/a.jpg");
    FileHistoryManager.Pin("/path/a.jpg");
    FileHistoryManager.Add("/path/b.jpg");

    FileHistoryManager.Rename("/path/a.jpg", "/path/z.jpg");

    Assert.Equal(["/path/z.jpg", "/path/b.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    Assert.Equal("/path/z.jpg", Assert.Single(FileHistoryManager.PinnedEntries).Path);
}

[Fact]
public void Rename_OntoAListedPath_MergesThemInTheNewerPlace_PinnedIfEitherWas()
{
    FileHistoryManager.Add("/path/a.jpg");
    FileHistoryManager.Pin("/path/a.jpg");
    FileHistoryManager.Add("/path/b.jpg");
    FileHistoryManager.Add("/path/c.jpg");

    FileHistoryManager.Rename("/path/a.jpg", "/path/c.jpg");

    Assert.Equal(["/path/b.jpg", "/path/c.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
    Assert.True(FileHistoryManager.AllEntries[1].IsPinned);
}

[Fact]
public void Rename_IgnoresCase_AndNeverMatchesAPathThatOnlyContainsTheOld()
{
    FileHistoryManager.Add("/Path/A.jpg");
    FileHistoryManager.Add("/path/a.jpg.bak");

    FileHistoryManager.Rename("/path/a.jpg", "/path/z.jpg");

    Assert.Equal(["/path/z.jpg", "/path/a.jpg.bak"], FileHistoryManager.AllEntries.Select(e => e.Path));
}

[Fact]
public void Rename_SameRenameTwice_ChangesNothingTheSecondTime()
{
    FileHistoryManager.Add("/path/a.jpg");
    FileHistoryManager.Add("/path/b.jpg");
    FileHistoryManager.Rename("/path/a.jpg", "/path/z.jpg");

    FileHistoryManager.Rename("/path/a.jpg", "/path/z.jpg");

    Assert.Equal(["/path/z.jpg", "/path/b.jpg"], FileHistoryManager.AllEntries.Select(e => e.Path));
}

[Fact]
public void RenameFolder_MovesEveryEntryUnderTheFolder()
{
    var x = Path.Combine(_tempRoot, "x");
    FileHistoryManager.Add(Path.Combine(x, "a.jpg"));
    FileHistoryManager.Add(Path.Combine(x, "sub", "b.jpg"));
    FileHistoryManager.Add(Path.Combine(_tempRoot, "xy", "c.jpg"));

    FileHistoryManager.RenameFolder(x, Path.Combine(_tempRoot, "y"));

    Assert.Equal(
        [Path.Combine(_tempRoot, "y", "a.jpg"), Path.Combine(_tempRoot, "y", "sub", "b.jpg"), Path.Combine(_tempRoot, "xy", "c.jpg")],
        FileHistoryManager.AllEntries.Select(e => e.Path));
}
```

Create `FileRenamesTests`. Its private `MockFrameProvider` is the same as in Task 3.

```csharp
using PicView.Core.FileHistory;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.ViewModels;
using PicView.Tests.FileHistory;
using R3;

namespace PicView.Tests.Navigation;

[Collection("Sequential")]
public class FileRenamesTests : IDisposable
{
    private readonly FileHistoryFileBackup _fileHistoryBackup = new();
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "PicViewRenames_" + Guid.NewGuid().ToString("N"));
    private readonly CoreViewModel _core;

    public FileRenamesTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        Settings.Navigation.IsFileHistoryEnabled = true;
        Settings.StartUp.LastFile = null;
        Directory.CreateDirectory(_folder);
        FileHistoryManager.Initialize(Path.Combine(_folder, "history.json"));
        _core = new CoreViewModel(null, file => ValueTask.FromResult(new ImageModel { FileInfo = file }));
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
        _fileHistoryBackup.Dispose();
    }

    [Fact]
    public void Follow_ReachesFileHistoryEveryTabInEveryMainWindowAndTheLastFile()
    {
        var first = OpenWindow().WindowTabs.ActiveTab.Value;
        var second = OpenWindow().WindowTabs.ActiveTab.Value;
        first.Display(Model(PathOf("a.jpg")));
        second.Display(Model(PathOf("a.jpg")));

        FileRenames.Follow(_core, PathOf("a.jpg"), PathOf("b.jpg"));

        Assert.Equal(PathOf("b.jpg"), Assert.Single(first.ViewHistory.Entries).Path);
        Assert.Equal(PathOf("b.jpg"), Assert.Single(second.ViewHistory.Entries).Path);
        Assert.Equal(PathOf("b.jpg"), Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(PathOf("b.jpg"), Settings.StartUp.LastFile);
    }

    [Fact]
    public void FollowFolder_ReachesFileHistoryTheTabsAndTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model(Path.Combine(_folder, "x", "a.jpg")));

        FileRenames.FollowFolder(_core, Path.Combine(_folder, "x"), Path.Combine(_folder, "y"));

        var moved = Path.Combine(_folder, "y", "a.jpg");
        Assert.Equal(moved, Assert.Single(tab.ViewHistory.Entries).Path);
        Assert.Equal(moved, Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(moved, Settings.StartUp.LastFile);
    }

    [Fact]
    public void Follow_WithoutACore_StillRenamesFileHistoryAndTheLastFile()
    {
        var tab = OpenWindow().WindowTabs.ActiveTab.Value;
        tab.Display(Model(PathOf("a.jpg")));

        FileRenames.Follow(null, PathOf("a.jpg"), PathOf("b.jpg"));

        Assert.Equal(PathOf("b.jpg"), Assert.Single(FileHistoryManager.AllEntries).Path);
        Assert.Equal(PathOf("b.jpg"), Settings.StartUp.LastFile);
        Assert.Equal(PathOf("a.jpg"), Assert.Single(tab.ViewHistory.Entries).Path);
    }

    private MainWindowViewModel OpenWindow()
    {
        var window = new MainWindowViewModel(_core, null!, null!, null!, null!);
        _core.MainWindows.MainWindows.Add(window);
        _core.MainWindows.ActiveWindow.Value = window;
        return window;
    }

    private string PathOf(string name) => Path.Combine(_folder, name);

    private static ImageModel Model(string path) => new() { FileInfo = new FileInfo(path), Image = new object() };
}
```

- [ ] **Step 2: Run the tests and watch them fail.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation.ViewHistoryTests|FullyQualifiedName~PicView.Tests.Navigation.FileRenamesTests|FullyQualifiedName~PicView.Tests.FileHistory"`
Expected: the build fails, because `ViewHistory.Rename` doesn't exist (CS1061).

- [ ] **Step 3: Implement renames in each history.**
  - `ViewHistory.Rename`, under the lock:
    - every entry whose `FilePath` is the old path (ignoring case) gets the new path, through `entry with { Path = newPath }`;
    - then neighbors that `Matches` each other merge: the later of the two is removed, and the cursor and target move down when they were at it or after it, so a cursor on either one lands on the merged entry.
  - `ViewHistory.RenameFolder` follows the same rules, mapping each `FilePath` through `FolderChange.PathInRenamedFolder`.
  - `FileHistoryManager.Rename` finds the old path exactly, ignoring case, with a `for` loop:
    - if the new path is listed under another entry, keep the entry nearer the newest end, under the new path and pinned if either was, and drop the other;
    - otherwise rename in place.

    Delete `GetEntryByString`, and the TODO above `Rename`. `RenameFolder` applies the same rules to every entry under the folder.
  - `LastFile.Rename` rewrites `Settings.StartUp.LastFile` when it's the old path, ignoring case. `LastFile.RenameFolder` rewrites it when it's under the old folder.
  - `FileRenames.Follow` renames in File History, then the Last File, then every tab of every Main Window.
    - It copies `core.MainWindows.MainWindows`, and each window's `WindowTabs.Tabs.Value`, with `ToArray()`, because the folder watcher calls it from its own thread.
    - A null core reaches only File History and the Last File.
  - `FileRenames.FollowFolder` does the same with the folder methods.

- [ ] **Step 4: Run the tests and watch them pass.**

Run: the same filter as Step 2.
Expected: all pass.

- [ ] **Step 5: Commit.**

```bash
git add src/PicView.Core src/PicView.Tests
git commit -m "Rename entries in every history at once" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 9: A rename reaches the history before the tab shows the renamed file

**Files:**
- Modify: `src/PicView.Core/Navigation/FileWatcherService.cs`
- Modify: `src/PicView.Avalonia/StartUp/TabNavigationInitializer.cs` (`InitializeNavigation`)
- Modify: `src/PicView.Core/Navigation/FolderChanges.cs` (`RenamedAsync`)
- Modify: `src/PicView.Avalonia/UI/RenameHelper.cs`
- Modify: `src/PicView.Core/ViewModels/ResizeImageViewModel.cs` (`ReplaceOriginalAsync`)
- Modify: `src/PicView.Tests/Navigation/FileWatcherServiceTests.cs`
- Modify: `src/PicView.Tests/FileTests/RenameTests.cs`

**Interfaces:**
- Consumes: `FileRenames.Follow` and `FollowFolder` (Task 8).
- Produces: `FileWatcherService(Func<string, string, int> stringComparer, SharedImageCache cache, IThumbnailCache? thumbnailCache = null, IFolderEvents? folderEvents = null, Action<string, string>? fileRenamed = null, Action<string, string>? folderRenamed = null)`

- [ ] **Step 1: Write the failing tests.**

In `FileWatcherServiceTests`, add the fields `private Action<string, string>? _fileRenamed;` and `private Action<string, string>? _folderRenamed;`. Then construct `_service` with:

```csharp
_service = new FileWatcherService((s1, s2) => string.CompareOrdinal(s1, s2), _cache, _mockThumbnailCache,
    _folderEvents,
    fileRenamed: (oldPath, newPath) => _fileRenamed?.Invoke(oldPath, newPath),
    folderRenamed: (oldFolder, newFolder) => _folderRenamed?.Invoke(oldFolder, newFolder));
```

Add these tests:

```csharp
[Fact]
public async Task OnFileRenamed_RenamesTheHistoryBeforeTheTabsFollow()
{
    var files = Files("old.jpg");
    var tab = CreateTab(_testDirectory, files, files[0]);
    tab.Display(new ImageModel { FileInfo = files[0], Image = new object() });
    string? shownWhenRenamed = null;
    _fileRenamed = (oldPath, newPath) =>
    {
        shownWhenRenamed = tab.Model.FileInfo?.FullName;
        tab.ViewHistory.Rename(oldPath, newPath);
    };
    var newPath = Path.Combine(_testDirectory, "new.jpg");

    Watch(tab).RaiseRenamed("old.jpg", "new.jpg");

    await WaitUntilAsync(() => tab.Model.FileInfo?.FullName == newPath);
    Assert.Equal(files[0].FullName, shownWhenRenamed);
    Assert.Equal(newPath, Assert.Single(tab.ViewHistory.Entries).Path);
}

[Fact]
public async Task OnSubfolderRenamed_RenamesTheHistoryBeforeTheTabsFollow()
{
    Settings.Sorting.IncludeSubDirectories = true;
    var files = Files(Path.Combine("sub", "b.jpg"));
    var tab = CreateTab(_testDirectory, files, files[0]);
    string? shownWhenRenamed = null;
    _folderRenamed = (_, _) => shownWhenRenamed = tab.Model.FileInfo?.FullName;
    var renamed = Path.Combine(_testDirectory, "renamed");

    Watch(tab).RaiseRenamed("sub", "renamed");

    await WaitUntilAsync(() => tab.Model.FileInfo?.DirectoryName == renamed);
    Assert.Equal(files[0].FullName, shownWhenRenamed);
}

[Fact]
public async Task OnFolderRenamed_RenamesTheHistoryBeforeTheTabsFollow()
{
    var files = Files("a.jpg");
    var tab = CreateTab(_testDirectory, files, files[0]);
    string? shownWhenRenamed = null;
    (string Old, string New)? renamedFolder = null;
    _folderRenamed = (oldFolder, newFolder) =>
    {
        shownWhenRenamed = tab.Model.FileInfo?.FullName;
        renamedFolder = (oldFolder, newFolder);
    };
    var moved = _testDirectory + "_renamed";

    Watch(tab).RaiseMoved(moved);

    await WaitUntilAsync(() => tab.Model.FileInfo?.DirectoryName == moved);
    Assert.Equal((_testDirectory, moved), renamedFolder);
    Assert.Equal(files[0].FullName, shownWhenRenamed);
}
```

Add these to `RenameTests`, with `using PicView.Core.Models;`:

```csharp
[Fact]
public async Task RenameAction_SameExtension_KeepsTheTabOnItsEntryUnderTheNewName()
{
    var (vm, tab) = CreateTabInCore(WriteImage("a.png", MagickColors.Red));

    await RenameHelper.RenameAction(vm, "b.png");

    Assert.Equal(Path.Combine(_testDirectory, "b.png"), Assert.Single(tab.ViewHistory.Entries).Path);
}

[Fact]
public async Task RenameAction_NewExtension_KeepsTheTabOnItsEntryUnderTheNewName()
{
    var (vm, tab) = CreateTabInCore(WriteImage("a.png", MagickColors.Red));

    await RenameHelper.RenameAction(vm, "b.jpg");

    Assert.Equal(Path.Combine(_testDirectory, "b.jpg"), Assert.Single(tab.ViewHistory.Entries).Path);
}

private (MainWindowViewModel Vm, TabViewModel Tab) CreateTabInCore(string path)
{
    var core = new CoreViewModel(null, file => ValueTask.FromResult(new ImageModel { FileInfo = file }));
    var vm = new MainWindowViewModel(core, new TranslationViewModel(), null!, new GlobalSettingsViewModel(),
        new GallerySharedSettingsViewModel()) { ToolTip = new ToolTipViewModel() };
    core.MainWindows.MainWindows.Add(vm);
    var tab = vm.WindowTabs.ActiveTab.Value;
    tab.Display(new ImageModel { FileInfo = new FileInfo(path), Image = new object() });
    return (vm, tab);
}
```

- [ ] **Step 2: Run the tests and watch them fail.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj --filter "FullyQualifiedName~PicView.Tests.Navigation.FileWatcherServiceTests|FullyQualifiedName~PicView.Tests.FileTests.RenameTests"`
Expected: the build fails, because `FileWatcherService` has no `fileRenamed` parameter (CS1739).

- [ ] **Step 3: Apply renames before the tabs follow.**
  - `FileWatcherService` takes the two optional callbacks.
    - `HandleAsync` calls `fileRenamed(oldFullPath, fullPath)` for a renamed file, and `folderRenamed(oldFullPath, fullPath)` for a renamed subfolder, before it hands the change to the tabs. This replaces the `FileHistoryManager.Rename` after them. Drop the `PicView.Core.FileHistory` using.
    - `FolderRenamedAsync` calls `folderRenamed(directory, newDirectory)` outside the watcher's lock, before the tabs follow.
  - `TabNavigationInitializer.InitializeNavigation` passes `fileRenamed: (oldPath, newPath) => FileRenames.Follow(core, oldPath, newPath)` and `folderRenamed: (oldFolder, newFolder) => FileRenames.FollowFolder(core, oldFolder, newFolder)`.
  - `RenameHelper.RenameFileAsync`:
    - With the same extension, call `FileRenames.Follow(vm.Core, oldPath, newPath)` right after `FileHelper.RenameFile` succeeds, before the tab's `FileInfo` changes.
    - With a new extension, call it after `magick.WriteAsync(newPath)` and before `LoadFromFileAsync(newPath, tab)`.
    - Remove the `FileHistoryManager.Rename` after `Task.Run`, and the `PicView.Core.FileHistory` using.
  - `ResizeImageViewModel.ReplaceOriginalAsync` calls `FileRenames.Follow(_mainVm?.Core, originalPath, convertedPath)` before `LoadFromFileAsync(convertedPath, tab)`, and no longer calls `FileHistoryManager.Rename`. Drop the using if nothing else needs it.
  - In `FolderChanges.RenamedAsync`, the model shown under the new name also takes `TiffNavigation = currentModel.TiffNavigation`, so a renamed TIFF still matches its page's entry.

- [ ] **Step 4: Run the tests and watch them pass.**

Run: the same filter as Step 2.
Expected: all pass. Then run the whole suite and expect `Failed: 0`.

- [ ] **Step 5: Commit.**

```bash
git add src/PicView.Core src/PicView.Avalonia src/PicView.Tests
git commit -m "Rename the history before a tab shows the renamed file" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

---

### Task 10: Glossary and ADRs, then the final checks

**Files:**
- Modify: `GLOSSARY.md` at the repository root, not `src/GLOSSARY.md`
- Modify: `docs/adr/0001-separate-favorites-and-file-history.md`
- Create: `docs/adr/0012-view-history-follows-what-each-tab-shows.md`

- [ ] **Step 1: Add the three glossary entries.** Copy them exactly from the spec's Glossary section: **View History**, **File History** and **Last File**. They go in `GLOSSARY.md` under `## Tabs`, after **Single Image**.
- [ ] **Step 2: Correct ADR-0001.** Replace "whereas **File History** is a chronological stream of recently opened files, archives, and web URLs with forward/backward traversal and entry pinning." with "whereas **File History** lists the files, archives and web URLs viewed most recently, with entry pinning; Back and Forward walk each tab's View History instead (ADR-0012)."
- [ ] **Step 3: Write ADR-0012** in the house style of ADR-0009: a title, the decision in a few short paragraphs, then `## Considered Options` and `## Consequences`.
  - **Decision:**
    - Each tab keeps a View History. It's recorded in the only three methods that change what a tab shows (`Display`, `DisplaySingleImage` and `ShowPage`), which also feed File History and the Last File.
    - A step only marks a target, and the cursor moves when the tab shows it. So the history follows what's on screen even when loads fail or overtake each other.
    - A rename reaches every history before a tab shows the renamed file.
  - **Considered Options:** recording where each load starts, and watching each tab's bindings from outside, each with the reasons from the spec's Approaches section.
  - **Consequences:**
    - A new way of showing an image has to go through one of the three methods.
    - `FileHistoryManager.Add` has one caller.
    - Back and Forward are per tab, and they no longer open the last file at either end.
    - The file history setting only governs File History.
    - Only stepping by one turns a TIFF's page.
- [ ] **Step 4: Run the whole suite.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj`
Expected: `Failed: 0`.

- [ ] **Step 5: Run the allocation tests in Release.**

Run: `dotnet test src/PicView.Tests/PicView.Tests.csproj -c Release --filter "FullyQualifiedName~PicView.Tests.Input.InputAllocationTests|FullyQualifiedName~PicView.Tests.FileHistory.HistoryAllocationTests"`
Expected: `Failed: 0`, with nothing skipped.

- [ ] **Step 6: Do the spec's by-hand check** from its Testing section, in a Debug build. If no one can drive the app, list the check for the user instead.

- [ ] **Step 7: Commit.**

```bash
git add GLOSSARY.md docs/adr
git commit -m "Record ADR-0012: View History follows what each tab shows" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```
