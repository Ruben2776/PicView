using PicView.Core.Config;
using PicView.Core.FileSorting;
using PicView.Core.Sizing;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Config;

[Collection("Sequential")]
public class WindowSettingsTests
{
    public WindowSettingsTests() => SetDefaults();

    [Fact]
    public void FromRecord_ReadsEveryWindowSetting()
    {
        var record = GetDefaults();
        record.WindowProperties = new WindowProperties
        {
            AutoFit = true, Maximized = true, Fullscreen = false, TopMost = true, KeepCentered = true,
            Left = 10, Top = 20, Width = 300, Height = 400
        };
        using var settings = WindowSettings.FromRecord(record);
        Assert.Equal(new RememberedWindowMode(false, true, true), settings.StartingMode);
        Assert.True(settings.IsTopMost.Value);
        Assert.True(settings.IsStayingCentered.Value);
        Assert.Equal(new NormalBounds(10, 20, 300, 400), settings.Bounds);
    }

    [Fact]
    public void FromRecord_NoWindowProperties_GivesTheDefaults()
    {
        using var fromNull = WindowSettings.FromRecord(null);
        using var fromEmpty = WindowSettings.FromRecord(new AppSettings());
        var defaults = new WindowProperties();
        foreach (var settings in new[] { fromNull, fromEmpty })
        {
            Assert.Equal(new RememberedWindowMode(defaults.Fullscreen, defaults.Maximized, defaults.AutoFit), settings.StartingMode);
            Assert.Equal(new NormalBounds(defaults.Left, defaults.Top, defaults.Width, defaults.Height), settings.Bounds);
        }
    }

    [Fact]
    public void Store_WritesEveryWindowSettingAndNothingElse()
    {
        var record = GetDefaults();
        record.WindowProperties!.Margin = 33;
        record.WindowProperties.EscLeavesFullscreen = false;
        using var settings = WindowSettings.FromRecord(GetDefaults());
        settings.IsTopMost.Value = true;
        settings.IsStayingCentered.Value = true;
        settings.Bounds = new NormalBounds(1, 2, 3, 4);
        settings.Store(record, new RememberedWindowMode(Fullscreen: true, Maximized: false, AutoFit: true));
        var stored = record.WindowProperties;
        Assert.True(stored.Fullscreen);
        Assert.False(stored.Maximized);
        Assert.True(stored.AutoFit);
        Assert.True(stored.TopMost);
        Assert.True(stored.KeepCentered);
        Assert.Equal((1d, 2d, 3d, 4d), (stored.Left, stored.Top, stored.Width, stored.Height));
        Assert.Equal(33, stored.Margin);
        Assert.False(stored.EscLeavesFullscreen);
    }

    [Fact]
    public void Store_RecordWithoutWindowProperties_CreatesTheGroup()
    {
        var record = new AppSettings();
        using var settings = WindowSettings.FromRecord(null);
        settings.Store(record, default);
        Assert.NotNull(record.WindowProperties);
    }

    [Fact]
    public void CopyFor_TakesTheOpenersValuesAndItsLiveMode_AndIsIndependentAfterwards()
    {
        using var opener = WindowSettings.FromRecord(GetDefaults());
        opener.IsTopMost.Value = true;
        opener.Bounds = new NormalBounds(5, 6, 7, 8);
        using var copy = opener.CopyFor(new RememberedWindowMode(false, true, true));
        Assert.Equal(new RememberedWindowMode(false, true, true), copy.StartingMode);
        Assert.True(copy.IsTopMost.Value);
        Assert.Equal(opener.Bounds, copy.Bounds);
        copy.IsTopMost.Value = false;
        Assert.True(opener.IsTopMost.Value);
    }

    [Fact]
    public void Store_WritesTheGalleryValues()
    {
        var record = GetDefaults();
        record.Gallery = null;
        using var settings = WindowSettings.FromRecord(GetDefaults());
        settings.Gallery.DockPosition.Value = Core.Gallery.GalleryDockPosition.Left;
        settings.Gallery.StartsDocked.Value = true;

        settings.Store(record, default);

        Assert.Equal(Core.Gallery.GalleryDockPosition.Left, record.Gallery!.DockPosition);
        Assert.True(record.Gallery.IsGalleryDocked);
    }

