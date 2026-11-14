using PicView.Core.Config;
using PicView.Core.DebugTools;
using PicView.Core.Gallery;
using PicView.Core.Localization;
using R3;

namespace PicView.Core.ViewModels;

/// <summary>
/// The fourteen toggled settings publish their changes (ADR-0020): each is a bindable over its stored value, and the
/// write-through here is the one writer of that stored value, followed by one coalesced save.
/// </summary>
public class GlobalSettingsViewModel
{
    private bool _isExtraSettingsInitialized;
    private readonly Func<ValueTask<bool>> _save;
    private int _saveState; // 0 idle; 1 a save runs; 2 a save runs and a change landed since it began
    private Task _saving = Task.CompletedTask;

    public BindableReactiveProperty<bool> IsLooping { get; }

    public BindableReactiveProperty<bool> IsSideBySide { get; }

    public BindableReactiveProperty<bool> IsScrollingEnabled { get; }

    public BindableReactiveProperty<bool> IsZoomedToFit { get; }

    public BindableReactiveProperty<bool> IsIncludingSubdirectories { get; }

    public BindableReactiveProperty<bool> IsFileHistoryEnabled { get; }

    public BindableReactiveProperty<bool> IsShowingFullPathInTitleBar { get; }

    public BindableReactiveProperty<bool> IsShowingTaskbarProgress { get; }

    public BindableReactiveProperty<bool> IsOpeningInSameWindow { get; }

    public BindableReactiveProperty<bool> IsConstrainingBackgroundColor { get; }

    public BindableReactiveProperty<bool> IsTopMost { get; }

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

    private readonly BindableReactiveProperty<string?> _loopingText = new();
    private readonly BindableReactiveProperty<string?> _scrollingText = new();
    private readonly BindableReactiveProperty<string?> _fileHistoryText = new();
    private readonly BindableReactiveProperty<string?> _interfaceText = new();
    private readonly BindableReactiveProperty<string?> _bottomBarText = new();
    private readonly BindableReactiveProperty<string?> _hoverBarText = new();

    public IReadOnlyBindableReactiveProperty<string?> LoopingText => _loopingText;

    public IReadOnlyBindableReactiveProperty<string?> ScrollingText => _scrollingText;

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
    internal Task Saving => _saving;

