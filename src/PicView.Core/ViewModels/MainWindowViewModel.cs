using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using R3;

namespace PicView.Core.ViewModels;

public class MainWindowViewModel : IDisposable
{
    #region Properties
    public CoreViewModel Core { get; }
    public IActionRunner? ActionRunner { get; set; }
    public FileHistoryViewModel? FileHistory { get; set; }
    public FavoritesViewModel? Favorites { get; set; }
    public TranslationViewModel Translation { get;  } 
    public GallerySharedSettingsViewModel GallerySettings { get; }
    public GlobalSettingsViewModel GlobalSettings { get; }
    public TopTitlebarViewModel TopTitlebarViewModel { get; }
    public TabOverviewViewModel WindowTabs { get; }
    public ToolTipViewModel? ToolTip { get; set; }
    public PrintPreviewViewModel? PrintPreview { get; set; }
    public ImageInfoWindowViewModel? InfoWindow { get; set; } 
    public ExifViewModel? Exif { get; set; }
    public ResizeImageViewModel? ResizeImageViewModel { get; set; }

    #region Window state
    public ClickedButton ClickedButton { get; set; }

    public IReadOnlyBindableReactiveProperty<bool> IsBottomToolbarShown { get; }
    
    public BindableReactiveProperty<double> ScrollViewerWidth { get; } = new(double.NaN);
    
    public BindableReactiveProperty<double> ScrollViewerHeight { get; } = new(double.NaN);

    public static int WindowMinWidth => SizeDefaults.WindowMinSize;
    public static int WindowMinHeight => SizeDefaults.WindowMinSize;

    public BindableReactiveProperty<double> WindowMaxWidth { get; } = new(double.NaN);

    public BindableReactiveProperty<double> WindowMaxHeight { get; } = new(double.NaN);

    /// <summary>
    /// The width to scale the image to
    /// </summary>
    public BindableReactiveProperty<double> ImageWidth { get; } = new(double.NaN);

    /// <summary>
    /// The height to scale the image to
    /// </summary>
    public BindableReactiveProperty<double> ImageHeight { get; } = new(double.NaN);

    public IReadOnlyBindableReactiveProperty<double> TitlebarHeight { get; }

    public IReadOnlyBindableReactiveProperty<double> BottombarHeight { get; }

    /// <summary>
    /// This window's Window Mode, which only its <see cref="WindowModeSwitcher"/> changes.
    /// </summary>
    public ReactiveProperty<WindowModeState> WindowModeState { get; } =
        new(Sizing.WindowModeState.Opening(Settings.WindowProperties.AutoFit));

    public IWindowModeSwitcher? WindowModeSwitcher { get; set; }

