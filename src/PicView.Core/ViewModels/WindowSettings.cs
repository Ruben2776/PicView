using PicView.Core.Config;
using PicView.Core.FileSorting;
using PicView.Core.Sizing;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// The settings one Main Window owns: seeded from the settings record when the window opens, and written back to it
/// only through <see cref="Store"/>.
/// </summary>
public sealed class WindowSettings : IDisposable
{
    /// <summary>
    /// The nine image, interface and background values, read from the record or copied from another window.
    /// </summary>
    private readonly record struct Display(
        bool IsSideBySide,
        bool IsLooping,
        bool IsZoomedToFit,
        bool IsScrollingEnabled,
        bool ShowInterface,
        bool ShowBottomBar,
        bool ShowHoverBar,
        int BackgroundChoice,
        bool IsConstrainingBackgroundColor);

    private readonly TranslationViewModel? _translations;

    private WindowSettings(bool isTopMost, bool isStayingCentered, bool isZoomResizingWindow, RememberedWindowMode startingMode, NormalBounds bounds, FileOrder fileOrder,
        Display display, WindowGallerySettings gallery, TranslationViewModel? translations)
    {
        _translations = translations;
        Gallery = gallery;
        IsSideBySide = new BindableReactiveProperty<bool>(display.IsSideBySide);
        IsLooping = new BindableReactiveProperty<bool>(display.IsLooping);
        IsZoomedToFit = new BindableReactiveProperty<bool>(display.IsZoomedToFit);
        IsScrollingEnabled = new BindableReactiveProperty<bool>(display.IsScrollingEnabled);
        ShowInterface = new BindableReactiveProperty<bool>(display.ShowInterface);
        ShowBottomBar = new BindableReactiveProperty<bool>(display.ShowBottomBar);
        ShowHoverBar = new BindableReactiveProperty<bool>(display.ShowHoverBar);
        BackgroundChoice = new BindableReactiveProperty<int>(display.BackgroundChoice);
        IsConstrainingBackgroundColor = new BindableReactiveProperty<bool>(display.IsConstrainingBackgroundColor);
        ImageBackground = new BindableReactiveProperty<object?>();
        ConstrainedImageBackground = new BindableReactiveProperty<object?>();

        // Like MainWindowViewModel.DockedGalleryToggleHeader: each text follows its setting and the language, with no
        // subscription of its own to LanguageChanged
        LoopingText = TextOf(IsLooping, translations?.LoopingEnabled, translations?.LoopingDisabled);
        ScrollingText = TextOf(IsScrollingEnabled, translations?.ScrollEnabled, translations?.ScrollDisabled);
        InterfaceText = TextOf(ShowInterface, translations?.HideUI, translations?.ShowUI);
        BottomBarText = TextOf(ShowBottomBar, translations?.HideBottomToolbar, translations?.ShowBottomToolbar);
        HoverBarText = TextOf(ShowHoverBar, translations?.HideHoverNavigationBar, translations?.ShowHoverNavigationBar);

        FileOrder = new BindableReactiveProperty<FileOrder>(fileOrder);
        IsTopMost = new BindableReactiveProperty<bool>(isTopMost);
        IsStayingCentered = new BindableReactiveProperty<bool>(isStayingCentered);
        IsZoomResizingWindow = new BindableReactiveProperty<bool>(isZoomResizingWindow);
        StartingMode = startingMode;
        Bounds = bounds;
    }

    public BindableReactiveProperty<bool> IsTopMost { get; }

    /// <summary>
    /// This window's gallery look, dock edge and start-docked choice.
    /// </summary>
    public WindowGallerySettings Gallery { get; }

    /// <summary>
    /// Whether the window closed and disposed these settings: reading still works, and nothing may subscribe to them.
    /// </summary>
    public bool IsDisposed => _isDisposed;

    // Read by a tab's follower on any thread
    private volatile bool _isDisposed;

    public BindableReactiveProperty<bool> IsSideBySide { get; }

    public BindableReactiveProperty<bool> IsLooping { get; }

    public BindableReactiveProperty<bool> IsZoomedToFit { get; }

    public BindableReactiveProperty<bool> IsScrollingEnabled { get; }

    public BindableReactiveProperty<bool> ShowInterface { get; }

