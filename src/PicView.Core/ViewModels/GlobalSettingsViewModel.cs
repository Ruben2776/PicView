using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// The nine toggled settings publish their changes (ADR-0020): each is a bindable over its stored value, and the
/// write-through here is the one writer of that stored value, followed by one coalesced save. The constructor
/// subscribes nothing: the write-throughs start in two groups, <see cref="InitializeActionSettings"/> and
/// <see cref="InitializeExtraSettings"/>, so a change before them writes and saves nothing.
/// </summary>
public class GlobalSettingsViewModel
{
    private readonly Lock _initializeLock = new();
    private bool _isActionSettingsInitialized;
    private bool _isExtraSettingsInitialized;
    private readonly CoalescedSave _coalescedSave;

    public BindableReactiveProperty<bool> IsIncludingSubdirectories { get; }

    public BindableReactiveProperty<bool> IsFileHistoryEnabled { get; }

    public BindableReactiveProperty<bool> IsShowingFullPathInTitleBar { get; }

    public BindableReactiveProperty<bool> IsShowingTaskbarProgress { get; }

    public BindableReactiveProperty<bool> IsOpeningInSameWindow { get; }

    public BindableReactiveProperty<bool> IsConstrainingBackgroundColor { get; }

    public BindableReactiveProperty<bool> ShowInterface { get; }

    public BindableReactiveProperty<bool> ShowBottomBar { get; }

    public BindableReactiveProperty<bool> ShowHoverBar { get; }

    /// <summary>
    /// Whether the alt buttons and the hover bar fade in on hover; a hover bar that cannot fade in takes no clicks (ADR-0021).
    /// </summary>
    public BindableReactiveProperty<bool> ShowFadeButtons { get; }

    /// <summary>
    /// The Screen Margin: the total gap AutoFit keeps free around the window; every Fit reads it.
    /// </summary>
    public BindableReactiveProperty<double> WindowMargin { get; }

    /// <summary>
    /// The margin a negative stored Screen Margin is replaced with, as the first window does before it is sized.
    /// </summary>
    public const double NegativeMarginReplacement = 45;

    private readonly BindableReactiveProperty<string?> _fileHistoryText = new();
    private readonly BindableReactiveProperty<string?> _interfaceText = new();
    private readonly BindableReactiveProperty<string?> _bottomBarText = new();
    private readonly BindableReactiveProperty<string?> _hoverBarText = new();

    public IReadOnlyBindableReactiveProperty<string?> FileHistoryText => _fileHistoryText;

    public IReadOnlyBindableReactiveProperty<string?> InterfaceText => _interfaceText;

    public IReadOnlyBindableReactiveProperty<string?> BottomBarText => _bottomBarText;

    public IReadOnlyBindableReactiveProperty<string?> HoverBarText => _hoverBarText;

    public BindableReactiveProperty<bool> ShowSetAsWallpaper { get; } = new(Settings.UIProperties.ShowSetAsWallpaper);

    public BindableReactiveProperty<object?> ImageBackground { get; } = new();

    public BindableReactiveProperty<object?> ConstrainedImageBackground { get; } = new();

    public BindableReactiveProperty<int> BackgroundChoice { get; } = new();

    public BindableReactiveProperty<int> GalleryMouseWheelBehavior { get; } = new((int)Settings.Gallery.GalleryMouseWheelBehavior);

    // Set by hand, not through a converter (macOS NativeMenu cannot bind a converted radio state)
    public BindableReactiveProperty<bool> IsConfigAuto { get; } = new(false);

    public BindableReactiveProperty<bool> IsConfigPortable { get; } = new(false);

    public BindableReactiveProperty<bool> IsConfigRoaming { get; } = new(false);

    /// <summary>macOS always uses the roaming folder, so the mode items are hidden there.</summary>
    public bool CanChooseConfigLocation { get; } = !OperatingSystem.IsMacOS();

    /// <summary>
    /// Checks exactly one of the three mode flags, and re-announces all three, so a radio the user clicked
    /// snaps back even when the stored mode did not change.
    /// </summary>
    public void SyncConfigLocation(ConfigLocation mode)
    {
        IsConfigAuto.Value = mode == ConfigLocation.Auto;
        IsConfigPortable.Value = mode == ConfigLocation.Portable;
        IsConfigRoaming.Value = mode == ConfigLocation.Roaming;
        IsConfigAuto.ForceNotify();
        IsConfigPortable.ForceNotify();
        IsConfigRoaming.ForceNotify();
    }

    /// <summary>The save loop in flight, completed when idle. For tests.</summary>
    internal Task Saving => _coalescedSave.Saving;

