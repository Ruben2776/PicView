using PicView.Core.ViewModels;
using R3;

namespace PicView.Core.Gallery;

/// <summary>
/// Changes one tab's Gallery Mode; nothing else does (ADR-0011). Crop, a Single Image and a file list at or over
/// <see cref="Config.Gallery.HideGalleryAtFileCount"/> hide the gallery without changing its mode, and mode changes
/// are refused while it is hidden. What it remembers goes to the Main Window the tab is in at that moment: a dock or
/// undock by hand sets whether a new tab of that window starts docked, and docking at an edge sets the edge every
/// Docked Gallery of that window sits at. It writes no settings record and saves nothing; the window stores its
/// settings when it closes. A switcher with no window, as a test makes, remembers nothing.
/// </summary>
public sealed class GalleryModeSwitcher : IDisposable
{
    private readonly ReactiveProperty<GalleryMode> _mode = new(GalleryMode.Closed);
    private readonly ReactiveProperty<bool> _isDocked = new(false);
    private readonly ReactiveProperty<bool> _isHidden = new(false);
    private readonly ReactiveProperty<GalleryMode> _shownMode = new(GalleryMode.Closed);
    private readonly Subject<GalleryModeChange> _changes = new();

    /// <summary>
    /// The gallery settings of the window the tab is in now, asked at each write and at <see cref="Start()"/>; null
    /// until <see cref="RemembersIn"/>, and its result null for a tab with no window.
    /// </summary>
    private Func<WindowGallerySettings?>? _currentWindow;

    /// <summary>
    /// The mode Expanded gives back when it closes: Closed or Docked.
    /// </summary>
    private GalleryMode _giveBack = GalleryMode.Closed;

    // The two hiders; the gallery is hidden while either holds
    private bool _hiddenByTab;
    private bool _isOverLimit;

    /// <summary>
    /// The tab's mode, whether or not it is hidden.
    /// </summary>
    public ReadOnlyReactiveProperty<GalleryMode> Mode => _mode;

    /// <summary>
    /// Whether the tab's gallery is docked, or expanded and gives back docked.
    /// </summary>
    public ReadOnlyReactiveProperty<bool> IsDocked => _isDocked;

    /// <summary>
    /// Whether crop, a Single Image or the file limit hides the gallery.
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
    /// True while <see cref="DockAt"/> moves the window's edge for a Closed gallery it is about to dock: the tab's
    /// gallery publishes no appearance for the edge alone then, as the dock that follows publishes the one appearance
    /// at the new edge (no Closed one at the new edge before it).
    /// </summary>
    internal bool IsMovingTheEdgeToDock { get; private set; }

    /// <summary>
    /// Gives the switcher the window it remembers in: <paramref name="currentWindow"/> is asked at each write, so a tab
    /// moved to another window writes there. The tab calls it once.
    /// </summary>
    internal void RemembersIn(Func<WindowGallerySettings?> currentWindow) => _currentWindow = currentWindow;

    /// <summary>
    /// Sets the mode a tab starts in, without animation and without remembering anything.
    /// </summary>
    public void Start(bool docked)
    {
        _giveBack = GalleryMode.Closed;
        SetMode(docked ? GalleryMode.Docked : GalleryMode.Closed, animate: false);
    }

    /// <summary>
    /// Starts docked or closed as the tab's window says a new tab starts; as the record says for a tab with no window.
    /// </summary>
    public void Start() =>
        Start(OpenWindow() is { } window
            ? window.StartsDocked.CurrentValue
            : WindowGallerySettings.StartsDockedWithoutWindow(Settings?.Gallery));

    /// <summary>
    /// Hides or shows the gallery for crop or a Single Image, without animation and without changing the mode.
    /// </summary>
    public void SetHidden(bool hidden)
    {
        _hiddenByTab = hidden;
        UpdateHidden();
    }

    /// <summary>
    /// Hides or shows the gallery for a file list at or over the file limit, without animation and without changing
    /// the mode. The tab's gallery coordinator calls it with each file list, as it is the one reader of the limit.
    /// </summary>
    public void SetOverLimit(bool overLimit)
    {
        _isOverLimit = overLimit;
        UpdateHidden();
    }

    private void UpdateHidden()
    {
        var hidden = _hiddenByTab || _isOverLimit;
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

        RememberDocked();
        return ValueTask.CompletedTask;
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
                break;
            case GalleryMode.Docked:
                SetMode(GalleryMode.Closed, animate: true);
                RememberDocked();
                break;
        }

        return ValueTask.CompletedTask;
    }

    public ValueTask Close()
    {
        if (_isHidden.Value || _mode.Value is GalleryMode.Closed)
        {
            return ValueTask.CompletedTask;
        }

        _giveBack = GalleryMode.Closed;
        SetMode(GalleryMode.Closed, animate: true);
        RememberDocked();
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Sets the edge every Docked Gallery of the tab's window sits at, and docks this tab. While hidden, only the edge
    /// changes. The tab publishes one Gallery Appearance for it, at the new edge.
    /// </summary>
    public ValueTask DockAt(GalleryDockPosition position)
    {
        if (_isHidden.Value || _mode.Value is not GalleryMode.Closed)
        {
            // One change: the edge of the shown or hidden gallery
            Remember(static window => window.DockPosition, position);
            if (!_isHidden.Value && _mode.Value is GalleryMode.Expanded)
            {
                _giveBack = GalleryMode.Docked;
                UpdateIsDocked();
            }
        }
        else
        {
            // The edge moves first, held, so the dock is published once with the new edge
            IsMovingTheEdgeToDock = true;
            try
            {
                Remember(static window => window.DockPosition, position);
            }
            finally
            {
                IsMovingTheEdgeToDock = false;
            }

            SetMode(GalleryMode.Docked, animate: true);
        }

        if (!_isHidden.Value)
        {
            RememberDocked();
        }

        return ValueTask.CompletedTask;
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

    // The docked choice made by hand, for the next tab of the window
    private void RememberDocked() => Remember(static window => window.StartsDocked, _isDocked.Value);

    // The window the tab is in now, or null without one or once it closed
    private WindowGallerySettings? OpenWindow() =>
        _currentWindow?.Invoke() is { IsDisposed: false } window ? window : null;

    /// <summary>
    /// Sets one of the tab's window's gallery settings. A closed window's settings take no change, as setting a
    /// disposed bindable throws (ADR-0038), so this does nothing once the window closed, nothing either when the close
    /// lands between the check and the set, and nothing for a tab with no window.
    /// </summary>
    private void Remember<T>(Func<WindowGallerySettings, BindableReactiveProperty<T>> pick, T value)
    {
        if (OpenWindow() is not { } window)
        {
            return;
        }

        try
        {
            pick(window).Value = value;
        }
        catch (ObjectDisposedException)
        {
            // The window closed between the check and the set
        }
    }

    public void Dispose()
    {
        _changes.Dispose();
        Disposable.Dispose(_mode, _isDocked, _isHidden, _shownMode);
    }
}
