using R3;

namespace PicView.Core.Gallery;

/// <summary>
/// Changes one tab's Gallery Mode; nothing else does (ADR-0011). Crop and a Single Image hide the gallery without
/// changing its mode, and mode changes are refused while it is hidden. The settings only remember whether a new tab
/// starts docked, written when the user changes the tab's docked choice by hand.
/// </summary>
/// <param name="saveSettings">Saves the settings once the remembered choice changed; the settings file by default.</param>
public sealed class GalleryModeSwitcher(Func<ValueTask<bool>>? saveSettings = null) : IDisposable
{
    private readonly Func<ValueTask<bool>> _saveSettings = saveSettings ?? SaveSettingsAsync;
    private readonly ReactiveProperty<GalleryMode> _mode = new(GalleryMode.Closed);
    private readonly ReactiveProperty<bool> _isDocked = new(false);
    private readonly ReactiveProperty<bool> _isHidden = new(false);
    private readonly ReactiveProperty<GalleryMode> _shownMode = new(GalleryMode.Closed);
    private readonly Subject<GalleryModeChange> _changes = new();

    /// <summary>
    /// The mode Expanded gives back when it closes: Closed or Docked.
    /// </summary>
    private GalleryMode _giveBack = GalleryMode.Closed;

    /// <summary>
    /// The tab's mode, whether or not it is hidden.
    /// </summary>
    public ReadOnlyReactiveProperty<GalleryMode> Mode => _mode;

    /// <summary>
    /// Whether the tab's gallery is docked, or expanded and gives back docked.
    /// </summary>
    public ReadOnlyReactiveProperty<bool> IsDocked => _isDocked;

    /// <summary>
    /// Whether crop or a Single Image hides the gallery.
    /// </summary>
    public ReadOnlyReactiveProperty<bool> IsHidden => _isHidden;

    /// <summary>
    /// The mode the tab shows: Closed while hidden, else <see cref="Mode"/>.
    /// </summary>
    public ReadOnlyReactiveProperty<GalleryMode> ShownMode => _shownMode;

    /// <summary>
    /// Every change of <see cref="ShownMode"/>, with whether to animate it.
    /// </summary>
    public Observable<GalleryModeChange> Changes => _changes;

    /// <summary>
    /// Sets the mode a tab starts in, without animation and without writing the settings.
    /// </summary>
    public void Start(bool docked)
    {
        _giveBack = GalleryMode.Closed;
        SetMode(docked ? GalleryMode.Docked : GalleryMode.Closed, animate: false);
    }

    /// <summary>
    /// Hides or shows the gallery for crop or a Single Image, without animation and without changing the mode.
    /// </summary>
    public void SetHidden(bool hidden)
    {
        if (_isHidden.Value == hidden)
        {
            return;
        }

        _isHidden.Value = hidden;
        Publish(animate: false);
    }

    public void ToggleExpanded()
    {
        if (_isHidden.Value)
        {
            return;
        }

        if (_mode.Value is GalleryMode.Expanded)
        {
            SetMode(_giveBack, animate: true);
            return;
        }

        _giveBack = _mode.Value;
        SetMode(GalleryMode.Expanded, animate: true);
    }

    public ValueTask ToggleDocked()
    {
        if (_isHidden.Value)
        {
            return ValueTask.CompletedTask;
        }

        switch (_mode.Value)
        {
            case GalleryMode.Expanded:
                _giveBack = _giveBack is GalleryMode.Docked ? GalleryMode.Closed : GalleryMode.Docked;
                UpdateIsDocked();
                break;
            case GalleryMode.Docked:
                SetMode(GalleryMode.Closed, animate: true);
                break;
            default:
                SetMode(GalleryMode.Docked, animate: true);
                break;
        }

        return RememberDockedAsync();
    }

    /// <summary>
    /// Closes the Expanded Gallery to the mode it gives back, or closes a Docked Gallery.
    /// </summary>
    public ValueTask Contract()
    {
        if (_isHidden.Value)
        {
            return ValueTask.CompletedTask;
        }

        switch (_mode.Value)
        {
            case GalleryMode.Expanded:
                SetMode(_giveBack, animate: true);
                return ValueTask.CompletedTask;
            case GalleryMode.Docked:
                SetMode(GalleryMode.Closed, animate: true);
                return RememberDockedAsync();
            default:
                return ValueTask.CompletedTask;
        }
    }

    public ValueTask Close()
    {
        if (_isHidden.Value || _mode.Value is GalleryMode.Closed)
        {
            return ValueTask.CompletedTask;
        }

        _giveBack = GalleryMode.Closed;
        SetMode(GalleryMode.Closed, animate: true);
        return RememberDockedAsync();
    }

    /// <summary>
    /// Sets where every Docked Gallery sits, and docks this tab. While hidden, only the position changes.
    /// </summary>
    public async ValueTask DockAt(GalleryDockPosition position)
    {
        Settings.Gallery.DockPosition = position;
        if (!_isHidden.Value)
        {
            switch (_mode.Value)
            {
                case GalleryMode.Expanded:
                    _giveBack = GalleryMode.Docked;
                    UpdateIsDocked();
                    break;
                case GalleryMode.Closed:
                    SetMode(GalleryMode.Docked, animate: true);
                    break;
            }

            Settings.Gallery.IsGalleryDocked = _isDocked.Value;
        }

        await _saveSettings().ConfigureAwait(false);
    }

    private void SetMode(GalleryMode mode, bool animate)
    {
        _mode.Value = mode;
        UpdateIsDocked();
        Publish(animate);
    }

    private void UpdateIsDocked() =>
        _isDocked.Value = _mode.Value is GalleryMode.Docked ||
                          _mode.Value is GalleryMode.Expanded && _giveBack is GalleryMode.Docked;

    private void Publish(bool animate)
    {
        var shown = _isHidden.Value ? GalleryMode.Closed : _mode.Value;
        var from = _shownMode.Value;
        if (from == shown)
        {
            return;
        }

        _shownMode.Value = shown;
        _changes.OnNext(new GalleryModeChange(from, shown, animate));
    }

    private async ValueTask RememberDockedAsync()
    {
        if (Settings.Gallery.IsGalleryDocked == _isDocked.Value)
        {
            return;
        }

        Settings.Gallery.IsGalleryDocked = _isDocked.Value;
        await _saveSettings().ConfigureAwait(false);
    }

    public void Dispose()
    {
        _changes.Dispose();
        Disposable.Dispose(_mode, _isDocked, _isHidden, _shownMode);
    }
}