    /// <param name="save">Saves the settings; null means <see cref="SettingsManager.SaveSettingsAsync"/>.</param>
    public GlobalSettingsViewModel(Func<ValueTask<bool>>? save = null)
    {
        _save = save ?? SaveSettingsAsync;

        IsLooping = new BindableReactiveProperty<bool>(Settings.UIProperties.Looping);
        IsSideBySide = new BindableReactiveProperty<bool>(Settings.ImageScaling.ShowImageSideBySide);
        IsScrollingEnabled = new BindableReactiveProperty<bool>(Settings.Zoom.ScrollEnabled);
        IsZoomedToFit = new BindableReactiveProperty<bool>(Settings.ImageScaling.ZoomToFit);
        IsIncludingSubdirectories = new BindableReactiveProperty<bool>(Settings.Sorting.IncludeSubDirectories);
        IsFileHistoryEnabled = new BindableReactiveProperty<bool>(Settings.Navigation.IsFileHistoryEnabled);
        IsShowingFullPathInTitleBar = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowFullPathInTitleBar);
        IsShowingTaskbarProgress = new BindableReactiveProperty<bool>(Settings.UIProperties.IsTaskbarProgressEnabled);
        IsOpeningInSameWindow = new BindableReactiveProperty<bool>(Settings.UIProperties.OpenInSameWindow);
        IsConstrainingBackgroundColor = new BindableReactiveProperty<bool>(Settings.UIProperties.IsConstrainBackgroundColorEnabled);
        IsTopMost = new BindableReactiveProperty<bool>(Settings.WindowProperties.TopMost);
        ShowInterface = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowInterface);
        ShowBottomBar = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowBottomNavBar);
        ShowHoverBar = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowHoverNavigationBar);
        ShowFadeButtons = new BindableReactiveProperty<bool>(Settings.UIProperties.ShowAltInterfaceButtons);
        // Built before the first window rewrites a negative stored margin, so it gives the same margin that window does
        WindowMargin = new BindableReactiveProperty<double>(Settings.WindowProperties.Margin < 0 ? NegativeMarginReplacement : Settings.WindowProperties.Margin);

        SyncConfigLocation(Settings.StartUp.ConfigLocation);

        Follow(IsLooping, value => Settings.UIProperties.Looping = value, rephrases: true);
        Follow(IsSideBySide, value => Settings.ImageScaling.ShowImageSideBySide = value);
        Follow(IsScrollingEnabled, value => Settings.Zoom.ScrollEnabled = value, rephrases: true);
        Follow(IsZoomedToFit, value => Settings.ImageScaling.ZoomToFit = value);
        Follow(IsIncludingSubdirectories, value => Settings.Sorting.IncludeSubDirectories = value);
        Follow(IsFileHistoryEnabled, value => Settings.Navigation.IsFileHistoryEnabled = value, rephrases: true);
        Follow(IsShowingFullPathInTitleBar, value => Settings.UIProperties.ShowFullPathInTitleBar = value);
        Follow(IsShowingTaskbarProgress, value => Settings.UIProperties.IsTaskbarProgressEnabled = value);
        Follow(IsOpeningInSameWindow, value => Settings.UIProperties.OpenInSameWindow = value);
        Follow(IsConstrainingBackgroundColor, value => Settings.UIProperties.IsConstrainBackgroundColorEnabled = value);
        Follow(IsTopMost, value => Settings.WindowProperties.TopMost = value);
        Follow(ShowInterface, value => Settings.UIProperties.ShowInterface = value, rephrases: true);
        Follow(ShowBottomBar, value => Settings.UIProperties.ShowBottomNavBar = value, rephrases: true);
        Follow(ShowHoverBar, value => Settings.UIProperties.ShowHoverNavigationBar = value, rephrases: true);
        Follow(ShowFadeButtons, value => Settings.UIProperties.ShowAltInterfaceButtons = value);
        Follow(WindowMargin, value => Settings.WindowProperties.Margin = value);
    }

    /// <summary>
    /// Writes the six menu texts from the current language and the settings they follow.
    /// </summary>
    public void Rephrase()
    {
        var t = TranslationManager.Translation;
        _loopingText.Value = IsLooping.Value ? t.LoopingEnabled : t.LoopingDisabled;
        _scrollingText.Value = IsScrollingEnabled.Value ? t.ScrollingEnabled : t.ScrollingDisabled;
        _fileHistoryText.Value = IsFileHistoryEnabled.Value ? t.FileHistoryEnabled : t.FileHistoryDisabled;
        _interfaceText.Value = ShowInterface.Value ? t.HideUI : t.ShowUI;
        _bottomBarText.Value = ShowBottomBar.Value ? t.HideBottomToolbar : t.ShowBottomToolbar;
        _hoverBarText.Value = ShowHoverBar.Value ? t.HideHoverNavigationBar : t.ShowHoverNavigationBar;
    }

    /// <summary>
    /// Extra settings, that are not reached through the main window.
    /// </summary>
    public void InitializeExtraSettings()
    {
        if (_isExtraSettingsInitialized)
        {
            return;
        }
        _isExtraSettingsInitialized = true;

        GalleryMouseWheelBehavior
            .Subscribe(x => {
                Settings.Gallery.GalleryMouseWheelBehavior = (GalleryMouseWheel)x;
            }, DebugHelper.LogError(nameof(GlobalSettingsViewModel), nameof(GalleryMouseWheelBehavior)));
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

    private void SaveSoon()
    {
        while (true)
        {
            if (Interlocked.CompareExchange(ref _saveState, 1, 0) is 0)
            {
                _saving = SaveUntilClean();
                return;
            }

            // A save runs: mark that a change landed (1 -> 2), or it is already marked. If the save ended between
            // the two checks the state is 0 again, so go round and start one
            if (Interlocked.CompareExchange(ref _saveState, 2, 1) is 1 or 2)
            {
                return;
            }
        }
    }

    private async Task SaveUntilClean()
    {
        while (true)
        {
            try
            {
                await _save().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                DebugHelper.LogDebug(nameof(GlobalSettingsViewModel), nameof(SaveUntilClean), e);
            }

            // Clean: a change after this starts its own save. Otherwise a change landed meanwhile, so save once more
            if (Interlocked.CompareExchange(ref _saveState, 0, 1) is 1)
            {
                return;
            }
            Interlocked.CompareExchange(ref _saveState, 1, 2);
        }
    }
}