    public BindableReactiveProperty<bool> ShowBottomBar { get; }

    public BindableReactiveProperty<bool> ShowHoverBar { get; }

    public BindableReactiveProperty<int> BackgroundChoice { get; }

    public BindableReactiveProperty<bool> IsConstrainingBackgroundColor { get; }

    /// <summary>
    /// The brush this window's viewers paint with; set by the Avalonia side on the UI thread.
    /// </summary>
    public BindableReactiveProperty<object?> ImageBackground { get; }

    /// <summary>
    /// The brush behind the image when the background is constrained to it; set by the Avalonia side on the UI thread.
    /// </summary>
    public BindableReactiveProperty<object?> ConstrainedImageBackground { get; }

    public IReadOnlyBindableReactiveProperty<string?> LoopingText { get; }

    public IReadOnlyBindableReactiveProperty<string?> ScrollingText { get; }

    public IReadOnlyBindableReactiveProperty<string?> InterfaceText { get; }

    public IReadOnlyBindableReactiveProperty<string?> BottomBarText { get; }

    public IReadOnlyBindableReactiveProperty<string?> HoverBarText { get; }

    public BindableReactiveProperty<bool> IsStayingCentered { get; }

    /// <summary>
    /// Zoom Resizes Window: whether this window, with AutoFit, takes the size of the zoomed image (ADR-0047).
    /// </summary>
    public BindableReactiveProperty<bool> IsZoomResizingWindow { get; }

    /// <summary>
    /// The order this window's tabs list their files in.
    /// </summary>
    public BindableReactiveProperty<FileOrder> FileOrder { get; }

    /// <summary>
    /// The mode the window starts in. Never changes; the live mode is the window's WindowModeState.
    /// </summary>
    public RememberedWindowMode StartingMode { get; }

    /// <summary>
    /// The window's last size and position in Normal mode.
    /// </summary>
    public NormalBounds Bounds { get; set; }

    private static IReadOnlyBindableReactiveProperty<string?> TextOf(BindableReactiveProperty<bool> setting,
        BindableReactiveProperty<string?>? whenOn, BindableReactiveProperty<string?>? whenOff) =>
        (whenOn is null || whenOff is null
            ? setting.Select(static string? (_) => null)
            : setting.CombineLatest(whenOn, whenOff, static (on, onText, offText) => on ? onText : offText))
        .ToReadOnlyBindableReactiveProperty(StringComparer.Ordinal);

    public static WindowSettings FromRecord(AppSettings? record, TranslationViewModel? translations = null)
    {
        var properties = record?.WindowProperties ?? new WindowProperties();
        var ui = record?.UIProperties ?? new UIProperties();
        var scaling = record?.ImageScaling ?? new ImageScaling();
        var zoom = record?.Zoom ?? new Zoom();
        return new WindowSettings(
            properties.TopMost,
            properties.KeepCentered,
            properties.ZoomResizesWindow,
            new RememberedWindowMode(properties.Fullscreen, properties.Maximized, properties.AutoFit),
            new NormalBounds(properties.Left, properties.Top, properties.Width, properties.Height),
            FileSorting.FileOrder.FromRecord(record?.Sorting),
            new Display(scaling.ShowImageSideBySide, ui.Looping, scaling.ZoomToFit, zoom.ScrollEnabled, ui.ShowInterface,
                ui.ShowBottomNavBar, ui.ShowHoverNavigationBar, ui.BgColorChoice, ui.IsConstrainBackgroundColorEnabled),
            WindowGallerySettings.FromRecord(record?.Gallery), translations);
    }

    /// <summary>
    /// The Side-by-Side a tab with no window follows: the record's, or off without one.
    /// </summary>
    public static bool IsSideBySideWithoutWindow(AppSettings? record) =>
        record?.ImageScaling?.ShowImageSideBySide ?? false;

    /// <summary>
    /// The looping a tab with no window follows: the record's, or off without one.
    /// </summary>
    public static bool IsLoopingWithoutWindow(AppSettings? record) => record?.UIProperties?.Looping ?? false;

