using ImageMagick;
using PicView.Core.Extensions;
using PicView.Core.ImageDecoding;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.Navigation;
using PicView.Core.Navigation.Interfaces;
using PicView.Core.Titles;
using PicView.Core.ViewModels;
using PicView.Tests.Navigation;
using R3;

namespace PicView.Tests.Titles;

/// <summary>
/// A tab derives its Titles from what it shows (ADR-0015): built when read, marked stale by the tab's own Shown, its
/// Title Phase, its zoom level and its file list, and written by nothing else.
/// </summary>
[Collection("Sequential")]
public class TabTitlesTests
{
    private readonly TabViewModel _tab;

    public TabTitlesTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        SetDefaults();
        TranslationManager.LoadLanguage("en");
        _tab = new TabViewModel(null!,
            TestTabServices.For(new SharedImageCache(f => ValueTask.FromResult(Model(f.Name))!), new ThumbnailCache(),
                new MockThumbnailLoader()));
    }

    private static string Loading => TranslationManager.Translation.Loading!;
    private static string NoImage => TranslationManager.Translation.NoImage!;
    private static string CropMessage => TranslationManager.Translation.CropMessage!;

    private static ImageModel Model(string name) => new()
    {
        FileInfo = new FileInfo(Path.Combine(Path.GetTempPath(), "PicViewTabTitlesTests", name)),
        Image = new object(),
        PixelWidth = 1,
        PixelHeight = 1
    };

    /// <summary>Gives the tab an iterator browsing <paramref name="names"/>, at the first.</summary>
    private FileInfo[] Browse(params string[] names)
    {
        var files = names.Select(n => Model(n).FileInfo!).ToArray();
        _tab.Initialize();
        _tab.ImageIterator!.Initialize(files, 0);
        return files;
    }

    private WindowTitles Expected(FileInfo file, int index, int count) =>
        ImageTitleFormatter.GenerateTitleStrings(new ImageTitleInfo(file, 1, 1, index, count), _tab.Zoom.Level.CurrentValue);

    [Fact]
    public void FreshTab_AllFour_AreNoImage()
    {
        Assert.Equal(NoImage, _tab.Titles.Title);
        Assert.Equal(NoImage, _tab.Titles.TitleTooltip);
        Assert.Equal(NoImage, _tab.Titles.TabTitle);
        Assert.Equal(StringExtensions.CombineWithAppName(NoImage), _tab.Titles.WindowTitle);
    }

    [Fact]
    public void Display_FileWithIterator_GivesIndexedTitle()
    {
        var files = Browse("a.jpg", "b.jpg");

        _tab.Display(Model("a.jpg"));

        var expected = Expected(files[0], 0, 2);
        Assert.Equal(expected.BaseTitle, _tab.Titles.Title);
        Assert.Equal(expected.TitleWithAppName, _tab.Titles.WindowTitle);
        Assert.Equal("a.jpg", _tab.Titles.TabTitle);
    }

    [Fact]
    public void DisplaySingleImage_WithTitle_TabTitleIsTheTitle()
    {
        var single = new ImageModel { Image = new object(), PixelWidth = 1, PixelHeight = 1 };

        _tab.DisplaySingleImage(single, SingleImageType.Base64, "Pasted");

        Assert.Equal("Pasted", _tab.Titles.TabTitle);
        Assert.StartsWith("Pasted", _tab.Titles.Title, StringComparison.Ordinal);
    }

    [Fact]
    public void SetLoading_AllFourAreLoading_UntilDisplay()
    {
        Browse("a.jpg");

        _tab.SetLoading();

        Assert.Equal(Loading, _tab.Titles.Title);
        Assert.Equal(Loading, _tab.Titles.TitleTooltip);
        Assert.Equal(Loading, _tab.Titles.TabTitle);
        Assert.Equal(StringExtensions.CombineWithAppName(Loading), _tab.Titles.WindowTitle);

        _tab.Display(Model("a.jpg"));

        Assert.Equal(TitlePhase.None, _tab.Titles.Phase);
        Assert.Equal("a.jpg", _tab.Titles.TabTitle);
    }

    [Fact]
    public void SetDownloading_TextReplacesAllFour_PerCall()
    {
        _tab.SetDownloading("x 10%");
        Assert.Equal("x 10%", _tab.Titles.Title);
        Assert.Equal(StringExtensions.CombineWithAppName("x 10%"), _tab.Titles.WindowTitle);

        _tab.SetDownloading("x 50%");
        Assert.Equal("x 50%", _tab.Titles.TabTitle);
        Assert.Equal("x 50%", _tab.Titles.TitleTooltip);
    }

    [Fact]
    public void SetError_HoldsThroughEndLoading_EndsOnSetLoadingAndOnDisplay()
    {
        // Ended with the Error's own ticket, the latest one, so only the phase being an Error keeps it
        var errorTicket = _tab.Titles.Enter(TitlePhase.Error, "e");
        _tab.EndLoading(errorTicket);
        Assert.Equal("e", _tab.Titles.Title);
        Assert.Equal(TitlePhase.Error, _tab.Titles.Phase);

        _tab.SetLoading();
        Assert.Equal(TitlePhase.Loading, _tab.Titles.Phase);

        Browse("a.jpg");
        _tab.SetError("e");
        _tab.Display(Model("a.jpg"));
        Assert.Equal(TitlePhase.None, _tab.Titles.Phase);
        Assert.Equal("a.jpg", _tab.Titles.TabTitle);
    }

    [Fact]
    public void EndLoading_FromLoading_RestoresDerivedTitles()
    {
        Browse("a.jpg");
        _tab.Display(Model("a.jpg"));
        var ticket = _tab.SetLoading();

        _tab.EndLoading(ticket);

        Assert.Equal(TitlePhase.None, _tab.Titles.Phase);
        Assert.Equal("a.jpg", _tab.Titles.TabTitle);
    }

    [Fact]
    public void EndLoading_WithAnOlderTicket_LeavesTheNewerLoadingInPlace()
    {
        var older = _tab.SetLoading();
        var newer = _tab.SetLoading();

        _tab.EndLoading(older);
        Assert.Equal(TitlePhase.Loading, _tab.Titles.Phase);

        _tab.EndLoading(newer);
        Assert.Equal(TitlePhase.None, _tab.Titles.Phase);
    }

    [Fact]
    public void Display_EndsLoading_WhateverTheTicket()
    {
        Browse("a.jpg");
        _tab.SetLoading();

        _tab.Display(Model("a.jpg"));

        Assert.Equal(TitlePhase.None, _tab.Titles.Phase);
    }

    [Fact]
    public void Display_FileWithoutIterator_ShowsLoading_UntilIteratorInitializes()
    {
        _tab.Display(Model("a.jpg"));

        Assert.Equal(Loading, _tab.Titles.Title);
        Assert.Equal(TitlePhase.None, _tab.Titles.Phase);

        Browse("a.jpg");

        Assert.Equal("a.jpg", _tab.Titles.TabTitle);
    }

    [Fact]
    public void Crop_Set_TitleIsCropMessage_TabTitleKeepsName()
    {
        Browse("a.jpg");
        _tab.Display(Model("a.jpg"));

        _tab.Crop = new CropViewModel(_tab.Zoom);

        Assert.Equal(CropMessage, _tab.Titles.Title);
        Assert.Equal(CropMessage, _tab.Titles.TitleTooltip);
        Assert.Equal("a.jpg", _tab.Titles.TabTitle);

        _tab.Crop = null;

        Assert.NotEqual(CropMessage, _tab.Titles.Title);
    }

    [Fact]
    public void Crop_RenameWhileCropping_KeepsCropTitleAndRenamesTab()
    {
        Browse("a.jpg");
        var model = Model("a.jpg");
        _tab.Display(model);
        _tab.Crop = new CropViewModel(_tab.Zoom);

        _tab.UpdateShownFile(model.FileInfo!.FullName, Model("b.jpg").FileInfo!);

        Assert.Equal(CropMessage, _tab.Titles.Title);
        Assert.Equal("b.jpg", _tab.Titles.TabTitle);
    }

    [Fact]
    public void ZoomLevel_Change_TitleFollows()
    {
        var files = Browse("a.jpg");
        _tab.Display(Model("a.jpg"));

        _tab.Zoom.SetFactor(2.5);

        Assert.Equal(Expected(files[0], 0, 1).BaseTitle, _tab.Titles.Title);
        Assert.Contains("250", _tab.Titles.Title, StringComparison.Ordinal);
    }

    [Fact]
    public void MaxIndex_Change_CountFollows()
    {
        var files = Browse("a.jpg", "c.jpg");
        _tab.Display(Model("a.jpg"));

        _tab.ImageIterator!.Insert(Model("b.jpg").FileInfo!, string.CompareOrdinal);

        Assert.Equal(Expected(files[0], 0, 3).BaseTitle, _tab.Titles.Title);
    }

    [Fact]
    public void Display_TwiceWithSameZoom_TitlesTheSecondFile()
    {
        Browse("a.jpg", "b.jpg");
        _tab.Display(Model("a.jpg"));
        _ = _tab.Titles.Title;

        _tab.Display(Model("b.jpg"));

        Assert.Equal("b.jpg", _tab.Titles.TabTitle);
    }

    [Fact]
    public void FullPathSetting_TurnedOn_TitleIsFilePathTitle()
    {
        var files = Browse("a.jpg");
        _tab.Display(Model("a.jpg"));
        _ = _tab.Titles.Title;

        _tab.Services!.GlobalSettings.IsShowingFullPathInTitleBar.Value = true;

        Assert.Equal(Expected(files[0], 0, 1).FilePathTitle, _tab.Titles.Title);
    }

    [Fact]
    public void SideBySideSetting_Changed_MarksTheTitles()
    {
        _ = _tab.Titles.Title;
        Assert.Equal(1, _tab.Titles.BuildCount);

        _tab.Services!.GlobalSettings.IsSideBySide.Value = true;
        _ = _tab.Titles.Title;

        Assert.Equal(2, _tab.Titles.BuildCount);
    }

    [Fact]
    public void Titles_AreBuiltOnRead_OnceAcrossMarks()
    {
        Browse("a.jpg");
        _tab.Display(Model("a.jpg"));
        _tab.Zoom.SetFactor(1.5);

        Assert.Equal(0, _tab.Titles.BuildCount);

        _ = _tab.Titles.Title;
        _ = _tab.Titles.TitleTooltip;
        _ = _tab.Titles.WindowTitle;
        _ = _tab.Titles.TabTitle;
        Assert.Equal(1, _tab.Titles.BuildCount);

        _ = _tab.Titles.Title;
        Assert.Equal(1, _tab.Titles.BuildCount);
    }

    [Fact]
    public void Mark_OnDisplay_ComesAfterTheImageBindable()
    {
        object? imageWhenMarked = null;
        _tab.Titles.PropertyChanged += (_, _) => imageWhenMarked = _tab.Image.Value;
        var model = Model("a.jpg");

        _tab.Display(model);

        Assert.Same(model.Image, imageWhenMarked);
    }

    [Fact]
    public async Task Display_FromPoolThread_TitleReadsOnTestThread()
    {
        Browse("a.jpg");

        await Task.Run(() => _tab.Display(Model("a.jpg")), TestContext.Current.CancellationToken);

        Assert.Equal("a.jpg", _tab.Titles.TabTitle);
    }

    [Fact]
    public void Initialize_SameIndexAndCountAfterRecreate_GivesIndexedTitle()
    {
        // A clipboard paste disposes the iterator; opening the same file again recreates one at the same index and
        // count, so the index and count bindables never change and cannot mark the titles themselves
        var files = Browse("a.jpg");
        _tab.Display(Model("a.jpg"));
        _tab.DisposeImageIterator();
        _tab.DisplaySingleImage(new ImageModel { Image = new object(), PixelWidth = 1, PixelHeight = 1 },
            SingleImageType.Clipboard, "Pasted");
        _tab.Initialize();
        _tab.Display(Model("a.jpg"));
        Assert.Equal(Loading, _tab.Titles.Title);

        _tab.ImageIterator!.Initialize(files, 0);

        Assert.Equal("a.jpg", _tab.Titles.TabTitle);
    }

    [Fact]
    public void Display_AfterDispose_DoesNotThrow()
    {
        // The titles exist and were read before the tab is disposed, so the marks below reach a disposed module
        _ = _tab.Titles.Title;
        _tab.Dispose();

        _tab.Display(Model("a.jpg"));
        var ticket = _tab.SetLoading();
        _tab.EndLoading(ticket);
        _tab.Crop = new CropViewModel(_tab.Zoom);

        _ = _tab.Titles.Title;
    }

    [Fact]
    public void SaysLoading_NoPhase_SaysFalse()
    {
        bool? last = null;
        using var watching = _tab.Titles.SaysLoading.Subscribe(v => last = v);

        Assert.False(last);
    }

    [Fact]
    public void SaysLoading_LoadingThenEnded_SaysTrueThenFalse()
    {
        var seen = new List<bool>();
        using var watching = _tab.Titles.SaysLoading.Subscribe(seen.Add);

        var ticket = _tab.SetLoading();
        _tab.EndLoading(ticket);

        Assert.Equal([false, true, false], seen);
    }

    [Fact]
    public void SaysLoading_Downloading_SaysTrue()
    {
        bool? last = null;
        using var watching = _tab.Titles.SaysLoading.Subscribe(v => last = v);

        _tab.SetDownloading("50%");

        Assert.True(last);
    }

    [Fact]
    public void SaysLoading_Error_SaysFalse()
    {
        bool? last = null;
        using var watching = _tab.Titles.SaysLoading.Subscribe(v => last = v);

        _tab.SetLoading();
        _tab.SetError("failed");

        Assert.False(last);
    }

    private sealed class MockThumbnailLoader : IThumbnailLoader
    {
        public ValueTask<object?> GetThumbnailAsync(FileInfo file, CoreViewModel core) => ValueTask.FromResult<object?>(null);

        public ValueTask<object?> GetThumbnailAsync(FileInfo file, uint size, CoreViewModel? core = null,
            MagickImage? magick = null) => ValueTask.FromResult<object?>(null);

        public object? GetExifThumbnail(FileInfo file) => null;
        public object? GetThumbQuick(FileInfo file, CoreViewModel? core = null) => null;
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }
}