    /// <param name="save">Saves the settings; null means <see cref="SettingsManager.SaveSettingsAsync"/>.</param>
    public GlobalSettingsViewModel(Func<ValueTask<bool>>? save = null)
    {
        _coalescedSave = new CoalescedSave(save ?? SaveSettingsAsync, nameof(GlobalSettingsViewModel));

        IsIncludingSubdirectories = new BindableReactiveProperty<bool>(Settings.Sorting.IncludeSubDirectories);
        IsFileHistoryEnabled = new BindableReactiveProperty<bool>(Settings.Navigation.IsFileHistoryEnabled);
        IsShowingFullPathInTitleBar = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowFullPathInTitleBar);
        IsShowingTaskbarProgress = new BindableReactiveProperty<bool>(Settings.UIProperties.IsTaskbarProgressEnabled);
        IsOpeningInSameWindow = new BindableReactiveProperty<bool>(Settings.UIProperties.OpenInSameWindow);
        IsConstrainingBackgroundColor = new BindableReactiveProperty<bool>(Settings.UIProperties.IsConstrainBackgroundColorEnabled);
        ShowInterface = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowInterface);
        ShowBottomBar = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowBottomNavBar);
        ShowHoverBar = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowHoverNavigationBar);
        ShowFadeButtons = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowAltInterfaceButtons);
        // Built before the first window rewrites a negative stored margin, so it gives the same margin that window does
        WindowMargin = new BindableReactiveProperty<double>(Settings.WindowProperties.Margin < 0 ? NegativeMarginReplacement : Settings.WindowProperties.Margin);

        SyncConfigLocation(Settings.StartUp.ConfigLocation);
    }

    /// <summary>
    /// Starts the write-throughs of the nine settings the Actions toggle. Idempotent and safe from any thread;
    /// a concurrent caller returns only after the subscriptions exist.
    /// </summary>
    public void InitializeActionSettings()
    {
        lock (_initializeLock)
        {
            if (_isActionSettingsInitialized)
            {
                return;
            }

            Follow(IsIncludingSubdirectories, value => Settings.Sorting.IncludeSubDirectories = value);
            Follow(IsFileHistoryEnabled, value => Settings.Navigation.IsFileHistoryEnabled = value, rephrases: true);
            Follow(IsShowingFullPathInTitleBar, value => Settings.UIProperties.ShowFullPathInTitleBar = value);
            Follow(IsShowingTaskbarProgress, value => Settings.UIProperties.IsTaskbarProgressEnabled = value);
            Follow(IsOpeningInSameWindow, value => Settings.UIProperties.OpenInSameWindow = value);
            Follow(IsConstrainingBackgroundColor, value => Settings.UIProperties.IsConstrainBackgroundColorEnabled = value);
            Follow(ShowInterface, value => Settings.UIProperties.ShowInterface = value, rephrases: true);
            Follow(ShowBottomBar, value => Settings.UIProperties.ShowBottomNavBar = value, rephrases: true);
            Follow(ShowHoverBar, value => Settings.UIProperties.ShowHoverNavigationBar = value, rephrases: true);
            _isActionSettingsInitialized = true;
        }
    }

    /// <summary>
    /// Rewrites the four menu texts whenever the language changes.
    /// </summary>
    public void FollowLanguage(Observable<Unit> languageChanged)
    {
        languageChanged.Subscribe(_ => Rephrase(),
            DebugHelper.LogError(nameof(GlobalSettingsViewModel), nameof(FollowLanguage)));
    }

    /// <summary>
    /// Writes the four menu texts from the current language and the settings they follow.
    /// </summary>
    public void Rephrase()
    {
        var t = TranslationManager.Translation;
        _fileHistoryText.Value = IsFileHistoryEnabled.Value ? t.FileHistoryEnabled : t.FileHistoryDisabled;
        _interfaceText.Value = ShowInterface.Value ? t.HideUI : t.ShowUI;
        _bottomBarText.Value = ShowBottomBar.Value ? t.HideBottomToolbar : t.ShowBottomToolbar;
        _hoverBarText.Value = ShowHoverBar.Value ? t.HideHoverNavigationBar : t.ShowHoverNavigationBar;
    }

    /// <summary>
    /// Starts the write-throughs of the extra settings, that are not reached through the main window. Idempotent and
    /// safe from any thread; a concurrent caller returns only after the subscriptions exist.
    /// </summary>
    public void InitializeExtraSettings()
    {
        lock (_initializeLock)
        {
            if (_isExtraSettingsInitialized)
            {
                return;
            }

            Follow(ShowFadeButtons, value => Settings.UIProperties.ShowAltInterfaceButtons = value);
            Follow(WindowMargin, value => Settings.WindowProperties.Margin = value);
            GalleryMouseWheelBehavior
                .Subscribe(x => {
                    Settings.Gallery.GalleryMouseWheelBehavior = (GalleryMouseWheel)x;
                }, DebugHelper.LogError(nameof(GlobalSettingsViewModel), nameof(GalleryMouseWheelBehavior)));
            _isExtraSettingsInitialized = true;
        }
    }

    private void Follow<T>(BindableReactiveProperty<T> setting, Action<T> write, bool rephrases = false)
    {
        setting.Skip(1).Subscribe(value =>
        {
            write(value);
            if (rephrases)
            {
                Rephrase();
            }
            SaveSoon();
        }, DebugHelper.LogError(nameof(GlobalSettingsViewModel), nameof(Follow)));
    }

    internal void SaveSoon() => _coalescedSave.Soon();
}