    [Fact]
    public void CopyFor_CopiesTheGalleryValues()
    {
        using var opener = WindowSettings.FromRecord(GetDefaults());
        opener.Gallery.DockPosition.Value = Core.Gallery.GalleryDockPosition.Top;
        opener.Gallery.StartsDocked.Value = true;

        using var copy = opener.CopyFor(default);
        Assert.Equal(Core.Gallery.GalleryDockPosition.Top, copy.Gallery.DockPosition.Value);
        Assert.True(copy.Gallery.StartsDocked.Value);
        copy.Gallery.DockPosition.Value = Core.Gallery.GalleryDockPosition.Right;
        Assert.Equal(Core.Gallery.GalleryDockPosition.Top, opener.Gallery.DockPosition.Value);

        opener.Dispose();
        Assert.True(opener.Gallery.IsDisposed);
        Assert.False(copy.Gallery.IsDisposed);
    }

    [Fact]
    public void FromRecord_ReadsTheFileOrder()
    {
        var record = GetDefaults();
        record.Sorting = new Sorting { SortPreference = 5, Ascending = false };
        using var settings = WindowSettings.FromRecord(record);
        Assert.Equal(new FileOrder(SortFilesBy.LastWriteTime, false), settings.FileOrder.Value);
    }

    [Fact]
    public void FromRecord_NoSorting_IsNameAscending()
    {
        using var fromNull = WindowSettings.FromRecord(null);
        using var fromEmpty = WindowSettings.FromRecord(new AppSettings { Sorting = null });
        Assert.Equal(new FileOrder(SortFilesBy.Name, true), fromNull.FileOrder.Value);
        Assert.Equal(new FileOrder(SortFilesBy.Name, true), fromEmpty.FileOrder.Value);
    }

    [Fact]
    public void Store_WritesTheFileOrderAndLeavesSubdirectoriesAlone()
    {
        var record = GetDefaults();
        record.Sorting = new Sorting { IncludeSubDirectories = true, SortPreference = 0, Ascending = true };
        using var settings = WindowSettings.FromRecord(GetDefaults());
        settings.FileOrder.Value = new FileOrder(SortFilesBy.FileSize, false);
        settings.Store(record, default);
        Assert.Equal(1, record.Sorting.SortPreference);
        Assert.False(record.Sorting.Ascending);
        Assert.True(record.Sorting.IncludeSubDirectories);
    }

    [Fact]
    public void Store_RecordWithoutSorting_CreatesTheGroup()
    {
        var record = new AppSettings { Sorting = null };
        using var settings = WindowSettings.FromRecord(null);
        settings.FileOrder.Value = new FileOrder(SortFilesBy.Extension, true);
        settings.Store(record, default);
        Assert.NotNull(record.Sorting);
        Assert.Equal(3, record.Sorting.SortPreference);
        Assert.True(record.Sorting.Ascending);
    }

    [Fact]
    public void CopyFor_TakesTheOpenersFileOrder_AndIsIndependentAfterwards()
    {
        using var opener = WindowSettings.FromRecord(GetDefaults());
        opener.FileOrder.Value = new FileOrder(SortFilesBy.CreationTime, false);
        using var copy = opener.CopyFor(default);
        Assert.Equal(new FileOrder(SortFilesBy.CreationTime, false), copy.FileOrder.Value);
        copy.FileOrder.Value = new FileOrder(SortFilesBy.Random, true);
        Assert.Equal(new FileOrder(SortFilesBy.CreationTime, false), opener.FileOrder.Value);
    }

    [Fact]
    public void FileOrderChange_WritesNothingToTheRecord()
    {
        using var settings = WindowSettings.FromRecord(Settings);
        var before = (Settings.Sorting!.SortPreference, Settings.Sorting.Ascending);
        settings.FileOrder.Value = new FileOrder(SortFilesBy.LastAccessTime, !before.Ascending);
        Assert.Equal(before, (Settings.Sorting.SortPreference, Settings.Sorting.Ascending));
    }

