using PicView.Core.IPlatform;
using PicView.Core.Sizing;
using R3;

namespace PicView.Core.ViewModels;

public class MainWindowViewModel : IDisposable
{
    #region Properties
    public CoreViewModel Core { get; }
    public IActionRunner? ActionRunner { get; set; }
    public IPlatformWindowService? PlatformWindowService { get; }
    public FileHistoryViewModel? FileHistory { get; set; }
    public FavoritesViewModel? Favorites { get; set; }
    public TranslationViewModel Translation { get;  } 
    public GallerySharedSettingsViewModel GallerySettings { get; }
    public GlobalSettingsViewModel GlobalSettings { get; }
    public TopTitlebarViewModel TopTitlebarViewModel { get; }  = new();
    public TabOverviewViewModel WindowTabs { get; }
    public ToolTipViewModel? ToolTip { get; set; }
    public PrintPreviewViewModel? PrintPreview { get; set; }
    public ImageInfoWindowViewModel? InfoWindow { get; set; } 
    public ExifViewModel? Exif { get; set; }
    public ResizeImageViewModel? ResizeImageViewModel { get; set; }

    #region Window state
    public bool IsNavigationButtonLeftClicked { get; set; }
    public bool IsNavigationButtonRightClicked { get; set; }
    
    public bool IsClickArrowLeftClicked { get; set; }
    public bool IsClickArrowRightClicked { get; set; }

    public bool IsBottomToolbarRightRotationClicked { get; set; }
    public bool IsBottomToolbarLeftRotationClicked { get; set; }
    // Hidden with the interface, as the window applies its settings, so a Fit before that already leaves it out
    public BindableReactiveProperty<bool> IsBottomToolbarShown { get; } =
        new(Settings.UIProperties.ShowBottomNavBar && Settings.UIProperties.ShowInterface);
    public BindableReactiveProperty<bool> IsSideBySide { get; } = new(Settings.ImageScaling.ShowImageSideBySide);
    
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

    public BindableReactiveProperty<double> TitlebarHeight { get; } = new();

    public BindableReactiveProperty<double> BottombarHeight { get; } = new();

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

    public BindableReactiveProperty<bool> IsLoadingIndicatorShown { get; } = new();

    public BindableReactiveProperty<bool> IsUIShown { get; } = new();
    public BindableReactiveProperty<bool> IsTopToolbarShown { get; } = new(Settings.UIProperties.ShowInterface);

    public BindableReactiveProperty<bool> IsEditableTitlebarOpen { get; } = new(false);
    
    public BindableReactiveProperty<bool> IsScrollingEnabled { get; } = new(Settings.Zoom.ScrollEnabled);

    public BindableReactiveProperty<bool> IsZoomedToFit { get; } = new(Settings.ImageScaling.ZoomToFit);
    
    public BindableReactiveProperty<bool> IsTopMost { get; } = new(Settings.WindowProperties.TopMost);
    
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
    public MainWindowViewModel(CoreViewModel core, TranslationViewModel translations, IPlatformWindowService windowService, GlobalSettingsViewModel globalSettings, GallerySharedSettingsViewModel gallerySettings, TabViewModel? firstTab = null)
    {
        Core = core;
        Translation = translations;
        PlatformWindowService = windowService;
        GlobalSettings = globalSettings;
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

        // Each tab has its own gallery mode, and every window shares one TranslationViewModel, so the text is per window
        var isDocked = WindowTabs.ActiveTab.Select(static tab => tab.Gallery.Switcher.IsDocked.AsObservable()).Switch();
        DockedGalleryToggleHeader = (translations is null
                ? isDocked.Select(static _ => (string?)null)
                : Observable.CombineLatest(isDocked, translations.HideDockedGallery, translations.ShowDockedGallery,
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
            IsTopToolbarShown,
            IsEditableTitlebarOpen,
            RunActionCommand,
            IsBottomToolbarShown,
            IsAutoFit,
            IsSideBySide,
            IsFullscreen,
            IsScrollingEnabled,
            IsZoomedToFit
        );
        GC.SuppressFinalize(this);
    }
}
