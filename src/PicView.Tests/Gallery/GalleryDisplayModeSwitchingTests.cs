using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using PicView.Avalonia.CustomControls;
using PicView.Avalonia.Views.Gallery;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using PicView.Core.Models;
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
        var core = new CoreViewModel(null, _ => ValueTask.FromResult(new ImageModel()));
        core.GallerySettings.Initialize();
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