    [Fact]
    public void Change_WritesNothingToTheRecord()
    {
        using var settings = WindowSettings.FromRecord(Settings);
        settings.IsTopMost.Value = !settings.IsTopMost.Value;
        settings.IsStayingCentered.Value = !settings.IsStayingCentered.Value;
        var ui = Settings.UIProperties!;
        var uiBefore = (ui.Looping, ui.ShowInterface, ui.ShowBottomNavBar, ui.ShowHoverNavigationBar, ui.BgColorChoice,
            ui.IsConstrainBackgroundColorEnabled);
        var otherBefore = (Settings.ImageScaling!.ShowImageSideBySide, Settings.ImageScaling.ZoomToFit, Settings.Zoom!.ScrollEnabled);

        settings.IsSideBySide.Value = !settings.IsSideBySide.Value;
        settings.IsLooping.Value = !settings.IsLooping.Value;
        settings.IsZoomedToFit.Value = !settings.IsZoomedToFit.Value;
        settings.IsScrollingEnabled.Value = !settings.IsScrollingEnabled.Value;
        settings.ShowInterface.Value = !settings.ShowInterface.Value;
        settings.ShowBottomBar.Value = !settings.ShowBottomBar.Value;
        settings.ShowHoverBar.Value = !settings.ShowHoverBar.Value;
        settings.BackgroundChoice.Value = ui.BgColorChoice + 1;
        settings.IsConstrainingBackgroundColor.Value = !settings.IsConstrainingBackgroundColor.Value;

        Assert.Equal(new WindowProperties().TopMost, Settings.WindowProperties!.TopMost);
        Assert.Equal(new WindowProperties().KeepCentered, Settings.WindowProperties.KeepCentered);
        Assert.Equal(uiBefore, (ui.Looping, ui.ShowInterface, ui.ShowBottomNavBar, ui.ShowHoverNavigationBar, ui.BgColorChoice,
            ui.IsConstrainBackgroundColorEnabled));
        Assert.Equal(otherBefore, (Settings.ImageScaling.ShowImageSideBySide, Settings.ImageScaling.ZoomToFit, Settings.Zoom.ScrollEnabled));
    }

    private static AppSettings RecordWithTheNineSet()
    {
        var record = GetDefaults();
        record.ImageScaling!.ShowImageSideBySide = true;
        record.ImageScaling.ZoomToFit = true;
        record.UIProperties!.Looping = true;
        record.UIProperties.ShowInterface = false;
        record.UIProperties.ShowBottomNavBar = false;
        record.UIProperties.ShowHoverNavigationBar = false;
        record.UIProperties.BgColorChoice = 4;
        record.UIProperties.IsConstrainBackgroundColorEnabled = true;
        record.Zoom!.ScrollEnabled = true;
        return record;
    }

    private static void AssertTheNine(WindowSettings settings)
    {
        Assert.True(settings.IsSideBySide.Value);
        Assert.True(settings.IsLooping.Value);
        Assert.True(settings.IsZoomedToFit.Value);
        Assert.True(settings.IsScrollingEnabled.Value);
        Assert.False(settings.ShowInterface.Value);
        Assert.False(settings.ShowBottomBar.Value);
        Assert.False(settings.ShowHoverBar.Value);
        Assert.Equal(4, settings.BackgroundChoice.Value);
        Assert.True(settings.IsConstrainingBackgroundColor.Value);
    }

    [Fact]
    public void FromRecord_ReadsTheNineSettings()
    {
        using var settings = WindowSettings.FromRecord(RecordWithTheNineSet());
        AssertTheNine(settings);
    }

    [Fact]
    public void FromRecord_MissingGroups_GivesTheDefaults()
    {
        var ui = new UIProperties();
        var scaling = new ImageScaling();
        var zoom = new Zoom();
        using var fromNull = WindowSettings.FromRecord(null);
        using var fromEmpty = WindowSettings.FromRecord(new AppSettings
        {
            UIProperties = null, ImageScaling = null, Zoom = null
        });
        foreach (var settings in new[] { fromNull, fromEmpty })
        {
            Assert.Equal(scaling.ShowImageSideBySide, settings.IsSideBySide.Value);
            Assert.Equal(ui.Looping, settings.IsLooping.Value);
            Assert.Equal(scaling.ZoomToFit, settings.IsZoomedToFit.Value);
            Assert.Equal(zoom.ScrollEnabled, settings.IsScrollingEnabled.Value);
            Assert.Equal(ui.ShowInterface, settings.ShowInterface.Value);
            Assert.Equal(ui.ShowBottomNavBar, settings.ShowBottomBar.Value);
            Assert.Equal(ui.ShowHoverNavigationBar, settings.ShowHoverBar.Value);
            Assert.Equal(ui.BgColorChoice, settings.BackgroundChoice.Value);
            Assert.Equal(ui.IsConstrainBackgroundColorEnabled, settings.IsConstrainingBackgroundColor.Value);
            Assert.Null(settings.ImageBackground.Value);
            Assert.Null(settings.ConstrainedImageBackground.Value);
        }
    }

