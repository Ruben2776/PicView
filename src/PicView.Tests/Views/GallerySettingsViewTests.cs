using Avalonia.Controls;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Config;
using PicView.Core.Gallery;
using PicView.Core.Models;
using PicView.Core.ViewModels;
using PicView.Tests.Config;
using R3;

namespace PicView.Tests.Views;

/// <summary>
/// The Settings window's gallery page shows and edits the Active Main Window's gallery look, and follows when another
/// Main Window becomes active; its item spacing, line spacing and motion photo badges stay shared.
/// </summary>
[Collection("Sequential")]
public class GallerySettingsViewTests
{
    public GallerySettingsViewTests()
    {
        SetDefaults();
    }

    private sealed class Page
    {
        public required CoreViewModel Core { get; init; }
        public required MainWindowViewModel A { get; init; }
        public required MainWindowViewModel B { get; init; }
        public required GallerySettingsView View { get; init; }
        public required Window Window { get; init; }

        // Saves of the shared gallery settings and of the global settings
        public required Func<int> Saves { get; init; }

        // Executions of either window's look commands
        public required Func<int> Commands { get; init; }

        public ComboBox DockedStretch => View.FindControl<ComboBox>("DockedGalleryComboBox")!;
        public ComboBox ExpandedStretch => View.FindControl<ComboBox>("FullGalleryComboBox")!;
        public ComboBox DisplayMode => View.FindControl<ComboBox>("DisplayModeComboBox")!;
        public CustomSlider DockedSize => View.FindControl<CustomSlider>("DockedGallerySlider")!;
        public CustomSlider ExpandedSize => View.FindControl<CustomSlider>("ExpandedGallerySlider")!;

        public void Activate(MainWindowViewModel? window)
        {
            Core.MainWindows.ActiveWindow.Value = window;
            Dispatcher.UIThread.RunJobs();
        }
    }

