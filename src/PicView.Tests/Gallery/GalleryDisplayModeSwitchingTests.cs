using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Config;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
using PicView.Core.SettingsSearch;
using PicView.Core.ViewModels;
using R3;

namespace PicView.Tests.Gallery;

/// <summary>
/// The places that switch the Gallery Display Mode: the gallery's two buttons and its menus, and the Settings window.
/// </summary>
[Collection("Sequential")]
public class GalleryDisplayModeSwitchingTests
{
    public GalleryDisplayModeSwitchingTests()
    {
        ObservableSystem.DefaultFrameProvider = new MockFrameProvider();
        TranslationManager.Init();
        SetDefaults();
    }

    private sealed class MockFrameProvider : FrameProvider
    {
        public override long GetFrameCount() => 0;
        public override void Register(IFrameRunnerWorkItem callback) => callback.MoveNext(0);
    }

    [AvaloniaFact]
    public void SwitchButtons_InColumns_ShowTheToTilesButtonWithTheToggleCommandAndATooltip()
    {
        WithGalleryView((core, _, view) =>
        {
            Assert.True(TranslationManager.LoadLanguage("en"));
            core.Translation.UpdateLanguage();
            var toTiles = view.FindControl<IconButton>("ToTilesButton")!;
            var toColumns = view.FindControl<IconButton>("ToColumnsButton")!;

            Assert.True(toTiles.IsVisible);
            Assert.False(toColumns.IsVisible);
            Assert.Same(core.GallerySettings.ToggleExpandedDisplayModeCommand, toTiles.Command);
            Assert.Same(core.GallerySettings.ToggleExpandedDisplayModeCommand, toColumns.Command);
            Assert.Equal("Switch gallery display mode", ToolTip.GetTip(toTiles));
            Assert.Equal("Switch gallery display mode", ToolTip.GetTip(toColumns));
        });
    }

    [AvaloniaTheory]
    [InlineData(GalleryDisplayMode.Masonry)]
    [InlineData(GalleryDisplayMode.Rows)]
    public void SwitchButtons_InMasonryOrRows_ShowTheToColumnsButton(GalleryDisplayMode mode)
    {
        WithGalleryView((core, _, view) =>
        {
            core.GallerySettings.SetExpandedDisplayModeCommand.Execute(mode);

            Assert.False(view.FindControl<IconButton>("ToTilesButton")!.IsVisible);
            Assert.True(view.FindControl<IconButton>("ToColumnsButton")!.IsVisible);
        });
    }

    [AvaloniaTheory]
    [InlineData(GalleryDisplayMode.Columns, true)]
    [InlineData(GalleryDisplayMode.Masonry, false)]
    [InlineData(GalleryDisplayMode.Rows, false)]
    public void GalleryMenu_Expanded_ShowsTheExpandedStretchOnlyInColumns(GalleryDisplayMode mode, bool stretchIsShown)
    {
        WithGalleryView((core, tab, view) =>
        {
            var menu = MenuOf(view, tab);
            tab.Gallery.Switcher.ToggleExpanded();

            core.GallerySettings.SetExpandedDisplayModeCommand.Execute(mode);

            Assert.True(Item(menu, "DisplayModeMenuItem").IsVisible);
            Assert.Equal(stretchIsShown, Item(menu, "ExpandedStretchMenuItem").IsVisible);
            Assert.False(Item(menu, "DockedStretchMenuItem").IsVisible);
        });
    }

    [AvaloniaFact]
    public void GalleryMenu_NotExpanded_ShowsTheDockedStretchAndNoDisplayMode()
    {
        WithGalleryView((core, tab, view) =>
        {
            var menu = MenuOf(view, tab);

            core.GallerySettings.SetExpandedDisplayModeCommand.Execute(GalleryDisplayMode.Masonry);

            Assert.False(Item(menu, "DisplayModeMenuItem").IsVisible);
            Assert.False(Item(menu, "ExpandedStretchMenuItem").IsVisible);
            Assert.True(Item(menu, "DockedStretchMenuItem").IsVisible);
        });
    }

    [AvaloniaTheory]
    [InlineData("DisplayModeColumnsMenuItem", GalleryDisplayMode.Columns)]
    [InlineData("DisplayModeMasonryMenuItem", GalleryDisplayMode.Masonry)]
    [InlineData("DisplayModeRowsMenuItem", GalleryDisplayMode.Rows)]
    public void GalleryMenu_DisplayModeItem_SetsItsModeAndIsTheOnlyOneChecked(string itemName, GalleryDisplayMode mode)
    {
        WithGalleryView((core, tab, view) =>
        {
            var displayModes = Item(MenuOf(view, tab), "DisplayModeMenuItem");
            // Start from another mode, so Columns is a change too
            core.GallerySettings.SetExpandedDisplayModeCommand.Execute(
                mode is GalleryDisplayMode.Columns ? GalleryDisplayMode.Rows : GalleryDisplayMode.Columns);
            var item = Item(displayModes, itemName);

            item.Command!.Execute(item.CommandParameter);

            Assert.Equal(mode, Settings.Gallery.ExpandedGalleryDisplayMode);
            Assert.Equal(MenuItemToggleType.Radio, item.ToggleType);
            Assert.Equal([itemName], displayModes.Items.OfType<MenuItem>().Where(x => x.IsChecked).Select(x => x.Name));
        });
    }