    public IReadOnlyBindableReactiveProperty<bool> IsAutoFit { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsFullscreen { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsMaximized { get; }

    public IReadOnlyBindableReactiveProperty<bool> ShouldRestoreBeShown { get; }

    public IReadOnlyBindableReactiveProperty<bool> ShouldMaximizeBeShown { get; }

    /// <summary>
    /// The text of the menu item that docks or closes the active tab's Docked Gallery.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<string?> DockedGalleryToggleHeader { get; }

    /// <summary>
    /// Whether this window shows its loading indicator: while its active tab is Busy, or while the tab says Loading or
    /// Downloading and the titlebar is hidden, since the titlebar already says Loading (ADR-0025). Derived; nothing
    /// sets it.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<bool> IsLoadingIndicatorShown { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsUIShown { get; }

    /// <summary>
    /// <see cref="IsUIShown"/> under the name its bindings use.
    /// </summary>
    public IReadOnlyBindableReactiveProperty<bool> IsTopToolbarShown => IsUIShown;

    /// <summary>
    /// Which bars this window shows, their heights and whether the hover bar may show, derived from the three
    /// interface settings and its Window Mode (ADR-0021).
    /// </summary>
    public IReadOnlyBindableReactiveProperty<InterfaceVisibility> Interface { get; }

    public IReadOnlyBindableReactiveProperty<bool> IsHoverBarShown { get; }

    public BindableReactiveProperty<bool> IsEditableTitlebarOpen { get; } = new(false);

    #endregion
    
    #endregion

    #region Commands

    /// <summary>
    /// Runs the Action its parameter names, one of <see cref="Keybindings.Actions"/>.
    /// </summary>
    public ReactiveCommand<string> RunActionCommand { get; }
    private async ValueTask RunAction(string action, CancellationToken cancellationToken) { if (ActionRunner is null) return; await ActionRunner.Run(action).ConfigureAwait(false); }

    #endregion

    /// <param name="firstTab">A tab moved out of another Main Window, for a window that opens with it.</param>
    public MainWindowViewModel(CoreViewModel core, TranslationViewModel translations, GlobalSettingsViewModel globalSettings, GallerySharedSettingsViewModel gallerySettings, TabViewModel? firstTab = null)
    {
        Core = core;
        Translation = translations;
        GlobalSettings = globalSettings;
        TopTitlebarViewModel = new(globalSettings);
        GallerySettings = gallerySettings;
        WindowTabs = new TabOverviewViewModel(this, firstTab);

        // Window Mode
        IsAutoFit = WindowModeState.Select(static state => state.AutoFit).ToReadOnlyBindableReactiveProperty();
        var mode = WindowModeState.Select(static state => state.Mode);
        IsFullscreen = mode.Select(static m => m is WindowMode.Fullscreen).ToReadOnlyBindableReactiveProperty();
        IsMaximized = mode.Select(static m => m is WindowMode.Maximized).ToReadOnlyBindableReactiveProperty();
        ShouldRestoreBeShown = mode.Select(static m => m is WindowMode.Fullscreen or WindowMode.Maximized)
            .ToReadOnlyBindableReactiveProperty();
        ShouldMaximizeBeShown = mode.Select(static m => m is not WindowMode.Maximized)
            .ToReadOnlyBindableReactiveProperty();

        // Interface Visibility: the settings publish their value (ADR-0020); without them, the stored record is read once
        Observable<InterfaceVisibility> visibility = globalSettings is null
            ? WindowModeState.Select(static state => InterfaceVisibility.For(Settings.UIProperties.ShowInterface,
                Settings.UIProperties.ShowBottomNavBar, Settings.UIProperties.ShowHoverNavigationBar, state.Shown))
            : Observable.CombineLatest(globalSettings.ShowInterface, globalSettings.ShowBottomBar,
                globalSettings.ShowHoverBar, WindowModeState,
                static (showInterface, showBottomBar, showHoverBar, state) =>
                    InterfaceVisibility.For(showInterface, showBottomBar, showHoverBar, state.Shown));
        Interface = visibility.ToReadOnlyBindableReactiveProperty();
        IsHoverBarShown = Interface.AsObservable().Select(static v => v.ShowsHoverBar).ToReadOnlyBindableReactiveProperty();
        IsUIShown = Interface.AsObservable().Select(static v => v.IsInterfaceShown).ToReadOnlyBindableReactiveProperty();
        IsLoadingIndicatorShown = WindowTabs.ActiveTab
            .Select(static tab => tab.IsBusy.AsObservable()
                .CombineLatest(tab.Titles.SaysLoading, static (busy, loading) => (busy, loading)))
            .Switch()
            .CombineLatest(IsTopToolbarShown.AsObservable(),
                static (tab, titlebarShown) => tab.busy || (tab.loading && !titlebarShown))
            .DistinctUntilChanged()
            .ToReadOnlyBindableReactiveProperty();
        IsBottomToolbarShown = Interface.AsObservable().Select(static v => v.IsBottomBarShown).ToReadOnlyBindableReactiveProperty();
        TitlebarHeight = Interface.AsObservable().Select(static v => v.TitlebarHeight).ToReadOnlyBindableReactiveProperty();
        BottombarHeight = Interface.AsObservable().Select(static v => v.BottombarHeight).ToReadOnlyBindableReactiveProperty();

        // Each tab has its own gallery mode, and every window shares one TranslationViewModel, so the text is per window
        var isDocked = WindowTabs.ActiveTab.Select(static tab => tab.Gallery.Switcher.IsDocked.AsObservable()).Switch();
        DockedGalleryToggleHeader = (translations is null
                ? isDocked.Select(static string? (_) => null)
                : isDocked.CombineLatest(translations.HideDockedGallery, translations.ShowDockedGallery,
                    static (docked, hide, show) => docked ? hide : show))
            .ToReadOnlyBindableReactiveProperty(StringComparer.Ordinal);

        RunActionCommand = new ReactiveCommand<string>(RunAction);
    }

    public void Dispose()
    {
        Disposable.Dispose(
            ResizeImageViewModel,
            ScrollViewerWidth,
            ScrollViewerHeight,
            WindowMaxWidth,
            WindowMaxHeight,
            ImageWidth,
            ImageHeight,
            TitlebarHeight,
            BottombarHeight,
            WindowModeState,
            IsFullscreen,
            IsMaximized,
            ShouldRestoreBeShown,
            ShouldMaximizeBeShown,
            DockedGalleryToggleHeader,
            IsLoadingIndicatorShown,
            IsUIShown,
            IsEditableTitlebarOpen,
            RunActionCommand,
            IsBottomToolbarShown,
            IsAutoFit,
            IsFullscreen,
            Interface,
            IsHoverBarShown,
            TopTitlebarViewModel.DropDownMenu
        );
        GC.SuppressFinalize(this);
    }
}