    [Fact]
    public void Store_WritesTheNineSettings_AndLeavesTheAppSettingsOfThoseGroupsAlone()
    {
        var record = GetDefaults();
        record.UIProperties!.OpeningMode = record.UIProperties.OpeningMode == OpeningMode.NewTab ? OpeningMode.NewWindow : OpeningMode.NewTab;
        record.Zoom!.ZoomSpeed = 7.5;
        record.ImageScaling!.IsScalingSetToNearestNeighbor = !record.ImageScaling.IsScalingSetToNearestNeighbor;
        var openingMode = record.UIProperties.OpeningMode;
        var zoomSpeed = record.Zoom.ZoomSpeed;
        var nearestNeighbor = record.ImageScaling.IsScalingSetToNearestNeighbor;

        using var settings = WindowSettings.FromRecord(RecordWithTheNineSet());
        settings.Store(record, default);

        Assert.True(record.ImageScaling.ShowImageSideBySide);
        Assert.True(record.ImageScaling.ZoomToFit);
        Assert.True(record.UIProperties.Looping);
        Assert.False(record.UIProperties.ShowInterface);
        Assert.False(record.UIProperties.ShowBottomNavBar);
        Assert.False(record.UIProperties.ShowHoverNavigationBar);
        Assert.Equal(4, record.UIProperties.BgColorChoice);
        Assert.True(record.UIProperties.IsConstrainBackgroundColorEnabled);
        Assert.True(record.Zoom.ScrollEnabled);
        Assert.Equal(openingMode, record.UIProperties.OpeningMode);
        Assert.Equal(zoomSpeed, record.Zoom.ZoomSpeed);
        Assert.Equal(nearestNeighbor, record.ImageScaling.IsScalingSetToNearestNeighbor);
    }

    [Fact]
    public void Store_RecordWithoutTheGroups_CreatesThem()
    {
        var record = new AppSettings { UIProperties = null, ImageScaling = null, Zoom = null };
        using var settings = WindowSettings.FromRecord(RecordWithTheNineSet());
        settings.Store(record, default);
        Assert.True(record.UIProperties!.Looping);
        Assert.True(record.ImageScaling!.ZoomToFit);
        Assert.True(record.Zoom!.ScrollEnabled);
    }

    [Fact]
    public void CopyFor_TakesTheNineValues_AndIsIndependentAfterwards()
    {
        using var opener = WindowSettings.FromRecord(RecordWithTheNineSet());
        using var copy = opener.CopyFor(default);
        AssertTheNine(copy);

        copy.IsSideBySide.Value = false;
        copy.IsLooping.Value = false;
        copy.IsZoomedToFit.Value = false;
        copy.IsScrollingEnabled.Value = false;
        copy.ShowInterface.Value = true;
        copy.ShowBottomBar.Value = true;
        copy.ShowHoverBar.Value = true;
        copy.BackgroundChoice.Value = 1;
        copy.IsConstrainingBackgroundColor.Value = false;
        AssertTheNine(opener);
    }

    [Fact]
    public void CopyFor_KeepsTheTranslations_SoTheCopysTextsFollowTheLanguage()
    {
        var translations = new TranslationViewModel();
        translations.LoopingEnabled.Value = "on";
        translations.LoopingDisabled.Value = "off";
        using var opener = WindowSettings.FromRecord(GetDefaults(), translations);
        using var copy = opener.CopyFor(default);
        opener.IsLooping.Value = false;
        copy.IsLooping.Value = true;
        Assert.Equal("on", copy.LoopingText.Value);
        translations.LoopingEnabled.Value = "on-2";
        Assert.Equal("on-2", copy.LoopingText.Value);
    }