    [AvaloniaFact]
    public void ItemMenu_HasTheThreeDisplayModesAsRadioItems()
    {
        var galleryItem = new GalleryItem();
        Assert.True(galleryItem.Resources.TryGetResource("GalleryContextMenu", ThemeVariant.Default, out var resource));
        var displayModes = Item(Assert.IsType<ContextMenu>(resource), "ItemDisplayModeMenuItem")
            .Items.OfType<MenuItem>().ToList();

        Assert.Equal([GalleryDisplayMode.Columns, GalleryDisplayMode.Masonry, GalleryDisplayMode.Rows],
            displayModes.Select(x => (GalleryDisplayMode)x.CommandParameter!));
        Assert.All(displayModes, x => Assert.Equal(MenuItemToggleType.Radio, x.ToggleType));
    }

    // A context menu takes its DataContext from its control as it opens; the tests give it directly, so no popup opens
    [AvaloniaTheory]
    [InlineData("DisplayModeMasonryItem", GalleryDisplayMode.Masonry)]
    [InlineData("DisplayModeRowsItem", GalleryDisplayMode.Rows)]
    public void SettingsComboBox_PickingAnItem_SetsThatDisplayMode(string itemName, GalleryDisplayMode expected)
    {
        WithGallerySettings((core, view) =>
        {
            view.FindControl<ComboBox>("DisplayModeComboBox")!.SelectedItem = view.FindControl<ComboBoxItem>(itemName);

            Assert.Equal(expected, core.GallerySettings.ExpandedGalleryDisplayMode.Value);
            Assert.Equal(expected, Settings.Gallery.ExpandedGalleryDisplayMode);
            Assert.Equal(expected is GalleryDisplayMode.Rows, Settings.Gallery.RowsWasLastUsedDisplayMode);
        });
    }

    [AvaloniaFact]
    public void SettingsComboBox_PickingColumnsAfterRows_SetsColumnsAndStillRemembersRows()
    {
        WithGallerySettings((core, view) =>
        {
            var comboBox = view.FindControl<ComboBox>("DisplayModeComboBox")!;
            comboBox.SelectedItem = view.FindControl<ComboBoxItem>("DisplayModeRowsItem");

            comboBox.SelectedItem = view.FindControl<ComboBoxItem>("DisplayModeColumnsItem");

            Assert.Equal(GalleryDisplayMode.Columns, core.GallerySettings.ExpandedGalleryDisplayMode.Value);
            Assert.Equal(GalleryDisplayMode.Columns, Settings.Gallery.ExpandedGalleryDisplayMode);
            Assert.True(Settings.Gallery.RowsWasLastUsedDisplayMode);
        });
    }

    [AvaloniaTheory]
    [InlineData("DisplayModeColumnsItem", GalleryDisplayMode.Columns)]
    [InlineData("DisplayModeMasonryItem", GalleryDisplayMode.Masonry)]
    [InlineData("DisplayModeRowsItem", GalleryDisplayMode.Rows)]
    public void SettingsComboBox_CommandSetsDisplayMode_ShowsIt(string itemName, GalleryDisplayMode mode)
    {
        WithGallerySettings((core, view) =>
        {
            var comboBox = view.FindControl<ComboBox>("DisplayModeComboBox")!;

            // Pick something else in the combo box first, so it has to follow the command rather than keep its selection
            comboBox.SelectedIndex = mode is GalleryDisplayMode.Columns ? 1 : 0;
            core.GallerySettings.SetExpandedDisplayModeCommand.Execute(mode);

            Assert.Same(view.FindControl<ComboBoxItem>(itemName), comboBox.SelectedItem);
        });
    }

    [AvaloniaFact]
    public void SettingsComboBox_ToggleCommandFromColumns_ShowsMasonry()
    {
        WithGallerySettings((core, view) =>
        {
            core.GallerySettings.ToggleExpandedDisplayModeCommand.Execute(Unit.Default);

            Assert.Same(view.FindControl<ComboBoxItem>("DisplayModeMasonryItem"),
                view.FindControl<ComboBox>("DisplayModeComboBox")!.SelectedItem);
        });
    }