    /// <summary>
    /// The settings of a window opened from this one's window, whose remembered mode is <paramref name="openerMode"/>.
    /// </summary>
    public WindowSettings CopyFor(RememberedWindowMode openerMode) =>
        new(IsTopMost.Value, IsStayingCentered.Value, IsZoomResizingWindow.Value, openerMode, Bounds, FileOrder.Value,
            new Display(IsSideBySide.Value, IsLooping.Value, IsZoomedToFit.Value, IsScrollingEnabled.Value,
                ShowInterface.Value, ShowBottomBar.Value, ShowHoverBar.Value, BackgroundChoice.Value,
                IsConstrainingBackgroundColor.Value),
            Gallery.Copy(), _translations);

    public void Store(AppSettings record, RememberedWindowMode remembered)
    {
        var properties = record.WindowProperties ??= new WindowProperties();
        properties.Fullscreen = remembered.Fullscreen;
        properties.Maximized = remembered.Maximized;
        properties.AutoFit = remembered.AutoFit;
        properties.TopMost = IsTopMost.Value;
        properties.KeepCentered = IsStayingCentered.Value;
        properties.ZoomResizesWindow = IsZoomResizingWindow.Value;
        properties.Left = Bounds.Left;
        properties.Top = Bounds.Top;
        properties.Width = Bounds.Width;
        properties.Height = Bounds.Height;
        FileOrder.Value.StoreTo(record.Sorting ??= new Sorting());

        // Only the nine values: the rest of these groups are app-wide settings
        var ui = record.UIProperties ??= new UIProperties();
        ui.Looping = IsLooping.Value;
        ui.ShowInterface = ShowInterface.Value;
        ui.ShowBottomNavBar = ShowBottomBar.Value;
        ui.ShowHoverNavigationBar = ShowHoverBar.Value;
        ui.BgColorChoice = BackgroundChoice.Value;
        ui.IsConstrainBackgroundColorEnabled = IsConstrainingBackgroundColor.Value;
        var scaling = record.ImageScaling ??= new ImageScaling();
        scaling.ShowImageSideBySide = IsSideBySide.Value;
        scaling.ZoomToFit = IsZoomedToFit.Value;
        (record.Zoom ??= new Zoom()).ScrollEnabled = IsScrollingEnabled.Value;
        Gallery.StoreTo(record.Gallery ??= new Config.Gallery());
    }

    /// <summary>
    /// Forwards the values of the observable <paramref name="pick"/> gives for these settings, as it publishes them.
    /// It never errors, and it completes only for a window that has closed and disposed them: then it yields nothing,
    /// also when the window closes between the check and the subscribe.
    /// </summary>
    /// <remarks>
    /// A follower of the window a tab or page is following puts this inside a <c>Switch</c>: the Switch stays open
    /// when a closed window's settings give nothing, so the follower goes silent until it follows another window.
    /// </remarks>
    public Observable<T> Follow<T>(Func<WindowSettings, Observable<T>> pick)
    {
        if (IsDisposed)
        {
            return Observable.Empty<T>();
        }

        return Observable.Create<T, (WindowSettings Settings, Func<WindowSettings, Observable<T>> Pick)>(
            (this, pick),
            static (observer, state) =>
            {
                try
                {
                    return state.Pick(state.Settings).Subscribe(observer.OnNext);
                }
                catch (ObjectDisposedException)
                {
                    observer.OnCompleted();
                    return Disposable.Empty;
                }
            });
    }

    public void Dispose()
    {
        _isDisposed = true;
        Gallery.Dispose();
        IsTopMost.Dispose();
        IsStayingCentered.Dispose();
        IsZoomResizingWindow.Dispose();
        FileOrder.Dispose();
        IsSideBySide.Dispose();
        IsLooping.Dispose();
        IsZoomedToFit.Dispose();
        IsScrollingEnabled.Dispose();
        ShowInterface.Dispose();
        ShowBottomBar.Dispose();
        ShowHoverBar.Dispose();
        BackgroundChoice.Dispose();
        IsConstrainingBackgroundColor.Dispose();
        ImageBackground.Dispose();
        ConstrainedImageBackground.Dispose();
        LoopingText.Dispose();
        ScrollingText.Dispose();
        InterfaceText.Dispose();
        BottomBarText.Dispose();
        HoverBarText.Dispose();
    }
}