    private sealed record TextCase(
        WindowSettings Settings,
        Func<WindowSettings, BindableReactiveProperty<bool>> Setting,
        Func<WindowSettings, IReadOnlyBindableReactiveProperty<string?>> Text,
        BindableReactiveProperty<string?> WhenOn,
        BindableReactiveProperty<string?> WhenOff);

    private static TextCase TextCaseOf(string name, TranslationViewModel t)
    {
        var settings = WindowSettings.FromRecord(GetDefaults(), t);
        return name switch
        {
            "Looping" => new TextCase(settings, static s => s.IsLooping, static s => s.LoopingText, t.LoopingEnabled, t.LoopingDisabled),
            "Scrolling" => new TextCase(settings, static s => s.IsScrollingEnabled, static s => s.ScrollingText, t.ScrollEnabled, t.ScrollDisabled),
            "Interface" => new TextCase(settings, static s => s.ShowInterface, static s => s.InterfaceText, t.HideUI, t.ShowUI),
            "BottomBar" => new TextCase(settings, static s => s.ShowBottomBar, static s => s.BottomBarText, t.HideBottomToolbar, t.ShowBottomToolbar),
            "HoverBar" => new TextCase(settings, static s => s.ShowHoverBar, static s => s.HoverBarText, t.HideHoverNavigationBar, t.ShowHoverNavigationBar),
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
    }

    [Theory]
    [InlineData("Looping")]
    [InlineData("Scrolling")]
    [InlineData("Interface")]
    [InlineData("BottomBar")]
    [InlineData("HoverBar")]
    public void Texts_FollowTheSettingAndTheLanguage(string name)
    {
        var translations = new TranslationViewModel();
        var c = TextCaseOf(name, translations);
        using var settings = c.Settings;
        c.WhenOn.Value = "on";
        c.WhenOff.Value = "off";

        c.Setting(settings).Value = true;
        Assert.Equal("on", c.Text(settings).Value);
        c.Setting(settings).Value = false;
        Assert.Equal("off", c.Text(settings).Value);

        // A language change: the translation bindable changes under an unchanged setting
        c.WhenOff.Value = "off-2";
        Assert.Equal("off-2", c.Text(settings).Value);
        c.Setting(settings).Value = true;
        c.WhenOn.Value = "on-2";
        Assert.Equal("on-2", c.Text(settings).Value);
    }

    [Fact]
    public void Texts_WithoutTranslations_AreNull()
    {
        using var settings = WindowSettings.FromRecord(GetDefaults());
        settings.IsLooping.Value = !settings.IsLooping.Value;
        Assert.Null(settings.LoopingText.Value);
        Assert.Null(settings.ScrollingText.Value);
        Assert.Null(settings.InterfaceText.Value);
        Assert.Null(settings.BottomBarText.Value);
        Assert.Null(settings.HoverBarText.Value);
    }

    [Fact]
    public void Dispose_ATextNoLongerFollows()
    {
        var translations = new TranslationViewModel();
        translations.HideUI.Value = "hide";
        translations.ShowUI.Value = "show";
        var settings = WindowSettings.FromRecord(GetDefaults(), translations);
        var text = settings.InterfaceText;
        settings.ShowInterface.Value = true;
        Assert.Equal("hide", text.Value);

        settings.Dispose();
        translations.HideUI.Value = "hide-2";

        Assert.Equal("hide", text.Value);
    }

    [Fact]
    public void Dispose_MarksTheSettingsDisposed()
    {
        var settings = WindowSettings.FromRecord(GetDefaults());
        Assert.False(settings.IsDisposed);

        settings.Dispose();

        Assert.True(settings.IsDisposed);
    }

    // What the guarded change in ToggleHelper exists for: R3 throws on a set once the bindable is disposed
    [Fact]
    public void WindowSettings_SetAfterDispose_Throws()
    {
        var settings = WindowSettings.FromRecord(GetDefaults());

        settings.Dispose();

        Assert.Throws<ObjectDisposedException>(() => settings.IsLooping.Value = !settings.IsLooping.Value);
    }
}