    // Review Focus 4. Showing the settings selects the saved mode in the combo box, which raises SelectionChanged and so
    // runs the command. In every saved state that must write nothing: no new value, no change to which of Masonry and
    // Rows was used last, no save
    [AvaloniaTheory]
    [InlineData(GalleryDisplayMode.Columns, true, "DisplayModeColumnsItem")]
    [InlineData(GalleryDisplayMode.Columns, false, "DisplayModeColumnsItem")]
    [InlineData(GalleryDisplayMode.Masonry, false, "DisplayModeMasonryItem")]
    [InlineData(GalleryDisplayMode.Rows, true, "DisplayModeRowsItem")]
    // A file edited by hand, where the last used mode disagrees with the saved one, is left as it is too
    [InlineData(GalleryDisplayMode.Masonry, true, "DisplayModeMasonryItem")]
    [InlineData(GalleryDisplayMode.Rows, false, "DisplayModeRowsItem")]
    public void SettingsComboBox_Shown_ShowsTheSavedModeAndWritesNothing(GalleryDisplayMode saved, bool rowsWasLastUsed,
        string itemName)
    {
        Settings.Gallery.ExpandedGalleryDisplayMode = saved;
        Settings.Gallery.RowsWasLastUsedDisplayMode = rowsWasLastUsed;
        // As WithGallerySettings, with the setting watched from before the view exists
        var saves = 0;
        var core = NewCore(() =>
        {
            saves++;
            return ValueTask.FromResult(true);
        });
        core.GallerySettings.Start();
        var idle = core.GallerySettings.Saving;
        var published = 0;
        using var subscription = core.GallerySettings.ExpandedGalleryDisplayMode.Skip(1).Subscribe(_ => published++);
        var view = new GallerySettingsView();
        var window = new Window { DataContext = core, Content = view };

        try
        {
            window.Show();
            Dispatcher.UIThread.RunJobs();

            Assert.Same(view.FindControl<ComboBoxItem>(itemName),
                view.FindControl<ComboBox>("DisplayModeComboBox")!.SelectedItem);
            // Only the write-through writes the record and saves, and it runs once for each value the setting publishes
            Assert.Equal(0, published);
            // The save was not called. It ends at once here, which can leave the idle task in place even after a
            // save, so the count is what tells
            Assert.Equal(0, saves);
            Assert.Same(idle, core.GallerySettings.Saving);
            Assert.Equal(saved, core.GallerySettings.ExpandedGalleryDisplayMode.Value);
            Assert.Equal(saved, Settings.Gallery.ExpandedGalleryDisplayMode);
            Assert.Equal(rowsWasLastUsed, Settings.Gallery.RowsWasLastUsedDisplayMode);
        }
        finally
        {
            window.Close();
        }
    }

    [Fact]
    public void SettingsSearch_GalleryDisplayMode_IsSuggestedAndTaggedWithItsModes()
    {
        Assert.True(TranslationManager.LoadLanguage("en"));
        var data = new SettingsSearchData();

        var item = Assert.Single(SettingsSearchIndex.Build(data), x => x.Name == "Gallery display mode");

        // Picking the suggestion searches for its name, so the tags must hold the name too
        Assert.Equal(data.GalleryDisplayModeSearchTags, item.Tags);
        Assert.Contains("Gallery display mode", item.Tags);
        Assert.Contains("Columns", item.Tags);
        Assert.Contains("Masonry", item.Tags);
        Assert.Contains("Rows", item.Tags);
    }

    // The display mode saves through the delegate, so no test here writes the settings file
    private static CoreViewModel NewCore(Func<ValueTask<bool>>? save = null) =>
        new(null, _ => ValueTask.FromResult(new ImageModel()),
            saveGallerySettings: save ?? (static () => ValueTask.FromResult(true)));

    private static void WithGallerySettings(Action<CoreViewModel, GallerySettingsView> test)
    {
        var core = NewCore();
        core.GallerySettings.Start();
        var view = new GallerySettingsView();
        var window = new Window { DataContext = core, Content = view };
        window.Show();
        try
        {
            test(core, view);
        }
        finally
        {
            window.Close();
        }
    }

    private static ContextMenu MenuOf(GalleryView view, TabViewModel tab)
    {
        var menu = ((Panel)view.Content!).ContextMenu!;
        menu.DataContext = tab;
        return menu;
    }

    private static MenuItem Item(ItemsControl parent, string name) =>
        parent.Items.OfType<MenuItem>().Single(x => x.Name == name);

    // The gallery view loads its XAML only when the application's DataContext is a CoreViewModel
    private static void WithGalleryView(Action<CoreViewModel, TabViewModel, GalleryView> test)
    {
        var core = NewCore();
        core.GallerySettings.Start();
        var vm = new MainWindowViewModel(core, core.Translation, core.GlobalSettings!, core.GallerySettings);
        var tab = vm.WindowTabs.ActiveTab.Value;
        var application = Application.Current!;
        var previous = application.DataContext;
        application.DataContext = core;
        try
        {
            test(core, tab, new GalleryView { DataContext = tab });
        }
        finally
        {
            application.DataContext = previous;
        }
    }
}