    // Two Main Windows with their own gallery look, the first one active, and the page shown
    private static Page Show()
    {
        var saves = 0;
        Func<ValueTask<bool>> save = () =>
        {
            saves++;
            return ValueTask.FromResult(true);
        };
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()), StartedSettings.New(save),
            save);
        core.GallerySettings.Start();
        var a = NewWindow(core);
        var b = NewWindow(core);
        SetLook(a.WindowSettings.Gallery, GalleryStretchMode.Uniform, GalleryStretchMode.Square,
            GalleryDisplayMode.Columns, 80, 200);
        // A size between two ticks, which a snap to the tick would round
        SetLook(b.WindowSettings.Gallery, GalleryStretchMode.FillSquare, GalleryStretchMode.UniformToFill,
            GalleryDisplayMode.Rows, 100.5, 120.5);
        core.MainWindows.ActiveWindow.Value = a;

        var commands = 0;
        foreach (var gallery in new[] { a.WindowSettings.Gallery, b.WindowSettings.Gallery })
        {
            gallery.SetDockedStretchModeCommand.Subscribe(_ => commands++);
            gallery.SetExpandedStretchModeCommand.Subscribe(_ => commands++);
            gallery.SetExpandedDisplayModeCommand.Subscribe(_ => commands++);
        }

        var view = new GallerySettingsView();
        var window = new Window { Width = 900, Height = 2000, DataContext = core, Content = view };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return new Page
        {
            Core = core, A = a, B = b, View = view, Window = window, Saves = () => saves, Commands = () => commands
        };
    }

    // A Main Window with its gallery settings started, as an opened window's are
    private static MainWindowViewModel NewWindow(CoreViewModel core)
    {
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        vm.WindowSettings.Gallery.Start();
        core.MainWindows.MainWindows.Add(vm);
        return vm;
    }

    private static void SetLook(WindowGallerySettings gallery, GalleryStretchMode docked, GalleryStretchMode expanded,
        GalleryDisplayMode displayMode, double dockedSize, double expandedSize)
    {
        gallery.DockedGalleryStretchMode.Value = docked;
        gallery.ExpandedGalleryStretchMode.Value = expanded;
        gallery.ExpandedGalleryDisplayMode.Value = displayMode;
        gallery.DockedGalleryItemSize.Value = dockedSize;
        gallery.ExpandedGalleryItemSize.Value = expandedSize;
    }

    private static (GalleryStretchMode, GalleryStretchMode, GalleryDisplayMode, double, double) LookOf(
        MainWindowViewModel window)
    {
        var gallery = window.WindowSettings.Gallery;
        return (gallery.DockedGalleryStretchMode.Value, gallery.ExpandedGalleryStretchMode.Value,
            gallery.ExpandedGalleryDisplayMode.Value, gallery.DockedGalleryItemSize.Value,
            gallery.ExpandedGalleryItemSize.Value);
    }

    private static (GalleryStretchMode, GalleryStretchMode, GalleryDisplayMode, double, double) RecordLook()
    {
        var record = Settings.Gallery;
        return (record.DockedGalleryStretchMode, record.ExpandedGalleryStretchMode, record.ExpandedGalleryDisplayMode,
            record.DockedGalleryItemSize, record.ExpandedGalleryItemSize);
    }

    private static object? Selected<T>(ComboBox comboBox) =>
        comboBox.SelectedItem is ComboBoxItem { Tag: T tag } ? tag : null;

    private static void Select<T>(ComboBox comboBox, T value)
    {
        comboBox.SelectedItem = comboBox.Items.OfType<ComboBoxItem>().Single(x => Equals(x.Tag, value));
        Dispatcher.UIThread.RunJobs();
    }

    private static void AssertShows(Page page, MainWindowViewModel window)
    {
        var (docked, expanded, displayMode, dockedSize, expandedSize) = LookOf(window);
        Assert.Equal(docked, Selected<GalleryStretchMode>(page.DockedStretch));
        Assert.Equal(expanded, Selected<GalleryStretchMode>(page.ExpandedStretch));
        Assert.Equal(displayMode, Selected<GalleryDisplayMode>(page.DisplayMode));
        Assert.Equal(dockedSize, page.DockedSize.Value);
        Assert.Equal(expandedSize, page.ExpandedSize.Value);
    }

    [AvaloniaFact]
    public void Page_ShowsTheActiveMainWindowsGalleryValues_AndFollowsASwitch()
    {
        var page = Show();
        try
        {
            AssertShows(page, page.A);

            page.Activate(page.B);
            AssertShows(page, page.B);

            page.Activate(page.A);
            AssertShows(page, page.A);

            // A change made elsewhere in the active window shows too
            page.A.WindowSettings.Gallery.SetDockedStretchModeCommand.Execute(GalleryStretchMode.UniformToFill);
            Dispatcher.UIThread.RunJobs();
            AssertShows(page, page.A);
        }
        finally
        {
            page.Window.Close();
        }
    }

    [AvaloniaFact]
    public void Page_SwitchingTheActiveMainWindow_IssuesNoCommand_AndWritesNothing()
    {
        var page = Show();
        try
        {
            var a = LookOf(page.A);
            var b = LookOf(page.B);
            var record = RecordLook();
            var published = 0;
            using var watch = Observable.Merge(page.A.WindowSettings.Gallery.Look, page.B.WindowSettings.Gallery.Look)
                .Skip(2)
                .Subscribe(_ => published++);

            page.Activate(page.B);
            page.Activate(page.A);
            page.Activate(null);
            page.Activate(page.B);

            AssertShows(page, page.B);
            Assert.Equal(0, page.Commands());
            Assert.Equal(0, published);
            Assert.Equal(a, LookOf(page.A));
            Assert.Equal(b, LookOf(page.B));
            Assert.Equal(record, RecordLook());
            Assert.Equal(0, page.Saves());
        }
        finally
        {
            page.Window.Close();
        }
    }

    [AvaloniaFact]
    public void Page_AChange_EditsOnlyTheActiveMainWindow()
    {
        var page = Show();
        try
        {
            var a = LookOf(page.A);
            var record = RecordLook();
            page.Activate(page.B);

            // The first window's values, which the page must not still be showing
            Select(page.DockedStretch, GalleryStretchMode.Uniform);
            Select(page.ExpandedStretch, GalleryStretchMode.Square);
            Select(page.DisplayMode, GalleryDisplayMode.Columns);

            Assert.Equal(GalleryStretchMode.Uniform, page.B.WindowSettings.Gallery.DockedGalleryStretchMode.Value);
            Assert.Equal(GalleryStretchMode.Square, page.B.WindowSettings.Gallery.ExpandedGalleryStretchMode.Value);
            Assert.Equal(GalleryDisplayMode.Columns, page.B.WindowSettings.Gallery.ExpandedGalleryDisplayMode.Value);
            Assert.Equal(3, page.Commands());
            Assert.Equal(a, LookOf(page.A));
            Assert.Equal(record, RecordLook());
            Assert.Equal(0, page.Saves());
        }
        finally
        {
            page.Window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Page_ItemSizeSlider_WritesTheActiveWindow_AndSavesNothing()
    {
        var page = Show();
        try
        {
            var a = LookOf(page.A);
            var record = RecordLook();
            page.Activate(page.B);

            page.DockedSize.Value = 130;
            page.ExpandedSize.Value = 230;
            Dispatcher.UIThread.RunJobs();
            await page.Core.GallerySettings.Saving;

            Assert.Equal(130, page.B.WindowSettings.Gallery.DockedGalleryItemSize.Value);
            Assert.Equal(230, page.B.WindowSettings.Gallery.ExpandedGalleryItemSize.Value);
            Assert.Equal(a, LookOf(page.A));
            Assert.Equal(record, RecordLook());
            Assert.Equal(0, page.Saves());
            Assert.Equal(0, page.Commands());
        }
        finally
        {
            page.Window.Close();
        }
    }

    [AvaloniaFact]
    public void Page_NoActiveMainWindow_DoesNothing()
    {
        var page = Show();
        try
        {
            var a = LookOf(page.A);
            var b = LookOf(page.B);
            var record = RecordLook();
            page.Activate(null);

            var exception = Record.Exception(() =>
            {
                Select(page.DockedStretch, GalleryStretchMode.Square);
                Select(page.ExpandedStretch, GalleryStretchMode.FillSquare);
                Select(page.DisplayMode, GalleryDisplayMode.Masonry);
                page.DockedSize.Value = 140;
                page.ExpandedSize.Value = 240;
                Dispatcher.UIThread.RunJobs();
            });

            Assert.Null(exception);
            Assert.Equal(0, page.Commands());
            Assert.Equal(a, LookOf(page.A));
            Assert.Equal(b, LookOf(page.B));
            Assert.Equal(record, RecordLook());
            Assert.Equal(0, page.Saves());
        }
        finally
        {
            page.Window.Close();
        }
    }

    [AvaloniaFact]
    public async Task Page_TheStayingSettings_StillWriteThroughAndSave()
    {
        var page = Show();
        try
        {
            var a = LookOf(page.A);
            var b = LookOf(page.B);
            var spacing = page.View.FindControl<CustomSlider>("ItemSpacingSlider")!;
            var lineSpacing = page.View.FindControl<CustomSlider>("LineSpacingSlider")!;
            var badges = page.View.FindControl<TextToggleButton>("MotionPhotoBadgesToggleButton")!;
            var newSpacing = Settings.Gallery.ItemSpacing + 3;
            var newLineSpacing = Settings.Gallery.LineSpacing + 2;
            var showBadges = !Settings.Gallery.ShowMotionPhotoBadges;

            spacing.Value = newSpacing;
            lineSpacing.Value = newLineSpacing;
            badges.IsChecked = showBadges;
            Dispatcher.UIThread.RunJobs();
            await page.Core.GallerySettings.Saving;

            Assert.Equal(newSpacing, page.Core.GallerySettings.GalleryItemSpacing.Value);
            Assert.Equal(newSpacing, Settings.Gallery.ItemSpacing);
            Assert.Equal(newLineSpacing, page.Core.GallerySettings.GalleryLineSpacing.Value);
            Assert.Equal(newLineSpacing, Settings.Gallery.LineSpacing);
            Assert.Equal(showBadges, page.Core.GallerySettings.ShowMotionPhotoBadges.Value);
            Assert.Equal(showBadges, Settings.Gallery.ShowMotionPhotoBadges);
            Assert.InRange(page.Saves(), 1, 3);
            Assert.Equal(a, LookOf(page.A));
            Assert.Equal(b, LookOf(page.B));
        }
        finally
        {
            page.Window.Close();
        }
    }

    [AvaloniaFact]
    public void Page_ActiveMainWindowClosesUnderIt_FollowsTheRemainingWindow()
    {
        var page = Show();
        try
        {
            var b = LookOf(page.B);
            var record = RecordLook();

            // As a close does: the window's settings are disposed, then the remaining window becomes active
            var exception = Record.Exception(() =>
            {
                page.A.WindowSettings.Dispose();
                Dispatcher.UIThread.RunJobs();
                page.Core.MainWindows.MainWindows.Remove(page.A);
                page.Activate(page.B);
            });

            Assert.Null(exception);
            AssertShows(page, page.B);
            Assert.Equal(0, page.Commands());
            Assert.Equal(b, LookOf(page.B));

            Select(page.DisplayMode, GalleryDisplayMode.Masonry);
            Assert.Equal(GalleryDisplayMode.Masonry, page.B.WindowSettings.Gallery.ExpandedGalleryDisplayMode.Value);
            Assert.Equal(1, page.Commands());
            Assert.Equal(record, RecordLook());
            Assert.Equal(0, page.Saves());
        }
        finally
        {
            page.Window.Close();
        }
    }
}
